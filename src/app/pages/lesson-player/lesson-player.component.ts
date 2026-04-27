import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LessonPlayerService } from '../../core/services/lesson-player.service';
import { Lesson } from '../../shared/models/learning.models';

type LessonStepKind = 'vocab' | 'quiz' | 'blank' | 'speaking' | 'reward';
type FeedbackState = 'idle' | 'correct' | 'incorrect';

interface LessonStep {
  kind: LessonStepKind;
  title: string;
  prompt: string;
  helper: string;
}

@Component({
  selector: 'app-lesson-player',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './lesson-player.component.html',
  styleUrl: './lesson-player.component.scss',
})
export class LessonPlayerComponent {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly lessonPlayerService = inject(LessonPlayerService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  protected readonly lessonKey = signal('');
  protected readonly lesson = signal<Lesson | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isCompleting = signal(false);
  protected readonly error = signal('');
  protected readonly completionMessage = signal('');
  protected readonly currentStepIndex = signal(0);
  protected readonly selectedOption = signal<'der' | 'die' | 'das' | ''>('');
  protected readonly blankAnswer = signal('');
  protected readonly speakingReady = signal(false);
  protected readonly feedback = signal<FeedbackState>('idle');
  protected readonly feedbackMessage = signal('');
  protected readonly earnedXp = signal(0);
  protected readonly level = computed(() => this.lesson()?.level ?? 'A1');
  protected readonly lessonSteps = computed<LessonStep[]>(() => [
    {
      kind: 'vocab',
      title: 'Learn vocabulary',
      prompt: 'Preview the target words before the challenge starts.',
      helper: 'Listen once, repeat once, then move on.',
    },
    {
      kind: 'quiz',
      title: 'Quick quiz',
      prompt: 'Choose the right article for the noun.',
      helper: 'Keep it fast. Instant feedback comes right after selection.',
    },
    {
      kind: 'blank',
      title: 'Fill in the blank',
      prompt: 'Type the missing German word.',
      helper: 'Grammar clarity first, speed second.',
    },
    {
      kind: 'speaking',
      title: 'Speaking exercise',
      prompt: 'Say the phrase aloud with a steady rhythm.',
      helper: 'This simulates the future speaking evaluator slot.',
    },
    {
      kind: 'reward',
      title: 'Reward unlocked',
      prompt: 'XP, streak energy, and badge progress update here.',
      helper: 'End the lesson on a clear win.',
    },
  ]);
  protected readonly activeStep = computed(
    () => this.lessonSteps()[this.currentStepIndex()] ?? this.lessonSteps()[0],
  );
  protected readonly progressPercent = computed(() =>
    Math.round((this.currentStepIndex() / (this.lessonSteps().length - 1)) * 100),
  );
  protected readonly currentWord = computed(() => this.lesson()?.vocabulary[0] ?? null);
  protected readonly speakingScore = computed(() => (this.feedback() === 'correct' ? 92 : 0));

  constructor() {
    this.activatedRoute.paramMap.subscribe((params) => {
      const lessonKey = params.get('lessonKey') ?? '';

      this.lessonKey.set(lessonKey);
      this.completionMessage.set('');
      this.currentStepIndex.set(0);
      this.selectedOption.set('');
      this.blankAnswer.set('');
      this.speakingReady.set(false);
      this.feedback.set('idle');
      this.feedbackMessage.set('');
      this.earnedXp.set(0);
      this.loadLesson(lessonKey);
    });
  }

  protected playWord(germanWord: string, audioUrl?: string | null): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    if (audioUrl) {
      const audio = new Audio(audioUrl);
      void audio.play();
      return;
    }

    const synth = window.speechSynthesis;
    const utterance = new SpeechSynthesisUtterance(germanWord);
    utterance.lang = 'de-DE';
    synth.cancel();
    synth.speak(utterance);
  }

  protected completeLesson(): void {
    const currentLesson = this.lesson();
    if (!currentLesson) {
      return;
    }

    this.isCompleting.set(true);
    this.error.set('');
    this.completionMessage.set('');

    this.lessonPlayerService.completeLesson(currentLesson).subscribe({
      next: () => {
        this.completionMessage.set('Lesson logged and synced to progress.');
        void this.router.navigate(['/progress']);
      },
      error: (err: unknown) => {
        this.error.set(`Could not save lesson completion: ${String(err)}`);
      },
      complete: () => this.isCompleting.set(false),
    });
  }

  protected submitQuizAnswer(option: 'der' | 'die' | 'das'): void {
    this.selectedOption.set(option);

    if (option === 'der') {
      this.markCorrect('Nice. "Hund" takes "der", so the grammar target sticks.');
      return;
    }

    this.markIncorrect('Close, but this noun uses "der". Try once more.');
  }

  protected submitBlankAnswer(): void {
    const normalized = this.blankAnswer().trim().toLowerCase();

    if (normalized === 'hund') {
      this.markCorrect('Exactly. Short, correct, and ready for speaking.');
      return;
    }

    this.markIncorrect('Not quite. The missing word is "Hund".');
  }

  protected assessSpeaking(): void {
    this.speakingReady.set(true);
    this.markCorrect('Great energy. Pronunciation feels strong enough to bank the reward.');
  }

  protected continueFromFeedback(): void {
    this.feedback.set('idle');
    this.feedbackMessage.set('');

    if (this.activeStep().kind === 'reward') {
      this.completeLesson();
      return;
    }

    this.currentStepIndex.update((index) => Math.min(index + 1, this.lessonSteps().length - 1));
  }

  protected skipVocabStep(): void {
    this.feedback.set('correct');
    this.feedbackMessage.set('Vocabulary anchored. On to the first grammar check.');
  }

  private markCorrect(message: string): void {
    this.feedback.set('correct');
    this.feedbackMessage.set(message);
    this.earnedXp.update((xp) => xp + 10);
  }

  private markIncorrect(message: string): void {
    this.feedback.set('incorrect');
    this.feedbackMessage.set(message);
  }

  private loadLesson(lessonKey: string): void {
    this.isLoading.set(true);
    this.error.set('');

    this.lessonPlayerService.getLessonByKey(lessonKey).subscribe({
      next: (lesson) => this.lesson.set(lesson),
      error: (err: unknown) => this.error.set(`Failed to load lesson data: ${String(err)}`),
      complete: () => this.isLoading.set(false),
    });
  }
}

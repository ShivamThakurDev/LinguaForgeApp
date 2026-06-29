import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LessonPlayerService } from '../../core/services/lesson-player.service';
import { LessonService } from '../../core/services/lesson.service';
import { Lesson, LessonExercise } from '../../shared/models/learning.models';

type PlayerStepKind = 'vocab' | 'exercise' | 'speaking' | 'reward';
type FeedbackState = 'idle' | 'correct' | 'incorrect';

interface PlayerStep {
  kind: PlayerStepKind;
  title: string;
  helper: string;
  exercise?: LessonExercise;
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
  private readonly lessonService = inject(LessonService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  protected readonly lessonKey = signal('');
  protected readonly lesson = signal<Lesson | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isCompleting = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly error = signal('');
  protected readonly completionMessage = signal('');
  protected readonly currentStepIndex = signal(0);
  protected readonly selectedOption = signal('');
  protected readonly blankAnswer = signal('');
  protected readonly speakingReady = signal(false);
  protected readonly feedback = signal<FeedbackState>('idle');
  protected readonly feedbackMessage = signal('');
  protected readonly earnedXp = signal(0);
  protected readonly level = computed(() => this.lesson()?.level ?? 'A1');

  // Steps are built from the lesson's real, server-provided exercises: an intro
  // vocab step, one step per exercise, then speaking and the reward screen.
  protected readonly steps = computed<PlayerStep[]>(() => {
    const exercises = this.lesson()?.exercises ?? [];
    const steps: PlayerStep[] = [
      { kind: 'vocab', title: 'Learn vocabulary', helper: 'Listen once, repeat once, then move on.' },
    ];

    exercises.forEach((exercise, index) =>
      steps.push({
        kind: 'exercise',
        title: `Challenge ${index + 1}`,
        helper: 'Answer is checked on the server — no peeking at the key.',
        exercise,
      }),
    );

    steps.push(
      { kind: 'speaking', title: 'Speaking exercise', helper: 'Say the phrase aloud with a steady rhythm.' },
      { kind: 'reward', title: 'Reward unlocked', helper: 'End the lesson on a clear win.' },
    );

    return steps;
  });

  protected readonly activeStep = computed(() => this.steps()[this.currentStepIndex()] ?? this.steps()[0]);
  protected readonly activeExercise = computed(() => this.activeStep()?.exercise ?? null);
  protected readonly progressPercent = computed(() =>
    Math.round((this.currentStepIndex() / Math.max(1, this.steps().length - 1)) * 100),
  );
  protected readonly currentWord = computed(() => this.lesson()?.vocabulary[0] ?? null);

  // Longest vocab entry is most likely a full phrase — good for the speaking prompt.
  protected readonly speakingPhrase = computed(() => {
    const vocab = this.lesson()?.vocabulary ?? [];
    if (!vocab.length) {
      return 'Guten Tag';
    }
    return vocab.reduce((longest, item) => (item.german.length > longest.german.length ? item : longest)).german;
  });

  constructor() {
    this.activatedRoute.paramMap.subscribe((params) => {
      const lessonKey = params.get('lessonKey') ?? '';

      this.lessonKey.set(lessonKey);
      this.resetLessonState();
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

  protected selectOption(option: string): void {
    if (this.isSubmitting() || this.feedback() === 'correct') {
      return;
    }
    this.selectedOption.set(option);
    this.submitExerciseAnswer(option);
  }

  protected submitBlank(): void {
    if (this.isSubmitting() || this.feedback() === 'correct') {
      return;
    }
    this.submitExerciseAnswer(this.blankAnswer().trim());
  }

  private submitExerciseAnswer(answer: string): void {
    const exercise = this.activeExercise();
    if (!exercise || !answer) {
      return;
    }

    this.isSubmitting.set(true);
    this.feedback.set('idle');

    this.lessonService.submitAnswer({ exerciseId: exercise.id, submittedAnswer: answer }).subscribe({
      next: (result) => {
        if (result.isCorrect) {
          this.feedback.set('correct');
          this.feedbackMessage.set(result.explanation || 'Correct!');
          this.earnedXp.update((xp) => xp + result.earnedXp);
        } else {
          this.feedback.set('incorrect');
          this.feedbackMessage.set(
            result.explanation
              ? `Not quite. ${result.explanation}`
              : `Not quite. The answer is "${result.correctAnswer}".`,
          );
        }
      },
      error: (err: { status?: number }) => {
        this.feedback.set('incorrect');
        this.feedbackMessage.set(
          err?.status === 401
            ? 'Sign in to check answers and save your progress.'
            : 'We could not check that answer. Please try again.',
        );
      },
      complete: () => this.isSubmitting.set(false),
    });
  }

  protected assessSpeaking(): void {
    this.speakingReady.set(true);
    this.feedback.set('correct');
    this.feedbackMessage.set('Great energy. Pronunciation feels strong enough to bank the reward.');
  }

  protected skipVocabStep(): void {
    this.feedback.set('correct');
    this.feedbackMessage.set('Vocabulary anchored. On to the first challenge.');
  }

  protected continueFromFeedback(): void {
    const wasReward = this.activeStep()?.kind === 'reward';
    this.feedback.set('idle');
    this.feedbackMessage.set('');
    this.selectedOption.set('');
    this.blankAnswer.set('');
    this.speakingReady.set(false);

    if (wasReward) {
      this.completeLesson();
      return;
    }

    this.currentStepIndex.update((index) => Math.min(index + 1, this.steps().length - 1));
  }

  protected retry(): void {
    this.feedback.set('idle');
    this.feedbackMessage.set('');
    this.selectedOption.set('');
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
      error: () => {
        this.error.set('Could not save your lesson. Please check your connection and try again.');
      },
      complete: () => this.isCompleting.set(false),
    });
  }

  private resetLessonState(): void {
    this.completionMessage.set('');
    this.currentStepIndex.set(0);
    this.selectedOption.set('');
    this.blankAnswer.set('');
    this.speakingReady.set(false);
    this.feedback.set('idle');
    this.feedbackMessage.set('');
    this.earnedXp.set(0);
  }

  private loadLesson(lessonKey: string): void {
    this.isLoading.set(true);
    this.error.set('');

    this.lessonPlayerService.getLessonByKey(lessonKey).subscribe({
      next: (lesson) => this.lesson.set(lesson),
      error: () => this.error.set('We could not load this lesson. Please go back and try again.'),
      complete: () => this.isLoading.set(false),
    });
  }
}

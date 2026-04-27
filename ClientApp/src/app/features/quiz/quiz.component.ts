import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { QuizService } from '../../core/services/quiz.service';
import { QuizEvaluationResponse, QuizExercise } from '../../shared/models/learning.models';

@Component({
  selector: 'app-quiz',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quiz.component.html',
  styleUrl: './quiz.component.scss',
})
export class QuizComponent {
  private readonly quizService = inject(QuizService);
  private readonly authService = inject(AuthService);
  private readonly learningSyncService = inject(LearningSyncService);

  topic = signal<string>('articles');
  level = signal<string>('A1');
  exerciseType = signal<string>('mcq');

  exercise = signal<QuizExercise | null>(null);
  selectedOptionId = signal<string>('');
  fillInAnswer = signal<string>('');
  isLoading = signal<boolean>(false);
  submitted = signal<boolean>(false);
  error = signal<string>('');
  evaluation = signal<QuizEvaluationResponse | null>(null);

  isCorrect = computed(() => this.evaluation()?.isCorrect ?? false);
  isAuthenticated = computed(() => !!this.authService.currentUser());

  generate(): void {
    if (!this.isAuthenticated()) {
      this.error.set('Sign in to generate AI practice.');
      return;
    }

    this.isLoading.set(true);
    this.error.set('');
    this.submitted.set(false);
    this.selectedOptionId.set('');
    this.fillInAnswer.set('');
    this.evaluation.set(null);

    this.quizService
      .generate({ topic: this.topic(), level: this.level(), exerciseType: this.exerciseType() })
      .subscribe({
        next: (res) => this.exercise.set(res.exercise),
        error: (err: unknown) => this.error.set(`Failed to generate quiz: ${String(err)}`),
        complete: () => this.isLoading.set(false),
      });
  }

  submit(): void {
    const exercise = this.exercise();
    if (!exercise) {
      return;
    }

    const selectedOption = exercise.options.find((option) => option.id === this.selectedOptionId());
    const correctOption = exercise.options.find((option) => option.id === exercise.correctOptionId);
    const submittedAnswer = exercise.exerciseType === 'fill-in'
      ? this.fillInAnswer().trim()
      : (selectedOption?.text ?? '');
    const correctAnswer = correctOption?.text ?? exercise.correctOptionId ?? exercise.promptText;

    if (!submittedAnswer) {
      return;
    }

    this.isLoading.set(true);
    this.error.set('');

    this.quizService.evaluate({
      lessonKey: `a1-${this.topic().toLowerCase()}`,
      topic: this.topic(),
      level: this.level(),
      exerciseType: exercise.exerciseType,
      question: exercise.question,
      promptText: exercise.promptText,
      correctAnswer,
      submittedAnswer,
    }).subscribe({
      next: (response) => {
        this.evaluation.set(response);
        this.submitted.set(true);
        this.learningSyncService.notifyUpdate();
      },
      error: (err: unknown) => {
        this.error.set(`Failed to evaluate answer: ${String(err)}`);
      },
      complete: () => this.isLoading.set(false),
    });
  }
}

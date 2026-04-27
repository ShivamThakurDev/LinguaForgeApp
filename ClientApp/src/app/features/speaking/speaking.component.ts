import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SpeechService } from '../../core/services/speech.service';
import { PronunciationResult } from '../../shared/models/learning.models';

@Component({
  selector: 'app-speaking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './speaking.component.html',
  styleUrl: './speaking.component.scss',
})
export class SpeakingComponent {
  private readonly speechService = inject(SpeechService);
  private readonly platformId = inject(PLATFORM_ID);

  referenceText = signal<string>('Guten Morgen, wie geht es dir?');
  locale = signal<string>('de-DE');
  result = signal<PronunciationResult | null>(null);
  isRecording = signal<boolean>(false);
  isAssessing = signal<boolean>(false);
  error = signal<string>('');

  private recorder: MediaRecorder | null = null;
  private chunks: Blob[] = [];

  scoreBars = computed(() => {
    const current = this.result();
    if (!current) {
      return [];
    }

    return [
      { label: 'Accuracy', value: current.accuracyScore },
      { label: 'Fluency', value: current.fluencyScore },
      { label: 'Completeness', value: current.completenessScore },
      { label: 'Overall', value: current.pronunciationScore },
    ];
  });

  async startRecording(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.error.set('');
    this.result.set(null);

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.chunks = [];
      this.recorder = new MediaRecorder(stream);

      this.recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.chunks.push(event.data);
        }
      };

      this.recorder.start();
      this.isRecording.set(true);
    } catch (err: unknown) {
      this.error.set(`Microphone access failed: ${String(err)}`);
    }
  }

  stopRecording(): void {
    if (!this.recorder) {
      return;
    }

    this.recorder.onstop = () => {
      const audioBlob = new Blob(this.chunks, { type: 'audio/wav' });
      this.assess(audioBlob);
    };

    this.recorder.stop();
    this.recorder.stream.getTracks().forEach((track) => track.stop());
    this.isRecording.set(false);
  }

  private assess(audioBlob: Blob): void {
    this.isAssessing.set(true);
    this.error.set('');

    this.speechService.assess(audioBlob, this.referenceText(), this.locale()).subscribe({
      next: (res) => this.result.set(res),
      error: (err: unknown) => this.error.set(`Assessment failed: ${String(err)}`),
      complete: () => this.isAssessing.set(false),
    });
  }
}
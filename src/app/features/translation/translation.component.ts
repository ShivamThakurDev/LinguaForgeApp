import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LanguageOption, TranslateResponseDto } from '../../shared/models/translation.models';
import { TranslationService } from '../../core/services/translation.service';
import { HttpErrorResponse } from '@angular/common/http';


@Component({
  standalone: true,
  selector: 'app-translation',
  imports: [CommonModule, FormsModule],
  templateUrl: './translation.component.html',
  styleUrls: ['./translation.component.scss'],
})
export class Translation implements OnInit {
  private readonly svc = inject(TranslationService);

languages = signal<LanguageOption[]>([]);
inputText = signal<string>('');
fromLang = signal<string>('');
toLang = signal<string>('de');
result = signal<TranslateResponseDto | null>(null);
isLoading = signal<boolean>(false);
errorMsg = signal<string>('');
swapping = signal<boolean>(false);
langError = signal<string>('');

   // ── Derived ──────────────────────────────────────────────────────────────
  charCount   = computed(() => this.inputText().length);
  canTranslate = computed(() =>
    this.inputText().trim().length > 0 &&
    this.toLang().length > 0 &&
    !this.isLoading()
  );

  ngOnInit(): void {
    this.svc.getSupportedLanguages().subscribe({
      next: (langs: LanguageOption[]) => this.languages.set(langs),
      error: (err: unknown) => this.langError.set('Failed to load languages: ' + (err instanceof Error ? err.message : String(err)))
    });
  }

  translate(): void {
    if (!this.canTranslate()) return;
    
    this.isLoading.set(true);
    this.errorMsg.set('');
    this.result.set(null);

    this.svc.translate({
      text: this.inputText(),
      from: this.fromLang(),
      to: this.toLang()
    }).subscribe({
      next: (res) => this.result.set(res),
      error: (err: unknown) => this.errorMsg.set('Translation failed: ' + (err instanceof Error ? err.message : String(err))),
      complete: () => this.isLoading.set(false)
    });
  }

  swapLanguages(): void {
    //Can't swap if source is auto-detect or same as target
    const from = this.fromLang();
    const to = this.toLang();
    if(!from || from === to) return;
    
    this.swapping.set(true);
    setTimeout(() => {
      this.fromLang.set(to);
      this.toLang.set(from);

      //If there's a result, put translated text back into input 
      const current = this.result();
      if(current) {
        this.inputText.set(current.translatedText);
        this.result.set(null);
      }
      this.swapping.set(false);
    }, 300); //simulate delay for better UX
    }

    clearAll(): void {
      this.inputText.set('');
      this.result.set(null);
      this.errorMsg.set('');
    }

    onInputChange(value:string): void {
      this.inputText.set(value);
      //Clear stale result when input changes
      if(this.result()) {
        this.result.set(null);
      }
    }
    copyResult():void{
      const text = this.result()?.translatedText;
      if(text) {
        navigator.clipboard.writeText(text).catch(err => {
          this.errorMsg.set('Failed to copy: ' + (err instanceof Error ? err.message : String(err)));
        });
      }
    }
    
    private handleError(err: HttpErrorResponse): void {
    if (err.status === 0) {
      this.errorMsg.set('Cannot reach the API — is your .NET backend running?');
    } else if (err.status === 400) {
      this.errorMsg.set('Invalid request — check your input.');
    } else if (err.status === 502) {
      this.errorMsg.set('Azure Translator is unavailable. Try again shortly.');
    } else {
      this.errorMsg.set(`Unexpected error (${err.status}). Please try again.`);
    }
  }
}

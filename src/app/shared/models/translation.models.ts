export interface TranslateRequestDto{
    text: string;
    from: string; //empty string = auto-detect
    to: string;
}

export interface TranslateResponseDto{
    translatedText: string;
    detectedSourceLanguage: string;
    languagePair: string; // e.g. "en->de"
    characterCount: number;
}

export interface LanguageOption{
    code: string; // e.g. "en"
    name: string; // e.g. "English"
}
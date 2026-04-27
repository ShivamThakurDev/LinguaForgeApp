export interface LessonVocab {
  german: string;
  english: string;
  partOfSpeech: string;
  audioUrl?: string | null;
}

export interface Lesson {
  lessonKey: string;
  title: string;
  level: string;
  description: string;
  vocabulary: LessonVocab[];
}

export interface QuizOption {
  id: string;
  text: string;
}

export interface QuizExercise {
  exerciseType: string;
  question: string;
  options: QuizOption[];
  correctOptionId: string;
  explanation: string;
  promptText: string;
}

export interface GenerateExerciseResponse {
  exercise: QuizExercise;
}

export interface QuizEvaluationRequest {
  lessonKey: string;
  topic: string;
  level: string;
  exerciseType: string;
  question: string;
  promptText: string;
  correctAnswer: string;
  submittedAnswer: string;
}

export interface QuizEvaluationResponse {
  isCorrect: boolean;
  scorePercent: number;
  earnedXp: number;
  feedback: string;
  correctedAnswer: string;
  weakTopic: string;
}

export interface PronunciationScore {
  phoneme: string;
  accuracyScore: number;
}

export interface PronunciationResult {
  accuracyScore: number;
  fluencyScore: number;
  completenessScore: number;
  pronunciationScore: number;
  phonemes: PronunciationScore[];
  recognizedText: string;
}

export interface ProgressBadge {
  code: string;
  name: string;
  description: string;
  unlockedAtUtc: string;
}

export interface HeatmapPoint {
  date: string;
  xp: number;
}

export interface UserProgress {
  userId: string;
  totalXp: number;
  currentStreakDays: number;
  level: number;
  badges: ProgressBadge[];
  heatmap: HeatmapPoint[];
}

export interface AuthUser {
  id: string;
  userName: string;
  email: string;
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  user: AuthUser;
}

export interface Recommendation {
  topic: string;
  lessonKey: string;
  suggestedExerciseType: string;
  level: string;
  reason: string;
  priorityScore: number;
}

import type { TaskPriority, TaskStatus } from "@/types/api";

type Tone = "neutral" | "primary" | "success" | "warning" | "danger";

export const TASK_STATUSES: TaskStatus[] = ["Backlog", "Todo", "InProgress", "InReview", "Done", "Cancelled"];

export const BOARD_STATUSES: TaskStatus[] = ["Backlog", "Todo", "InProgress", "InReview", "Done"];

export const TASK_PRIORITIES: TaskPriority[] = ["Low", "Medium", "High", "Critical"];

const STATUS_LABELS: Record<TaskStatus, string> = {
  Backlog: "Bekleyen",
  Todo: "Yapılacak",
  InProgress: "Devam ediyor",
  InReview: "İncelemede",
  Done: "Tamamlandı",
  Cancelled: "İptal edildi",
};

const STATUS_TONES: Record<TaskStatus, Tone> = {
  Backlog: "neutral",
  Todo: "neutral",
  InProgress: "primary",
  InReview: "warning",
  Done: "success",
  Cancelled: "danger",
};

const PRIORITY_LABELS: Record<TaskPriority, string> = {
  Low: "Düşük",
  Medium: "Orta",
  High: "Yüksek",
  Critical: "Kritik",
};

const PRIORITY_TONES: Record<TaskPriority, Tone> = {
  Low: "neutral",
  Medium: "primary",
  High: "warning",
  Critical: "danger",
};

export function getTaskStatusLabel(status: TaskStatus): string {
  return STATUS_LABELS[status];
}

export function getTaskStatusTone(status: TaskStatus): Tone {
  return STATUS_TONES[status];
}

export function getTaskPriorityLabel(priority: TaskPriority): string {
  return PRIORITY_LABELS[priority];
}

export function getTaskPriorityTone(priority: TaskPriority): Tone {
  return PRIORITY_TONES[priority];
}

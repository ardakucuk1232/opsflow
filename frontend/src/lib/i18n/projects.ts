import type { ProjectMemberRole, ProjectStatus } from "@/types/api";

export const PROJECT_STATUSES: ProjectStatus[] = ["Planning", "Active", "OnHold", "Completed", "Cancelled"];

const STATUS_LABELS: Record<ProjectStatus, string> = {
  Planning: "Planlanıyor",
  Active: "Aktif",
  OnHold: "Beklemede",
  Completed: "Tamamlandı",
  Cancelled: "İptal edildi",
};

const STATUS_TONES: Record<ProjectStatus, "neutral" | "primary" | "success" | "warning" | "danger"> = {
  Planning: "neutral",
  Active: "primary",
  OnHold: "warning",
  Completed: "success",
  Cancelled: "danger",
};

const MEMBER_ROLE_LABELS: Record<ProjectMemberRole, string> = {
  Lead: "Proje lideri",
  Member: "Üye",
};

export function getProjectStatusLabel(status: ProjectStatus): string {
  return STATUS_LABELS[status];
}

export function getProjectStatusTone(status: ProjectStatus) {
  return STATUS_TONES[status];
}

export function getProjectMemberRoleLabel(role: ProjectMemberRole): string {
  return MEMBER_ROLE_LABELS[role];
}

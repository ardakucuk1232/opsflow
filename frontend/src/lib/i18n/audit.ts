import { getProjectMemberRoleLabel, getProjectStatusLabel, PROJECT_STATUSES } from "@/lib/i18n/projects";
import { getTaskPriorityLabel, getTaskStatusLabel, TASK_PRIORITIES, TASK_STATUSES } from "@/lib/i18n/tasks";
import { formatDateOnly, formatDateTime } from "@/lib/utils/format";
import type { AuditAction, ProjectMemberRole, ProjectStatus, TaskPriority, TaskStatus } from "@/types/api";

export const AUDIT_ACTIONS: AuditAction[] = ["Created", "Updated", "Deleted"];

const ACTION_LABELS: Record<AuditAction, string> = {
  Created: "Oluşturuldu",
  Updated: "Güncellendi",
  Deleted: "Silindi",
};

const ACTION_TONES: Record<AuditAction, "success" | "primary" | "danger"> = {
  Created: "success",
  Updated: "primary",
  Deleted: "danger",
};

export const AUDIT_ENTITIES: { value: string; label: string }[] = [
  { value: "TaskItem", label: "Görev" },
  { value: "TaskComment", label: "Yorum" },
  { value: "Attachment", label: "Dosya" },
  { value: "Project", label: "Proje" },
  { value: "ProjectMember", label: "Proje üyesi" },
  { value: "User", label: "Kullanıcı" },
  { value: "UserRole", label: "Kullanıcı rolü" },
  { value: "Role", label: "Rol" },
  { value: "RolePermission", label: "Rol izni" },
  { value: "Company", label: "Şirket" },
];

const FIELD_LABELS: Record<string, string> = {
  Title: "Başlık",
  Description: "Açıklama",
  Status: "Durum",
  Priority: "Öncelik",
  AssigneeId: "Atanan kişi",
  ReporterId: "Oluşturan",
  DueDate: "Bitiş tarihi",
  CompletedAt: "Tamamlanma",
  Number: "Numara",
  ProjectId: "Proje",
  Name: "Ad",
  Key: "Kısa ad",
  StartDate: "Başlangıç",
  EndDate: "Bitiş",
  CreatedByUserId: "Oluşturan",
  Role: "Proje rolü",
  UserId: "Kullanıcı",
  RoleId: "Rol",
  PermissionId: "İzin",
  Permissions: "İzinler",
  IsSystemRole: "Sistem rolü",
  Email: "E-posta",
  FirstName: "Ad",
  LastName: "Soyad",
  PasswordHash: "Şifre",
  IsActive: "Aktif",
  IsEmailVerified: "E-posta doğrulandı",
  Body: "Yorum",
  EditedAt: "Düzenlenme",
  TaskItemId: "Görev",
  AuthorId: "Yazan",
  FileName: "Dosya adı",
  ContentType: "Dosya türü",
  SizeBytes: "Boyut",
  UploadedByUserId: "Yükleyen",
  Slug: "Kısa adres",
  LogoUrl: "Logo",
  SubscriptionPlan: "Plan",
};

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;
const DATE_TIME = /^\d{4}-\d{2}-\d{2}T/;

export function getAuditActionLabel(action: AuditAction): string {
  return ACTION_LABELS[action];
}

export function getAuditActionTone(action: AuditAction) {
  return ACTION_TONES[action];
}

export function getAuditEntityLabel(entityName: string): string {
  return AUDIT_ENTITIES.find((entity) => entity.value === entityName)?.label ?? entityName;
}

export function getAuditFieldLabel(field: string): string {
  return FIELD_LABELS[field] ?? field;
}

function formatText(value: string, field: string): string {
  if (field === "Status" && (TASK_STATUSES as string[]).includes(value)) {
    return getTaskStatusLabel(value as TaskStatus);
  }

  if (field === "Status" && (PROJECT_STATUSES as string[]).includes(value)) {
    return getProjectStatusLabel(value as ProjectStatus);
  }

  if (field === "Priority" && (TASK_PRIORITIES as string[]).includes(value)) {
    return getTaskPriorityLabel(value as TaskPriority);
  }

  if (field === "Role" && (value === "Lead" || value === "Member")) {
    return getProjectMemberRoleLabel(value as ProjectMemberRole);
  }

  if (value === "***") {
    return "Gizli";
  }

  if (GUID.test(value)) {
    return `#${value.slice(0, 8)}`;
  }

  if (DATE_ONLY.test(value)) {
    return formatDateOnly(value);
  }

  if (DATE_TIME.test(value)) {
    return field === "DueDate" ? formatDateOnly(value.slice(0, 10)) : formatDateTime(value);
  }

  return value;
}

export function formatAuditValue(value: unknown, field: string): string {
  if (value === null || value === undefined || value === "") {
    return "—";
  }

  if (typeof value === "boolean") {
    return value ? "Evet" : "Hayır";
  }

  if (typeof value === "number") {
    return field === "SizeBytes" ? `${new Intl.NumberFormat("tr-TR").format(value)} B` : String(value);
  }

  if (Array.isArray(value)) {
    return value.length === 0 ? "—" : value.map((item) => formatAuditValue(item, field)).join(", ");
  }

  if (typeof value === "string") {
    return formatText(value, field);
  }

  return JSON.stringify(value);
}

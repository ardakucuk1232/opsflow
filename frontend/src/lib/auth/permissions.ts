import type { AuthUser } from "@/types/api";

export const PERMISSIONS = {
  companyManage: "company.manage",
  userView: "user.view",
  userInvite: "user.invite",
  userManage: "user.manage",
  roleManage: "role.manage",
  projectViewAll: "project.view_all",
  projectCreate: "project.create",
  projectManage: "project.manage",
  taskCreate: "task.create",
  taskAssign: "task.assign",
  taskUpdate: "task.update",
  taskDelete: "task.delete",
  commentCreate: "comment.create",
  attachmentUpload: "attachment.upload",
  reportView: "report.view",
  auditLogView: "audit_log.view",
} as const;

export type PermissionCode = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];

export function hasPermission(user: AuthUser | null, permission: string): boolean {
  return user !== null && user.permissions.includes(permission);
}

export function holdsAll(user: AuthUser | null, permissions: readonly string[]): boolean {
  return user !== null && permissions.every((permission) => user.permissions.includes(permission));
}

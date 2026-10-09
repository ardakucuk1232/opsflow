import type { Role } from "@/types/api";

export const ADMIN_ROLE: Role = {
  id: "role-admin",
  name: "Admin",
  description: null,
  isSystemRole: true,
  permissions: [
    "attachment.upload",
    "audit_log.view",
    "comment.create",
    "company.manage",
    "project.create",
    "project.manage",
    "project.view_all",
    "report.view",
    "role.manage",
    "task.assign",
    "task.create",
    "task.delete",
    "task.update",
    "user.invite",
    "user.manage",
    "user.view",
  ],
  userCount: 1,
};

export const MANAGER_ROLE: Role = {
  id: "role-manager",
  name: "Manager",
  description: null,
  isSystemRole: true,
  permissions: ["project.create", "project.manage", "project.view_all", "user.view"],
  userCount: 0,
};

export const EMPLOYEE_ROLE: Role = {
  id: "role-employee",
  name: "Employee",
  description: null,
  isSystemRole: true,
  permissions: ["attachment.upload", "comment.create", "task.update", "user.view"],
  userCount: 2,
};

export const SYSTEM_ROLES = [ADMIN_ROLE, EMPLOYEE_ROLE, MANAGER_ROLE];

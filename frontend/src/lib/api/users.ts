import { api } from "@/lib/api/client";
import type { PagedResult, UserSummary } from "@/types/api";

export type UserStatusFilter = "Active" | "Invited" | "Inactive";

export type UserListParams = {
  page: number;
  pageSize: number;
  search?: string;
  status?: UserStatusFilter;
  roleId?: string;
};

export type InviteUserInput = {
  email: string;
  firstName: string;
  lastName: string;
  roleIds: string[];
};

function toQueryString(params: UserListParams): string {
  const query = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  });

  if (params.search) {
    query.set("search", params.search);
  }

  if (params.status) {
    query.set("status", params.status);
  }

  if (params.roleId) {
    query.set("roleId", params.roleId);
  }

  return query.toString();
}

function userPath(id: string, action?: string): string {
  const base = `/api/users/${encodeURIComponent(id)}`;

  return action ? `${base}/${action}` : base;
}

export const usersApi = {
  list(params: UserListParams, signal?: AbortSignal): Promise<PagedResult<UserSummary>> {
    return api<PagedResult<UserSummary>>(`/api/users?${toQueryString(params)}`, { signal });
  },

  invite(input: InviteUserInput): Promise<UserSummary> {
    return api<UserSummary>("/api/users/invitations", { method: "POST", body: input });
  },

  resendInvitation(id: string): Promise<void> {
    return api<void>(userPath(id, "resend-invitation"), { method: "POST" });
  },

  updateRoles(id: string, roleIds: string[]): Promise<UserSummary> {
    return api<UserSummary>(userPath(id, "roles"), { method: "PUT", body: { roleIds } });
  },

  deactivate(id: string): Promise<void> {
    return api<void>(userPath(id, "deactivate"), { method: "POST" });
  },

  activate(id: string): Promise<void> {
    return api<void>(userPath(id, "activate"), { method: "POST" });
  },
};

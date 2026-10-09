import { api } from "@/lib/api/client";
import type { Permission, Role } from "@/types/api";

export type SaveRoleInput = {
  name: string;
  description: string | null;
  permissions: string[];
};

function rolePath(id: string): string {
  return `/api/roles/${encodeURIComponent(id)}`;
}

export const rolesApi = {
  list(signal?: AbortSignal): Promise<Role[]> {
    return api<Role[]>("/api/roles", { signal });
  },

  permissions(signal?: AbortSignal): Promise<Permission[]> {
    return api<Permission[]>("/api/roles/permissions", { signal });
  },

  create(input: SaveRoleInput): Promise<Role> {
    return api<Role>("/api/roles", { method: "POST", body: input });
  },

  update(id: string, input: SaveRoleInput): Promise<Role> {
    return api<Role>(rolePath(id), { method: "PUT", body: input });
  },

  remove(id: string): Promise<void> {
    return api<void>(rolePath(id), { method: "DELETE" });
  },
};

import { api } from "@/lib/api/client";
import type { AuditAction, AuditLogEntry, PagedResult } from "@/types/api";

export type AuditLogParams = {
  page: number;
  pageSize: number;
  entityName?: string;
  action?: AuditAction;
  search?: string;
  from?: string;
  to?: string;
};

export const auditLogsApi = {
  list(params: AuditLogParams, signal?: AbortSignal): Promise<PagedResult<AuditLogEntry>> {
    const query = new URLSearchParams({ page: String(params.page), pageSize: String(params.pageSize) });

    for (const key of ["entityName", "action", "search", "from", "to"] as const) {
      const value = params[key];

      if (value) {
        query.set(key, value);
      }
    }

    return api<PagedResult<AuditLogEntry>>(`/api/audit-logs?${query}`, { signal });
  },
};

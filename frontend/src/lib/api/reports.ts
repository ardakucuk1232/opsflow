import { api } from "@/lib/api/client";
import type { MySummary, ReportOverview } from "@/types/api";

export const reportsApi = {
  overview(params: { projectId?: string; today: string }, signal?: AbortSignal): Promise<ReportOverview> {
    const query = new URLSearchParams({ today: params.today });

    if (params.projectId) {
      query.set("projectId", params.projectId);
    }

    return api<ReportOverview>(`/api/reports/overview?${query}`, { signal });
  },

  me(today: string, signal?: AbortSignal): Promise<MySummary> {
    return api<MySummary>(`/api/reports/me?${new URLSearchParams({ today })}`, { signal });
  },
};

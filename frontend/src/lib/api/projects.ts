import { api } from "@/lib/api/client";
import type {
  PagedResult,
  ProjectDetail,
  ProjectMemberRole,
  ProjectStatus,
  ProjectSummary,
} from "@/types/api";

export type ProjectListParams = {
  page: number;
  pageSize: number;
  search?: string;
  status?: ProjectStatus;
  memberOnly?: boolean;
};

export type CreateProjectInput = {
  name: string;
  key: string;
  description: string | null;
  status: ProjectStatus;
  startDate: string | null;
  endDate: string | null;
};

export type UpdateProjectInput = Omit<CreateProjectInput, "key">;

function projectPath(id: string, suffix = ""): string {
  return `/api/projects/${encodeURIComponent(id)}${suffix}`;
}

function memberPath(projectId: string, userId: string): string {
  return projectPath(projectId, `/members/${encodeURIComponent(userId)}`);
}

export const projectsApi = {
  list(params: ProjectListParams, signal?: AbortSignal): Promise<PagedResult<ProjectSummary>> {
    const query = new URLSearchParams({ page: String(params.page), pageSize: String(params.pageSize) });

    if (params.search) {
      query.set("search", params.search);
    }

    if (params.status) {
      query.set("status", params.status);
    }

    if (params.memberOnly) {
      query.set("memberOnly", "true");
    }

    return api<PagedResult<ProjectSummary>>(`/api/projects?${query.toString()}`, { signal });
  },

  get(id: string, signal?: AbortSignal): Promise<ProjectDetail> {
    return api<ProjectDetail>(projectPath(id), { signal });
  },

  create(input: CreateProjectInput): Promise<ProjectDetail> {
    return api<ProjectDetail>("/api/projects", { method: "POST", body: input });
  },

  update(id: string, input: UpdateProjectInput): Promise<ProjectDetail> {
    return api<ProjectDetail>(projectPath(id), { method: "PUT", body: input });
  },

  remove(id: string): Promise<void> {
    return api<void>(projectPath(id), { method: "DELETE" });
  },

  addMember(id: string, userId: string, role: ProjectMemberRole): Promise<ProjectDetail> {
    return api<ProjectDetail>(projectPath(id, "/members"), { method: "POST", body: { userId, role } });
  },

  updateMember(id: string, userId: string, role: ProjectMemberRole): Promise<ProjectDetail> {
    return api<ProjectDetail>(memberPath(id, userId), { method: "PUT", body: { role } });
  },

  removeMember(id: string, userId: string): Promise<void> {
    return api<void>(memberPath(id, userId), { method: "DELETE" });
  },
};

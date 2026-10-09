import { api } from "@/lib/api/client";
import type {
  AssignedTask,
  TaskComment,
  TaskDetail,
  TaskPriority,
  TaskStatus,
  TaskSummary,
} from "@/types/api";

export type CreateTaskInput = {
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  assigneeId: string | null;
  dueDate: string | null;
};

export type UpdateTaskInput = {
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueDate: string | null;
};

function taskPath(id: string, suffix = ""): string {
  return `/api/tasks/${encodeURIComponent(id)}${suffix}`;
}

export const tasksApi = {
  listForProject(projectId: string, signal?: AbortSignal): Promise<TaskSummary[]> {
    return api<TaskSummary[]>(`/api/projects/${encodeURIComponent(projectId)}/tasks`, { signal });
  },

  assignedToMe(signal?: AbortSignal): Promise<AssignedTask[]> {
    return api<AssignedTask[]>("/api/tasks/assigned-to-me", { signal });
  },

  get(id: string, signal?: AbortSignal): Promise<TaskDetail> {
    return api<TaskDetail>(taskPath(id), { signal });
  },

  create(projectId: string, input: CreateTaskInput): Promise<TaskDetail> {
    return api<TaskDetail>(`/api/projects/${encodeURIComponent(projectId)}/tasks`, { method: "POST", body: input });
  },

  update(id: string, input: UpdateTaskInput): Promise<TaskDetail> {
    return api<TaskDetail>(taskPath(id), { method: "PUT", body: input });
  },

  assign(id: string, assigneeId: string | null): Promise<TaskDetail> {
    return api<TaskDetail>(taskPath(id, "/assignee"), { method: "PUT", body: { assigneeId } });
  },

  move(id: string, status: TaskStatus, position: number): Promise<TaskDetail> {
    return api<TaskDetail>(taskPath(id, "/move"), { method: "POST", body: { status, position } });
  },

  remove(id: string): Promise<void> {
    return api<void>(taskPath(id), { method: "DELETE" });
  },

  comments(id: string, signal?: AbortSignal): Promise<TaskComment[]> {
    return api<TaskComment[]>(taskPath(id, "/comments"), { signal });
  },

  addComment(id: string, body: string): Promise<TaskComment> {
    return api<TaskComment>(taskPath(id, "/comments"), { method: "POST", body: { body } });
  },

  deleteComment(id: string, commentId: string): Promise<void> {
    return api<void>(taskPath(id, `/comments/${encodeURIComponent(commentId)}`), { method: "DELETE" });
  },
};

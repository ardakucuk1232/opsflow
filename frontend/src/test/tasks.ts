import type { TaskDetail, TaskSummary } from "@/types/api";
import { TEST_USER } from "@/test/fixtures";

export function taskSummary(overrides: Partial<TaskSummary>): TaskSummary {
  return {
    id: "task-1",
    projectId: "project-1",
    key: "WEB-1",
    number: 1,
    title: "Ana sayfa tasarımı",
    status: "Todo",
    priority: "Medium",
    assignee: null,
    dueDate: null,
    boardOrder: 0,
    commentCount: 0,
    createdAt: "2026-10-01T09:00:00Z",
    ...overrides,
  };
}

export const TASK_DETAIL: TaskDetail = {
  id: "task-1",
  projectId: "project-1",
  projectKey: "WEB",
  projectName: "Web sitesi",
  key: "WEB-1",
  number: 1,
  title: "Ana sayfa tasarımı",
  description: "Yeni tasarımı uygula",
  status: "Todo",
  priority: "Medium",
  assignee: null,
  reporter: { id: TEST_USER.id, firstName: "Arda", lastName: "Küçük", email: TEST_USER.email },
  dueDate: "2026-10-20",
  completedAt: null,
  createdAt: "2026-10-01T09:00:00Z",
  updatedAt: null,
};

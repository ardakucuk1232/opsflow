import type { TaskListParams } from "@/lib/api/tasks";
import { TASK_PRIORITIES, TASK_STATUSES } from "@/lib/i18n/tasks";
import type { TaskPriority, TaskSort, TaskStatus } from "@/types/api";

export const TASK_PAGE_SIZE = 20;

export const OPEN_STATUSES: TaskStatus[] = ["Backlog", "Todo", "InProgress", "InReview"];

export const TASK_SORTS: TaskSort[] = ["Newest", "Oldest", "DueDate", "Priority", "Updated"];

export type StatusFilter = "" | "open" | TaskStatus;
export type AssigneeFilter = "" | "me" | "none";
export type DueFilter = "" | "overdue" | "week";

export type TaskFilters = {
  q: string;
  project: string;
  status: StatusFilter;
  priority: "" | TaskPriority;
  assignee: AssigneeFilter;
  due: DueFilter;
  sort: TaskSort;
  page: number;
};

export const DEFAULT_TASK_FILTERS: TaskFilters = {
  q: "",
  project: "",
  status: "",
  priority: "",
  assignee: "",
  due: "",
  sort: "Newest",
  page: 1,
};

function oneOf<T extends string>(value: string | null, allowed: readonly T[], fallback: T): T {
  return value !== null && (allowed as readonly string[]).includes(value) ? (value as T) : fallback;
}

export function readTaskFilters(params: URLSearchParams): TaskFilters {
  const page = Number.parseInt(params.get("page") ?? "", 10);

  return {
    q: (params.get("q") ?? "").slice(0, 100),
    project: params.get("project") ?? "",
    status: oneOf<StatusFilter>(params.get("status"), ["open", ...TASK_STATUSES], ""),
    priority: oneOf<"" | TaskPriority>(params.get("priority"), TASK_PRIORITIES, ""),
    assignee: oneOf<AssigneeFilter>(params.get("assignee"), ["me", "none"], ""),
    due: oneOf<DueFilter>(params.get("due"), ["overdue", "week"], ""),
    sort: oneOf<TaskSort>(params.get("sort"), TASK_SORTS, "Newest"),
    page: Number.isFinite(page) && page > 1 ? page : 1,
  };
}

export function writeTaskFilters(filters: TaskFilters): string {
  const query = new URLSearchParams();

  for (const key of ["q", "project", "status", "priority", "assignee", "due", "sort", "page"] as const) {
    const value = filters[key];

    if (value !== DEFAULT_TASK_FILTERS[key] && value !== "") {
      query.set(key, String(value));
    }
  }

  return query.toString();
}

export function hasActiveFilters(filters: TaskFilters): boolean {
  return writeTaskFilters({ ...filters, sort: DEFAULT_TASK_FILTERS.sort, page: 1 }) !== "";
}

export function addDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split("-").map(Number);
  const date = new Date(Date.UTC(year, month - 1, day + days));

  return date.toISOString().slice(0, 10);
}

export function toTaskListParams(filters: TaskFilters, userId: string, today: string): TaskListParams {
  const params: TaskListParams = { page: filters.page, pageSize: TASK_PAGE_SIZE, sort: filters.sort };

  if (filters.q.trim()) {
    params.search = filters.q.trim();
  }

  if (filters.project) {
    params.projectId = filters.project;
  }

  if (filters.status === "open") {
    params.status = OPEN_STATUSES;
  } else if (filters.status) {
    params.status = [filters.status];
  }

  if (filters.priority) {
    params.priority = [filters.priority];
  }

  if (filters.assignee === "me") {
    params.assigneeId = userId;
  } else if (filters.assignee === "none") {
    params.unassigned = true;
  }

  if (filters.due === "overdue") {
    params.dueTo = addDays(today, -1);
    params.status ??= OPEN_STATUSES;
  } else if (filters.due === "week") {
    params.dueFrom = today;
    params.dueTo = addDays(today, 6);
  }

  return params;
}

export function isOverdue(task: { dueDate: string | null; status: TaskStatus }, today: string): boolean {
  return task.dueDate !== null && task.dueDate < today && OPEN_STATUSES.includes(task.status);
}

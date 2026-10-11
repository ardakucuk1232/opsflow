import { describe, expect, it } from "vitest";
import {
  addDays,
  DEFAULT_TASK_FILTERS,
  hasActiveFilters,
  isOverdue,
  OPEN_STATUSES,
  readTaskFilters,
  toTaskListParams,
  writeTaskFilters,
} from "@/lib/tasks/task-filters";
import { taskListQuery } from "@/lib/api/tasks";

describe("task filters in the URL", () => {
  it("reads known values and ignores unknown ones", () => {
    const filters = readTaskFilters(new URLSearchParams("q=rapor&status=open&priority=Huge&assignee=me&sort=DueDate&page=3"));

    expect(filters).toEqual({ ...DEFAULT_TASK_FILTERS, q: "rapor", status: "open", assignee: "me", sort: "DueDate", page: 3 });
  });

  it("writes only values that differ from the defaults", () => {
    expect(writeTaskFilters(DEFAULT_TASK_FILTERS)).toBe("");
    expect(writeTaskFilters({ ...DEFAULT_TASK_FILTERS, q: "giriş", due: "overdue", page: 2 })).toBe("q=giri%C5%9F&due=overdue&page=2");
  });

  it("does not count sorting or paging as a filter", () => {
    expect(hasActiveFilters({ ...DEFAULT_TASK_FILTERS, sort: "Priority", page: 4 })).toBe(false);
    expect(hasActiveFilters({ ...DEFAULT_TASK_FILTERS, priority: "High" })).toBe(true);
  });
});

describe("toTaskListParams", () => {
  const today = "2026-10-11";

  it("maps presets to API filters", () => {
    expect(toTaskListParams({ ...DEFAULT_TASK_FILTERS, status: "open", assignee: "me" }, "user-1", today)).toMatchObject({
      status: OPEN_STATUSES,
      assigneeId: "user-1",
    });
    expect(toTaskListParams({ ...DEFAULT_TASK_FILTERS, assignee: "none" }, "user-1", today)).toMatchObject({ unassigned: true });
  });

  it("treats overdue as open tasks due before today", () => {
    expect(toTaskListParams({ ...DEFAULT_TASK_FILTERS, due: "overdue" }, "u", today)).toMatchObject({
      dueTo: "2026-10-10",
      status: OPEN_STATUSES,
    });
    expect(toTaskListParams({ ...DEFAULT_TASK_FILTERS, due: "overdue", status: "InReview" }, "u", today)).toMatchObject({
      status: ["InReview"],
    });
  });

  it("covers the next seven days for this week", () => {
    expect(toTaskListParams({ ...DEFAULT_TASK_FILTERS, due: "week" }, "u", today)).toMatchObject({
      dueFrom: "2026-10-11",
      dueTo: "2026-10-17",
    });
  });

  it("builds a query string with repeated values", () => {
    expect(taskListQuery({ page: 1, pageSize: 20, status: ["Todo", "InProgress"], unassigned: true, search: "a b" })).toBe(
      "page=1&pageSize=20&search=a+b&status=Todo&status=InProgress&unassigned=true",
    );
  });
});

describe("dates", () => {
  it("adds days across month boundaries", () => {
    expect(addDays("2026-10-31", 1)).toBe("2026-11-01");
    expect(addDays("2026-03-01", -1)).toBe("2026-02-28");
  });

  it("marks only open tasks with a past due date as overdue", () => {
    expect(isOverdue({ dueDate: "2026-10-10", status: "Todo" }, "2026-10-11")).toBe(true);
    expect(isOverdue({ dueDate: "2026-10-10", status: "Done" }, "2026-10-11")).toBe(false);
    expect(isOverdue({ dueDate: "2026-10-11", status: "Todo" }, "2026-10-11")).toBe(false);
    expect(isOverdue({ dueDate: null, status: "Todo" }, "2026-10-11")).toBe(false);
  });
});

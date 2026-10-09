import { describe, expect, it } from "vitest";
import { columnOf, isOverdue, moveTask, todayIso } from "@/lib/tasks/board";
import { taskSummary } from "@/test/tasks";

const TASKS = [
  taskSummary({ id: "a", title: "A", status: "Todo", boardOrder: 0, number: 1 }),
  taskSummary({ id: "b", title: "B", status: "Todo", boardOrder: 1, number: 2 }),
  taskSummary({ id: "c", title: "C", status: "Todo", boardOrder: 2, number: 3 }),
  taskSummary({ id: "d", title: "D", status: "InProgress", boardOrder: 0, number: 4 }),
];

function titles(tasks: typeof TASKS, status: "Todo" | "InProgress") {
  return columnOf(tasks, status).map((task) => task.title);
}

describe("board helpers", () => {
  it("orders a column by board order and then by number", () => {
    const tied = [taskSummary({ id: "y", title: "Y", number: 9 }), taskSummary({ id: "x", title: "X", number: 2 })];

    expect(columnOf(tied, "Todo").map((task) => task.title)).toEqual(["X", "Y"]);
  });

  it("moves a task up inside its column", () => {
    expect(titles(moveTask(TASKS, "c", "Todo", 0), "Todo")).toEqual(["C", "A", "B"]);
  });

  it("moves a task into another column at the requested position", () => {
    const moved = moveTask(TASKS, "a", "InProgress", 0);

    expect(titles(moved, "Todo")).toEqual(["B", "C"]);
    expect(titles(moved, "InProgress")).toEqual(["A", "D"]);
  });

  it("clamps a position past the end of the column", () => {
    expect(titles(moveTask(TASKS, "a", "InProgress", 99), "InProgress")).toEqual(["D", "A"]);
  });

  it("treats an open task with a past due date as overdue", () => {
    expect(isOverdue("2026-10-01", "Todo", "2026-10-09")).toBe(true);
    expect(isOverdue("2026-10-09", "Todo", "2026-10-09")).toBe(false);
    expect(isOverdue("2026-10-01", "Done", "2026-10-09")).toBe(false);
    expect(isOverdue(null, "Todo", "2026-10-09")).toBe(false);
  });

  it("formats today as an ISO date in local time", () => {
    expect(todayIso(new Date(2026, 0, 5, 23, 30))).toBe("2026-01-05");
  });
});

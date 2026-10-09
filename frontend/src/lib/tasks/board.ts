import type { TaskStatus, TaskSummary } from "@/types/api";

export function columnOf(tasks: TaskSummary[], status: TaskStatus): TaskSummary[] {
  return tasks
    .filter((task) => task.status === status)
    .sort((left, right) => left.boardOrder - right.boardOrder || left.number - right.number);
}

export function moveTask(tasks: TaskSummary[], taskId: string, status: TaskStatus, position: number): TaskSummary[] {
  const moving = tasks.find((task) => task.id === taskId);

  if (!moving) {
    return tasks;
  }

  const target = columnOf(tasks, status).filter((task) => task.id !== taskId);
  const index = Math.max(0, Math.min(position, target.length));

  target.splice(index, 0, { ...moving, status });

  const reordered = new Map(target.map((task, order) => [task.id, { ...task, boardOrder: order }]));

  return tasks.map((task) => reordered.get(task.id) ?? task);
}

export function isOverdue(dueDate: string | null, status: TaskStatus, today: string): boolean {
  return dueDate !== null && status !== "Done" && status !== "Cancelled" && dueDate < today;
}

export function todayIso(now = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");

  return `${now.getFullYear()}-${month}-${day}`;
}

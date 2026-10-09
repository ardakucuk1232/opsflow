"use client";

import { CalendarDays, MessageSquare } from "lucide-react";
import type { DragEvent } from "react";
import { TaskPriorityBadge } from "@/components/tasks/task-badges";
import { getInitials } from "@/lib/auth/roles";
import { isOverdue } from "@/lib/tasks/board";
import { cn } from "@/lib/utils/cn";
import { formatDateOnly } from "@/lib/utils/format";
import type { TaskSummary } from "@/types/api";

type TaskCardProps = {
  task: TaskSummary;
  today: string;
  draggable: boolean;
  onOpen: () => void;
  onDragStart: (event: DragEvent<HTMLElement>) => void;
  onDragOver: (event: DragEvent<HTMLElement>) => void;
  onDrop: (event: DragEvent<HTMLElement>) => void;
};

export function TaskCard({ task, today, draggable, onOpen, onDragStart, onDragOver, onDrop }: TaskCardProps) {
  const overdue = isOverdue(task.dueDate, task.status, today);

  return (
    <article
      draggable={draggable}
      onDragStart={onDragStart}
      onDragOver={onDragOver}
      onDrop={onDrop}
      data-task-id={task.id}
      className={cn(
        "rounded-lg border border-border bg-surface p-3 shadow-xs transition-shadow hover:border-border-strong",
        draggable && "cursor-grab active:cursor-grabbing",
      )}
    >
      <button
        type="button"
        onClick={onOpen}
        className="block w-full rounded text-left focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
      >
        <span className="font-mono text-xs font-semibold text-muted">{task.key}</span>
        <span className="mt-1 block text-sm font-medium leading-snug text-foreground">{task.title}</span>
      </button>
      <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-muted">
        <TaskPriorityBadge priority={task.priority} />
        {task.dueDate ? (
          <span className={cn("flex items-center gap-1", overdue && "font-medium text-danger")}>
            <CalendarDays className="size-3.5" aria-hidden="true" />
            {formatDateOnly(task.dueDate)}
            {overdue ? <span className="sr-only">(gecikmiş)</span> : null}
          </span>
        ) : null}
        {task.commentCount > 0 ? (
          <span className="flex items-center gap-1">
            <MessageSquare className="size-3.5" aria-hidden="true" />
            {task.commentCount}
            <span className="sr-only">yorum</span>
          </span>
        ) : null}
        <span className="ml-auto">
          {task.assignee ? (
            <span
              title={`${task.assignee.firstName} ${task.assignee.lastName}`}
              className="flex size-6 items-center justify-center rounded-full bg-primary-soft text-[10px] font-semibold text-primary"
            >
              {getInitials(task.assignee.firstName, task.assignee.lastName)}
              <span className="sr-only">
                Atanan: {task.assignee.firstName} {task.assignee.lastName}
              </span>
            </span>
          ) : (
            <span className="text-muted">Atanmamış</span>
          )}
        </span>
      </div>
    </article>
  );
}

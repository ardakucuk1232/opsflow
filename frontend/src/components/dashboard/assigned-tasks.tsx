"use client";

import { CalendarDays, CheckCircle2 } from "lucide-react";
import Link from "next/link";
import { useMemo } from "react";
import { TaskPriorityBadge, TaskStatusBadge } from "@/components/tasks/task-badges";
import { Alert } from "@/components/ui/alert";
import { Spinner } from "@/components/ui/spinner";
import { tasksApi } from "@/lib/api/tasks";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { isOverdue, todayIso } from "@/lib/tasks/board";
import { cn } from "@/lib/utils/cn";
import { formatDateOnly } from "@/lib/utils/format";

export function AssignedTasks() {
  const tasks = useApiData(tasksApi.assignedToMe);
  const today = useMemo(() => todayIso(), []);

  return (
    <section aria-labelledby="assigned-heading" className="rounded-xl border border-border bg-surface shadow-xs">
      <div className="border-b border-border px-5 py-4">
        <h2 id="assigned-heading" className="text-base font-medium text-foreground">
          Bana atanan görevler
        </h2>
      </div>

      {tasks.error ? (
        <div className="p-5">
          <Alert>{getErrorMessage(tasks.error)}</Alert>
        </div>
      ) : null}

      {tasks.data === undefined && tasks.loading ? (
        <div className="flex justify-center py-10" role="status">
          <Spinner className="size-5 text-primary" />
          <span className="sr-only">Görevler yükleniyor</span>
        </div>
      ) : null}

      {tasks.data && tasks.data.length === 0 ? (
        <div className="flex flex-col items-center px-6 py-10 text-center">
          <CheckCircle2 className="size-7 text-success" aria-hidden="true" />
          <p className="mt-3 text-sm text-muted">Size atanmış açık görev yok.</p>
        </div>
      ) : null}

      {tasks.data && tasks.data.length > 0 ? (
        <ul className="divide-y divide-border">
          {tasks.data.map((task) => {
            const overdue = isOverdue(task.dueDate, task.status, today);

            return (
              <li key={task.id}>
                <Link
                  href={`/projects/${task.projectId}?task=${task.id}`}
                  className="flex flex-col gap-2 px-5 py-3 transition-colors hover:bg-surface-muted focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-foreground">
                      <span className="mr-2 font-mono text-xs text-muted">{task.key}</span>
                      {task.title}
                    </p>
                    <p className="truncate text-xs text-muted">{task.projectName}</p>
                  </div>
                  <div className="flex shrink-0 flex-wrap items-center gap-2 text-xs">
                    <TaskStatusBadge status={task.status} />
                    <TaskPriorityBadge priority={task.priority} />
                    {task.dueDate ? (
                      <span className={cn("flex items-center gap-1 text-muted", overdue && "font-medium text-danger")}>
                        <CalendarDays className="size-3.5" aria-hidden="true" />
                        {formatDateOnly(task.dueDate)}
                        {overdue ? " · gecikti" : null}
                      </span>
                    ) : null}
                  </div>
                </Link>
              </li>
            );
          })}
        </ul>
      ) : null}
    </section>
  );
}

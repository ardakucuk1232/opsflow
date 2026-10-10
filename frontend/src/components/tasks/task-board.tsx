"use client";

import { Plus, Search } from "lucide-react";
import { useCallback, useMemo, useRef, useState, type DragEvent } from "react";
import { CreateTaskDialog } from "@/components/tasks/create-task-dialog";
import { TaskCard } from "@/components/tasks/task-card";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { tasksApi } from "@/lib/api/tasks";
import { useAuth } from "@/lib/auth/auth-provider";
import { hasPermission, PERMISSIONS } from "@/lib/auth/permissions";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { useProjectChanges } from "@/lib/realtime/realtime-provider";
import { BOARD_STATUSES, getTaskStatusLabel } from "@/lib/i18n/tasks";
import { columnOf, moveTask, todayIso } from "@/lib/tasks/board";
import { cn } from "@/lib/utils/cn";
import type { ProjectDetail, TaskStatus, TaskSummary } from "@/types/api";

type TaskBoardProps = {
  project: ProjectDetail;
  refreshKey: number;
  onOpenTask: (taskId: string) => void;
  onChanged: () => void;
};

type Optimistic = { basedOn: TaskSummary[] | undefined; tasks: TaskSummary[] };

export function TaskBoard({ project, refreshKey, onOpenTask, onChanged }: TaskBoardProps) {
  const { user } = useAuth();
  const canCreate = hasPermission(user, PERMISSIONS.taskCreate);
  const canMove = hasPermission(user, PERMISSIONS.taskUpdate);

  const loadTasks = useCallback(
    (signal: AbortSignal) => {
      void refreshKey;
      return tasksApi.listForProject(project.id, signal);
    },
    [project.id, refreshKey],
  );
  const board = useApiData(loadTasks);

  useProjectChanges(project.id, board.reload);

  const [optimistic, setOptimistic] = useState<Optimistic | null>(null);
  const [search, setSearch] = useState("");
  const [assignee, setAssignee] = useState("all");
  const [showCancelled, setShowCancelled] = useState(false);
  const [creating, setCreating] = useState(false);
  const [moveError, setMoveError] = useState<string | null>(null);
  const [dropTarget, setDropTarget] = useState<TaskStatus | null>(null);
  const dragged = useRef<string | null>(null);

  const tasks = optimistic && optimistic.basedOn === board.data ? optimistic.tasks : (board.data ?? []);
  const today = useMemo(() => todayIso(), []);
  const statuses = showCancelled ? [...BOARD_STATUSES, "Cancelled" as const] : BOARD_STATUSES;

  const term = search.trim().toLocaleLowerCase("tr-TR");
  const visible = tasks.filter((task) => {
    if (term && !`${task.key} ${task.title}`.toLocaleLowerCase("tr-TR").includes(term)) {
      return false;
    }

    if (assignee === "me") {
      return task.assignee?.id === user?.id;
    }

    if (assignee === "none") {
      return task.assignee === null;
    }

    return assignee === "all" || task.assignee?.id === assignee;
  });

  async function drop(status: TaskStatus, beforeTaskId: string | null) {
    const taskId = dragged.current;
    dragged.current = null;
    setDropTarget(null);

    if (!taskId) {
      return;
    }

    const column = columnOf(tasks, status).filter((task) => task.id !== taskId);
    const index = beforeTaskId ? column.findIndex((task) => task.id === beforeTaskId) : -1;
    const position = index === -1 ? column.length : index;

    setMoveError(null);
    setOptimistic({ basedOn: board.data, tasks: moveTask(tasks, taskId, status, position) });

    try {
      await tasksApi.move(taskId, status, position);
      onChanged();
    } catch (error) {
      setOptimistic(null);
      setMoveError(getErrorMessage(error));
    }
  }

  function allowDrop(event: DragEvent<HTMLElement>, status: TaskStatus) {
    if (!dragged.current) {
      return;
    }

    event.preventDefault();

    if (dropTarget !== status) {
      setDropTarget(status);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
        <div className="relative lg:w-72">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" aria-hidden="true" />
          <Input
            type="search"
            aria-label="Görev ara"
            placeholder="Görev ara"
            className="pl-9"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        <Select aria-label="Atanan kişiye göre filtrele" className="lg:w-56" value={assignee} onChange={(event) => setAssignee(event.target.value)}>
          <option value="all">Herkes</option>
          <option value="me">Bana atananlar</option>
          <option value="none">Atanmamış</option>
          {project.members.map((member) => (
            <option key={member.userId} value={member.userId}>
              {member.firstName} {member.lastName}
            </option>
          ))}
        </Select>
        <Checkbox
          label="İptal edilenleri göster"
          className="shrink-0"
          checked={showCancelled}
          onChange={(event) => setShowCancelled(event.target.checked)}
        />
        {canCreate ? (
          <Button className="lg:ml-auto" onClick={() => setCreating(true)}>
            <Plus className="size-4" aria-hidden="true" />
            Yeni görev
          </Button>
        ) : null}
      </div>

      {moveError ? <Alert>{moveError}</Alert> : null}
      {board.error ? <Alert>{getErrorMessage(board.error)}</Alert> : null}

      {board.data === undefined && board.loading ? (
        <div className="flex justify-center py-16" role="status">
          <Spinner className="size-5 text-primary" />
          <span className="sr-only">Görevler yükleniyor</span>
        </div>
      ) : (
        <div className="relative overflow-x-auto pb-2">
          <div className="grid auto-cols-[minmax(200px,1fr)] grid-flow-col gap-4">
            {statuses.map((status) => {
              const column = columnOf(visible, status);

              return (
                <section
                  key={status}
                  aria-label={getTaskStatusLabel(status)}
                  data-status={status}
                  onDragOver={(event) => allowDrop(event, status)}
                  onDragLeave={() => setDropTarget((current) => (current === status ? null : current))}
                  onDrop={(event) => {
                    event.preventDefault();
                    void drop(status, null);
                  }}
                  className={cn(
                    "flex min-h-48 flex-col rounded-xl bg-surface-muted p-2.5 transition-colors",
                    dropTarget === status && "ring-2 ring-ring",
                  )}
                >
                  <h3 className="flex items-center justify-between px-1.5 pb-2.5 pt-1 text-xs font-semibold uppercase tracking-wide text-muted">
                    {getTaskStatusLabel(status)}
                    <span className="rounded-full bg-surface px-2 py-0.5 text-[11px] font-medium normal-case tracking-normal">
                      {column.length}
                    </span>
                  </h3>
                  <div className="flex flex-1 flex-col gap-2">
                    {column.map((task) => (
                      <TaskCard
                        key={task.id}
                        task={task}
                        today={today}
                        draggable={canMove}
                        onOpen={() => onOpenTask(task.id)}
                        onDragStart={(event) => {
                          dragged.current = task.id;
                          event.dataTransfer.effectAllowed = "move";
                          event.dataTransfer.setData("text/plain", task.id);
                        }}
                        onDragOver={(event) => allowDrop(event, status)}
                        onDrop={(event) => {
                          event.preventDefault();
                          event.stopPropagation();
                          void drop(status, task.id);
                        }}
                      />
                    ))}
                  </div>
                </section>
              );
            })}
          </div>
        </div>
      )}

      <CreateTaskDialog
        open={creating}
        project={project}
        onClose={() => setCreating(false)}
        onCreated={() => {
          setCreating(false);
          onChanged();
        }}
      />
    </div>
  );
}

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Trash2 } from "lucide-react";
import { useCallback, useState } from "react";
import { useForm } from "react-hook-form";
import { TaskAttachments } from "@/components/tasks/task-attachments";
import { TaskComments } from "@/components/tasks/task-comments";
import { TaskPriorityBadge, TaskStatusBadge } from "@/components/tasks/task-badges";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { FormField, describedBy } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { Textarea } from "@/components/ui/textarea";
import { ApiError } from "@/lib/api/errors";
import { tasksApi } from "@/lib/api/tasks";
import { PERMISSIONS } from "@/lib/auth/permissions";
import { usePermission } from "@/lib/auth/use-permission";
import { taskSchema, type TaskFormValues } from "@/lib/auth/schemas";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import {
  getTaskPriorityLabel,
  getTaskStatusLabel,
  TASK_PRIORITIES,
  TASK_STATUSES,
} from "@/lib/i18n/tasks";
import { formatDate, formatDateOnly } from "@/lib/utils/format";
import type { ProjectDetail, TaskDetail, TaskStatus } from "@/types/api";

const END_OF_COLUMN = 1_000_000;

type TaskDialogProps = {
  taskId: string | null;
  project: ProjectDetail;
  onClose: () => void;
  onChanged: () => void;
};

export function TaskDialog({ taskId, project, onClose, onChanged }: TaskDialogProps) {
  return (
    <Dialog open={taskId !== null} onClose={onClose} title="Görev ayrıntıları" size="lg">
      {taskId ? (
        <TaskDetails key={taskId} taskId={taskId} project={project} onChanged={onChanged} onDeleted={onClose} />
      ) : null}
    </Dialog>
  );
}

function TaskDetails({
  taskId,
  project,
  onChanged,
  onDeleted,
}: {
  taskId: string;
  project: ProjectDetail;
  onChanged: () => void;
  onDeleted: () => void;
}) {
  const canUpdate = usePermission(PERMISSIONS.taskUpdate);
  const canAssign = usePermission(PERMISSIONS.taskAssign);
  const canDelete = usePermission(PERMISSIONS.taskDelete);
  const loadTask = useCallback((signal: AbortSignal) => tasksApi.get(taskId, signal), [taskId]);
  const task = useApiData(loadTask);
  const [error, setError] = useState<string | null>(null);
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  if (task.error instanceof ApiError && task.error.status === 404) {
    return <Alert>Görev bulunamadı. Silinmiş olabilir.</Alert>;
  }

  if (task.error) {
    return <Alert>{getErrorMessage(task.error)}</Alert>;
  }

  if (!task.data) {
    return (
      <div className="flex justify-center py-10" role="status">
        <Spinner className="size-5 text-primary" />
        <span className="sr-only">Görev yükleniyor</span>
      </div>
    );
  }

  const data = task.data;

  async function run(action: () => Promise<unknown>) {
    setError(null);

    try {
      await action();
      task.reload();
      onChanged();
    } catch (exception) {
      setError(getErrorMessage(exception));
    }
  }

  async function handleDelete() {
    setError(null);

    try {
      await tasksApi.remove(data.id);
      onChanged();
      onDeleted();
    } catch (exception) {
      setError(getErrorMessage(exception));
    }
  }

  const members = project.members.filter((member) => member.isActive || member.userId === data.assignee?.id);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <p className="font-mono text-sm font-semibold text-muted">{data.key}</p>
        {canUpdate ? null : (
          <h3 className="mt-1 text-lg font-semibold text-foreground">{data.title}</h3>
        )}
      </div>

      {error ? <Alert>{error}</Alert> : null}

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField id="task-status-select" label="Durum">
          {canUpdate ? (
            <Select
              id="task-status-select"
              value={data.status}
              onChange={(event) =>
                void run(() => tasksApi.move(data.id, event.target.value as TaskStatus, END_OF_COLUMN))
              }
            >
              {TASK_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {getTaskStatusLabel(status)}
                </option>
              ))}
            </Select>
          ) : (
            <TaskStatusBadge status={data.status} />
          )}
        </FormField>
        <FormField id="task-assignee-select" label="Atanan kişi">
          {canAssign ? (
            <Select
              id="task-assignee-select"
              value={data.assignee?.id ?? ""}
              onChange={(event) => void run(() => tasksApi.assign(data.id, event.target.value || null))}
            >
              <option value="">Atanmamış</option>
              {members.map((member) => (
                <option key={member.userId} value={member.userId}>
                  {member.firstName} {member.lastName}
                </option>
              ))}
            </Select>
          ) : (
            <p className="text-sm text-foreground">
              {data.assignee ? `${data.assignee.firstName} ${data.assignee.lastName}` : "Atanmamış"}
            </p>
          )}
        </FormField>
      </div>

      {canUpdate ? (
        <TaskEditForm key={data.updatedAt ?? data.createdAt} task={data} onSubmit={(input) => run(() => tasksApi.update(data.id, input))} />
      ) : (
        <div className="flex flex-col gap-3 text-sm">
          <div className="flex flex-wrap items-center gap-2">
            <TaskPriorityBadge priority={data.priority} />
            {data.dueDate ? <span className="text-muted">Bitiş: {formatDateOnly(data.dueDate)}</span> : null}
          </div>
          <p className="whitespace-pre-line text-foreground">{data.description ?? "Açıklama eklenmemiş."}</p>
        </div>
      )}

      <dl className="grid gap-3 rounded-lg bg-surface-muted p-4 text-sm sm:grid-cols-3">
        <div>
          <dt className="text-xs text-muted">Oluşturan</dt>
          <dd className="text-foreground">
            {data.reporter.firstName} {data.reporter.lastName}
          </dd>
        </div>
        <div>
          <dt className="text-xs text-muted">Oluşturulma</dt>
          <dd className="text-foreground">{formatDate(data.createdAt)}</dd>
        </div>
        <div>
          <dt className="text-xs text-muted">Tamamlanma</dt>
          <dd className="text-foreground">{data.completedAt ? formatDate(data.completedAt) : "—"}</dd>
        </div>
      </dl>

      <TaskAttachments taskId={data.id} onChanged={onChanged} />

      <TaskComments taskId={data.id} onChanged={onChanged} />

      {canDelete ? (
        <div className="flex flex-wrap items-center justify-end gap-3 border-t border-border pt-5">
          {confirmingDelete ? (
            <>
              <span className="text-sm text-muted">Görev silinecek. Emin misiniz?</span>
              <Button variant="secondary" size="sm" onClick={() => setConfirmingDelete(false)}>
                Vazgeç
              </Button>
              <Button variant="danger" size="sm" onClick={() => void handleDelete()}>
                Görevi sil
              </Button>
            </>
          ) : (
            <Button variant="ghost" size="sm" onClick={() => setConfirmingDelete(true)}>
              <Trash2 className="size-4 text-danger" aria-hidden="true" />
              Sil
            </Button>
          )}
        </div>
      ) : null}
    </div>
  );
}

function TaskEditForm({
  task,
  onSubmit,
}: {
  task: TaskDetail;
  onSubmit: (input: { title: string; description: string | null; priority: TaskDetail["priority"]; dueDate: string | null }) => Promise<void>;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<TaskFormValues>({
    resolver: zodResolver(taskSchema),
    defaultValues: {
      title: task.title,
      description: task.description ?? "",
      status: task.status,
      priority: task.priority,
      assigneeId: task.assignee?.id ?? "",
      dueDate: task.dueDate ?? "",
    },
  });

  const submit = handleSubmit((values) =>
    onSubmit({
      title: values.title,
      description: values.description === "" ? null : values.description,
      priority: values.priority,
      dueDate: values.dueDate === "" ? null : values.dueDate,
    }),
  );

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-4">
      <FormField id="edit-task-title" label="Başlık" error={errors.title?.message}>
        <Input
          id="edit-task-title"
          autoComplete="off"
          invalid={Boolean(errors.title)}
          aria-describedby={describedBy("edit-task-title", errors.title?.message)}
          {...register("title")}
        />
      </FormField>
      <FormField id="edit-task-description" label="Açıklama" error={errors.description?.message}>
        <Textarea id="edit-task-description" rows={4} {...register("description")} />
      </FormField>
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField id="edit-task-priority" label="Öncelik">
          <Select id="edit-task-priority" {...register("priority")}>
            {TASK_PRIORITIES.map((priority) => (
              <option key={priority} value={priority}>
                {getTaskPriorityLabel(priority)}
              </option>
            ))}
          </Select>
        </FormField>
        <FormField id="edit-task-due" label="Bitiş tarihi" error={errors.dueDate?.message}>
          <Input id="edit-task-due" type="date" {...register("dueDate")} />
        </FormField>
      </div>
      <div className="flex justify-end">
        <Button type="submit" size="sm" loading={isSubmitting} disabled={!isDirty}>
          Değişiklikleri kaydet
        </Button>
      </div>
    </form>
  );
}

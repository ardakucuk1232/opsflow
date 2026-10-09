"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogActions } from "@/components/ui/dialog";
import { FormField, describedBy } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { tasksApi } from "@/lib/api/tasks";
import { usePermission } from "@/lib/auth/use-permission";
import { PERMISSIONS } from "@/lib/auth/permissions";
import { taskSchema, type TaskFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import {
  getTaskPriorityLabel,
  getTaskStatusLabel,
  TASK_PRIORITIES,
  TASK_STATUSES,
} from "@/lib/i18n/tasks";
import type { ProjectDetail, TaskDetail, TaskStatus } from "@/types/api";

type CreateTaskDialogProps = {
  open: boolean;
  project: ProjectDetail;
  initialStatus?: TaskStatus;
  onClose: () => void;
  onCreated: (task: TaskDetail) => void;
};

export function CreateTaskDialog({ open, project, initialStatus = "Todo", onClose, onCreated }: CreateTaskDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} title="Yeni görev" description={`${project.key} · ${project.name}`}>
      <CreateTaskForm project={project} initialStatus={initialStatus} onCancel={onClose} onCreated={onCreated} />
    </Dialog>
  );
}

function CreateTaskForm({
  project,
  initialStatus,
  onCancel,
  onCreated,
}: {
  project: ProjectDetail;
  initialStatus: TaskStatus;
  onCancel: () => void;
  onCreated: (task: TaskDetail) => void;
}) {
  const canAssign = usePermission(PERMISSIONS.taskAssign);
  const [formError, setFormError] = useState<string | null>(null);
  const assignableMembers = project.members.filter((member) => member.isActive);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<TaskFormValues>({
    resolver: zodResolver(taskSchema),
    defaultValues: {
      title: "",
      description: "",
      status: initialStatus,
      priority: "Medium",
      assigneeId: "",
      dueDate: "",
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      onCreated(
        await tasksApi.create(project.id, {
          title: values.title,
          description: values.description === "" ? null : values.description,
          status: values.status,
          priority: values.priority,
          assigneeId: values.assigneeId === "" ? null : values.assigneeId,
          dueDate: values.dueDate === "" ? null : values.dueDate,
        }),
      );
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      {formError ? <Alert>{formError}</Alert> : null}

      <FormField id="task-title" label="Başlık" error={errors.title?.message}>
        <Input
          id="task-title"
          autoComplete="off"
          invalid={Boolean(errors.title)}
          aria-describedby={describedBy("task-title", errors.title?.message)}
          {...register("title")}
        />
      </FormField>

      <FormField id="task-description" label="Açıklama (isteğe bağlı)" error={errors.description?.message}>
        <Textarea id="task-description" rows={4} invalid={Boolean(errors.description)} {...register("description")} />
      </FormField>

      <div className="grid gap-5 sm:grid-cols-2">
        <FormField id="task-status" label="Durum">
          <Select id="task-status" {...register("status")}>
            {TASK_STATUSES.map((status) => (
              <option key={status} value={status}>
                {getTaskStatusLabel(status)}
              </option>
            ))}
          </Select>
        </FormField>
        <FormField id="task-priority" label="Öncelik">
          <Select id="task-priority" {...register("priority")}>
            {TASK_PRIORITIES.map((priority) => (
              <option key={priority} value={priority}>
                {getTaskPriorityLabel(priority)}
              </option>
            ))}
          </Select>
        </FormField>
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        {canAssign ? (
          <FormField id="task-assignee" label="Atanan kişi">
            <Select id="task-assignee" {...register("assigneeId")}>
              <option value="">Atanmamış</option>
              {assignableMembers.map((member) => (
                <option key={member.userId} value={member.userId}>
                  {member.firstName} {member.lastName}
                </option>
              ))}
            </Select>
          </FormField>
        ) : null}
        <FormField id="task-due" label="Bitiş tarihi" error={errors.dueDate?.message}>
          <Input id="task-due" type="date" invalid={Boolean(errors.dueDate)} {...register("dueDate")} />
        </FormField>
      </div>

      <DialogActions>
        <Button variant="secondary" onClick={onCancel} disabled={isSubmitting}>
          Vazgeç
        </Button>
        <Button type="submit" loading={isSubmitting}>
          Görevi oluştur
        </Button>
      </DialogActions>
    </form>
  );
}

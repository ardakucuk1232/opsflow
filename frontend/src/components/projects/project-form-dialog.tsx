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
import { ApiError } from "@/lib/api/errors";
import { projectsApi } from "@/lib/api/projects";
import { createProjectSchema, type CreateProjectFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { getProjectStatusLabel, PROJECT_STATUSES } from "@/lib/i18n/projects";
import { suggestProjectKey } from "@/lib/utils/project-key";
import type { ProjectDetail } from "@/types/api";

const KEY_HINT = "Görev numaralarında kullanılır (ör. WEB-12). Sonradan değiştirilemez.";

type ProjectFormDialogProps = {
  open: boolean;
  project?: ProjectDetail;
  onClose: () => void;
  onSaved: (project: ProjectDetail) => void;
};

export function ProjectFormDialog({ open, project, onClose, onSaved }: ProjectFormDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={project ? "Projeyi düzenle" : "Yeni proje"}
      description={project ? undefined : "Projeyi oluşturan kişi otomatik olarak proje lideri olur."}
    >
      <ProjectForm project={project} onCancel={onClose} onSaved={onSaved} />
    </Dialog>
  );
}

function emptyToNull(value: string): string | null {
  return value === "" ? null : value;
}

function ProjectForm({
  project,
  onCancel,
  onSaved,
}: {
  project?: ProjectDetail;
  onCancel: () => void;
  onSaved: (project: ProjectDetail) => void;
}) {
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    getFieldState,
    formState: { errors, isSubmitting },
  } = useForm<CreateProjectFormValues>({
    resolver: zodResolver(createProjectSchema),
    defaultValues: {
      name: project?.name ?? "",
      key: project?.key ?? "",
      description: project?.description ?? "",
      status: project?.status ?? "Planning",
      startDate: project?.startDate ?? "",
      endDate: project?.endDate ?? "",
    },
  });

  const nameField = register("name", {
    onChange: (event: { target: { value: string } }) => {
      if (!project && !getFieldState("key").isDirty) {
        setValue("key", suggestProjectKey(event.target.value));
      }
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    const input = {
      name: values.name,
      description: emptyToNull(values.description),
      status: values.status,
      startDate: emptyToNull(values.startDate),
      endDate: emptyToNull(values.endDate),
    };

    try {
      onSaved(
        project
          ? await projectsApi.update(project.id, input)
          : await projectsApi.create({ ...input, key: values.key.toUpperCase() }),
      );
    } catch (error) {
      if (error instanceof ApiError && error.code === "projects.key_taken") {
        setError("key", { message: getErrorMessage(error) }, { shouldFocus: true });
        return;
      }

      setFormError(getErrorMessage(error));
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      {formError ? <Alert>{formError}</Alert> : null}

      <FormField id="project-name" label="Proje adı" error={errors.name?.message}>
        <Input
          id="project-name"
          autoComplete="off"
          invalid={Boolean(errors.name)}
          aria-describedby={describedBy("project-name", errors.name?.message)}
          {...nameField}
        />
      </FormField>

      <FormField id="project-key" label="Kısa ad" error={errors.key?.message} hint={KEY_HINT}>
        <Input
          id="project-key"
          autoComplete="off"
          className="uppercase read-only:bg-surface-muted read-only:text-muted"
          maxLength={10}
          readOnly={Boolean(project)}
          invalid={Boolean(errors.key)}
          aria-describedby={describedBy("project-key", errors.key?.message, KEY_HINT)}
          {...register("key")}
        />
      </FormField>

      <FormField id="project-description" label="Açıklama (isteğe bağlı)" error={errors.description?.message}>
        <Textarea
          id="project-description"
          rows={3}
          invalid={Boolean(errors.description)}
          aria-describedby={describedBy("project-description", errors.description?.message)}
          {...register("description")}
        />
      </FormField>

      <FormField id="project-status" label="Durum">
        <Select id="project-status" {...register("status")}>
          {PROJECT_STATUSES.map((status) => (
            <option key={status} value={status}>
              {getProjectStatusLabel(status)}
            </option>
          ))}
        </Select>
      </FormField>

      <div className="grid gap-5 sm:grid-cols-2">
        <FormField id="project-start" label="Başlangıç tarihi" error={errors.startDate?.message}>
          <Input id="project-start" type="date" invalid={Boolean(errors.startDate)} {...register("startDate")} />
        </FormField>
        <FormField id="project-end" label="Bitiş tarihi" error={errors.endDate?.message}>
          <Input
            id="project-end"
            type="date"
            invalid={Boolean(errors.endDate)}
            aria-describedby={describedBy("project-end", errors.endDate?.message)}
            {...register("endDate")}
          />
        </FormField>
      </div>

      <DialogActions>
        <Button variant="secondary" onClick={onCancel} disabled={isSubmitting}>
          Vazgeç
        </Button>
        <Button type="submit" loading={isSubmitting}>
          {project ? "Kaydet" : "Projeyi oluştur"}
        </Button>
      </DialogActions>
    </form>
  );
}

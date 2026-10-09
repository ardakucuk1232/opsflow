"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { PermissionPicker } from "@/components/roles/permission-picker";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogActions } from "@/components/ui/dialog";
import { FormField, describedBy } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { rolesApi } from "@/lib/api/roles";
import { useAuth } from "@/lib/auth/auth-provider";
import { getRoleLabel, getSystemRoleDescription } from "@/lib/auth/roles";
import { roleSchema, type RoleFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import type { Permission, Role } from "@/types/api";

export type RoleEditorTarget = { mode: "create" } | { mode: "edit"; role: Role };

type RoleEditorDialogProps = {
  target: RoleEditorTarget | null;
  catalog: Permission[];
  onClose: () => void;
  onSaved: (role: Role) => void;
};

export function RoleEditorDialog({ target, catalog, onClose, onSaved }: RoleEditorDialogProps) {
  const role = target?.mode === "edit" ? target.role : null;

  const title = role === null
    ? "Yeni rol"
    : role.isSystemRole
      ? getRoleLabel(role.name)
      : "Rolü düzenle";

  return (
    <Dialog
      open={target !== null}
      onClose={onClose}
      title={title}
      size="lg"
      description={role?.isSystemRole ? "Sistem rolleri değiştirilemez." : undefined}
    >
      {target ? (
        <RoleEditorForm key={role?.id ?? "new"} role={role} catalog={catalog} onCancel={onClose} onSaved={onSaved} />
      ) : null}
    </Dialog>
  );
}

function RoleEditorForm({
  role,
  catalog,
  onCancel,
  onSaved,
}: {
  role: Role | null;
  catalog: Permission[];
  onCancel: () => void;
  onSaved: (role: Role) => void;
}) {
  const { user } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);
  const readOnly = role?.isSystemRole ?? false;
  const ownPermissions = user?.permissions ?? [];
  const lacksExisting = !readOnly && (role?.permissions ?? []).some((code) => !ownPermissions.includes(code));

  const {
    register,
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RoleFormValues>({
    resolver: zodResolver(roleSchema),
    defaultValues: {
      name: role?.name ?? "",
      description: role?.description ?? "",
      permissions: role?.permissions ?? [],
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    const input = {
      name: values.name,
      description: values.description === "" ? null : values.description,
      permissions: values.permissions,
    };

    try {
      onSaved(role ? await rolesApi.update(role.id, input) : await rolesApi.create(input));
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  });

  if (readOnly && role) {
    return (
      <div className="flex flex-col gap-5">
        <p className="text-sm text-muted">{getSystemRoleDescription(role.name)}</p>
        <PermissionPicker
          catalog={catalog}
          value={role.permissions}
          onChange={() => undefined}
          isAllowed={() => true}
          readOnly
        />
        <DialogActions>
          <Button variant="secondary" onClick={onCancel}>
            Kapat
          </Button>
        </DialogActions>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      {formError ? <Alert>{formError}</Alert> : null}
      {lacksExisting ? (
        <Alert>Bu rolde sizde olmayan izinler var. Rolü yalnızca bu izinlerin hepsine sahip biri değiştirebilir.</Alert>
      ) : null}

      <FormField id="role-name" label="Rol adı" error={errors.name?.message}>
        <Input
          id="role-name"
          autoComplete="off"
          invalid={Boolean(errors.name)}
          aria-describedby={describedBy("role-name", errors.name?.message)}
          {...register("name")}
        />
      </FormField>

      <FormField id="role-description" label="Açıklama (isteğe bağlı)" error={errors.description?.message}>
        <Input
          id="role-description"
          autoComplete="off"
          invalid={Boolean(errors.description)}
          aria-describedby={describedBy("role-description", errors.description?.message)}
          {...register("description")}
        />
      </FormField>

      <div>
        <p className="mb-3 text-sm font-medium text-foreground">İzinler</p>
        <Controller
          control={control}
          name="permissions"
          render={({ field }) => (
            <PermissionPicker
              catalog={catalog}
              value={field.value}
              onChange={field.onChange}
              isAllowed={(code) => ownPermissions.includes(code)}
            />
          )}
        />
      </div>

      <DialogActions>
        <Button variant="secondary" onClick={onCancel} disabled={isSubmitting}>
          Vazgeç
        </Button>
        <Button type="submit" loading={isSubmitting} disabled={lacksExisting}>
          {role ? "Kaydet" : "Rolü oluştur"}
        </Button>
      </DialogActions>
    </form>
  );
}

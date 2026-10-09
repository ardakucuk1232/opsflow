"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { RoleCheckboxGroup } from "@/components/team/role-checkbox-group";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogActions } from "@/components/ui/dialog";
import { usersApi } from "@/lib/api/users";
import { userRolesSchema, type UserRolesFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import type { Role, UserSummary } from "@/types/api";

type EditRolesDialogProps = {
  user: UserSummary | null;
  roles: Role[];
  onClose: () => void;
  onSaved: (user: UserSummary) => void;
};

export function EditRolesDialog({ user, roles, onClose, onSaved }: EditRolesDialogProps) {
  return (
    <Dialog
      open={user !== null}
      onClose={onClose}
      title="Rolleri düzenle"
      description={user ? `${user.firstName} ${user.lastName} · ${user.email}` : undefined}
    >
      {user ? <EditRolesForm user={user} roles={roles} onCancel={onClose} onSaved={onSaved} /> : null}
    </Dialog>
  );
}

function EditRolesForm({
  user,
  roles,
  onCancel,
  onSaved,
}: {
  user: UserSummary;
  roles: Role[];
  onCancel: () => void;
  onSaved: (user: UserSummary) => void;
}) {
  const [formError, setFormError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<UserRolesFormValues>({
    resolver: zodResolver(userRolesSchema),
    defaultValues: { roleIds: user.roles.map((role) => role.id) },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      onSaved(await usersApi.updateRoles(user.id, values.roleIds));
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      {formError ? <Alert>{formError}</Alert> : null}

      <Controller
        control={control}
        name="roleIds"
        render={({ field }) => (
          <RoleCheckboxGroup
            roles={roles}
            value={field.value}
            onChange={field.onChange}
            error={errors.roleIds?.message}
          />
        )}
      />

      <DialogActions>
        <Button variant="secondary" onClick={onCancel} disabled={isSubmitting}>
          Vazgeç
        </Button>
        <Button type="submit" loading={isSubmitting}>
          Kaydet
        </Button>
      </DialogActions>
    </form>
  );
}

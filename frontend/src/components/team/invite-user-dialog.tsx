"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { RoleCheckboxGroup } from "@/components/team/role-checkbox-group";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogActions } from "@/components/ui/dialog";
import { FormField, describedBy } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { ApiError } from "@/lib/api/errors";
import { usersApi } from "@/lib/api/users";
import { inviteUserSchema, type InviteUserFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import type { Role, UserSummary } from "@/types/api";

type InviteUserDialogProps = {
  open: boolean;
  roles: Role[];
  onClose: () => void;
  onInvited: (user: UserSummary) => void;
};

export function InviteUserDialog({ open, roles, onClose, onInvited }: InviteUserDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Kullanıcı davet et"
      description="Davet edilen kişiye şifresini belirleyeceği bir bağlantı gönderilir."
    >
      <InviteUserForm roles={roles} onCancel={onClose} onInvited={onInvited} />
    </Dialog>
  );
}

function InviteUserForm({
  roles,
  onCancel,
  onInvited,
}: {
  roles: Role[];
  onCancel: () => void;
  onInvited: (user: UserSummary) => void;
}) {
  const [formError, setFormError] = useState<string | null>(null);
  const defaultRole = roles.find((role) => role.isSystemRole && role.name === "Employee");

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<InviteUserFormValues>({
    resolver: zodResolver(inviteUserSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      email: "",
      roleIds: defaultRole ? [defaultRole.id] : [],
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      onInvited(await usersApi.invite(values));
    } catch (error) {
      if (error instanceof ApiError && error.code === "auth.email_already_in_use") {
        setError("email", { message: getErrorMessage(error) }, { shouldFocus: true });
        return;
      }

      setFormError(getErrorMessage(error));
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      {formError ? <Alert>{formError}</Alert> : null}

      <div className="grid gap-5 sm:grid-cols-2">
        <FormField id="invite-first-name" label="Ad" error={errors.firstName?.message}>
          <Input
            id="invite-first-name"
            autoComplete="off"
            invalid={Boolean(errors.firstName)}
            aria-describedby={describedBy("invite-first-name", errors.firstName?.message)}
            {...register("firstName")}
          />
        </FormField>
        <FormField id="invite-last-name" label="Soyad" error={errors.lastName?.message}>
          <Input
            id="invite-last-name"
            autoComplete="off"
            invalid={Boolean(errors.lastName)}
            aria-describedby={describedBy("invite-last-name", errors.lastName?.message)}
            {...register("lastName")}
          />
        </FormField>
      </div>

      <FormField id="invite-email" label="E-posta" error={errors.email?.message}>
        <Input
          id="invite-email"
          type="email"
          autoComplete="off"
          placeholder="ad@sirket.com"
          invalid={Boolean(errors.email)}
          aria-describedby={describedBy("invite-email", errors.email?.message)}
          {...register("email")}
        />
      </FormField>

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
          Davet gönder
        </Button>
      </DialogActions>
    </form>
  );
}

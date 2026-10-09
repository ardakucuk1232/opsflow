"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useCallback, useState } from "react";
import { useForm } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField, describedBy } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { Spinner } from "@/components/ui/spinner";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/errors";
import { useAuth } from "@/lib/auth/auth-provider";
import {
  acceptInvitationSchema,
  PASSWORD_HINT,
  type AcceptInvitationFormValues,
} from "@/lib/auth/schemas";
import { useApiData } from "@/lib/hooks/use-api-data";
import { useFragmentParam } from "@/lib/hooks/use-fragment-param";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import type { InvitationPreview } from "@/types/api";

export function AcceptInvitationForm() {
  const token = useFragmentParam("token");

  if (token === undefined) {
    return <Loading />;
  }

  if (token === null) {
    return <InvalidInvitation />;
  }

  return <InvitationDetails token={token} />;
}

function Loading() {
  return (
    <div className="flex justify-center" role="status">
      <Spinner className="size-6 text-primary" />
      <span className="sr-only">Yükleniyor</span>
    </div>
  );
}

function InvalidInvitation() {
  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Davet kullanılamıyor</h1>
      <div className="mt-6">
        <Alert>
          Davet bağlantısı geçersiz, kullanılmış ya da süresi dolmuş. Sizi davet eden kişiden yeni bir
          davet göndermesini isteyin.
        </Alert>
      </div>
    </div>
  );
}

function InvitationDetails({ token }: { token: string }) {
  const loadPreview = useCallback((signal: AbortSignal) => authApi.previewInvitation(token, signal), [token]);
  const preview = useApiData(loadPreview);

  if (preview.error instanceof ApiError && preview.error.code === "auth.invalid_token") {
    return <InvalidInvitation />;
  }

  if (preview.error) {
    return (
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Davet yüklenemedi</h1>
        <div className="mt-6">
          <Alert>{getErrorMessage(preview.error)}</Alert>
        </div>
        <Button size="lg" fullWidth className="mt-6" onClick={preview.reload}>
          Tekrar dene
        </Button>
      </div>
    );
  }

  if (!preview.data) {
    return <Loading />;
  }

  return <AcceptForm token={token} invitation={preview.data} />;
}

function AcceptForm({ token, invitation }: { token: string; invitation: InvitationPreview }) {
  const router = useRouter();
  const { acceptInvitation } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);
  const [rejected, setRejected] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<AcceptInvitationFormValues>({
    resolver: zodResolver(acceptInvitationSchema),
    defaultValues: { password: "", passwordConfirmation: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      await acceptInvitation({ token, password: values.password });
      router.replace("/dashboard");
    } catch (error) {
      if (error instanceof ApiError && error.code === "auth.invalid_token") {
        setRejected(true);
        return;
      }

      setFormError(getErrorMessage(error));
    }
  });

  if (rejected) {
    return <InvalidInvitation />;
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Daveti kabul et</h1>
      <p className="mt-2 text-sm text-muted">
        <span className="font-medium text-foreground">{invitation.companyName}</span> sizi OpsFlow
        çalışma alanına davet etti. Hesabınızı kullanmaya başlamak için bir şifre belirleyin.
      </p>

      <form onSubmit={onSubmit} noValidate className="mt-8 flex flex-col gap-5">
        {formError ? <Alert>{formError}</Alert> : null}

        <FormField id="invitation-email" label="E-posta">
          <Input id="invitation-email" type="email" value={invitation.email} readOnly disabled />
        </FormField>

        <FormField id="password" label="Şifre" error={errors.password?.message} hint={PASSWORD_HINT}>
          <PasswordInput
            id="password"
            autoComplete="new-password"
            invalid={Boolean(errors.password)}
            aria-describedby={describedBy("password", errors.password?.message, PASSWORD_HINT)}
            {...register("password")}
          />
        </FormField>

        <FormField id="passwordConfirmation" label="Şifre (tekrar)" error={errors.passwordConfirmation?.message}>
          <PasswordInput
            id="passwordConfirmation"
            autoComplete="new-password"
            invalid={Boolean(errors.passwordConfirmation)}
            aria-describedby={describedBy("passwordConfirmation", errors.passwordConfirmation?.message)}
            {...register("passwordConfirmation")}
          />
        </FormField>

        <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
          Hesabımı oluştur
        </Button>
      </form>
    </div>
  );
}

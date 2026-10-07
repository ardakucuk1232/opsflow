"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField, describedBy } from "@/components/ui/form-field";
import { PasswordInput } from "@/components/ui/password-input";
import { Spinner } from "@/components/ui/spinner";
import { ButtonLink, TextLink } from "@/components/ui/text-link";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/errors";
import { useAuth } from "@/lib/auth/auth-provider";
import {
  PASSWORD_HINT,
  resetPasswordSchema,
  type ResetPasswordFormValues,
} from "@/lib/auth/schemas";
import { useFragmentParam } from "@/lib/hooks/use-fragment-param";
import { getErrorMessage } from "@/lib/i18n/error-messages";

export function ResetPasswordForm() {
  const token = useFragmentParam("token");
  const router = useRouter();
  const { logout } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);
  const [linkRejected, setLinkRejected] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { password: "", passwordConfirmation: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    if (!token) {
      return;
    }

    setFormError(null);

    try {
      await authApi.resetPassword({ token, newPassword: values.password });
      await logout();
      router.replace("/login?reset=1");
    } catch (error) {
      if (error instanceof ApiError && error.code === "auth.invalid_token") {
        setLinkRejected(true);
        return;
      }

      setFormError(getErrorMessage(error));
    }
  });

  if (token === undefined) {
    return (
      <div className="flex justify-center" role="status">
        <Spinner className="size-6 text-primary" />
        <span className="sr-only">Yükleniyor</span>
      </div>
    );
  }

  if (token === null || linkRejected) {
    return (
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">
          Bağlantı kullanılamıyor
        </h1>
        <div className="mt-6">
          <Alert>
            Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş. Yeni bir bağlantı isteyebilirsiniz.
          </Alert>
        </div>
        <ButtonLink href="/forgot-password" className="mt-6">
          Yeni bağlantı iste
        </ButtonLink>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Yeni şifre belirle</h1>
      <p className="mt-2 text-sm text-muted">
        Şifreniz değiştiğinde açık olan tüm oturumlarınız kapatılır.
      </p>

      <form onSubmit={onSubmit} noValidate className="mt-8 flex flex-col gap-5">
        {formError ? <Alert>{formError}</Alert> : null}

        <FormField
          id="password"
          label="Yeni şifre"
          error={errors.password?.message}
          hint={PASSWORD_HINT}
        >
          <PasswordInput
            id="password"
            autoComplete="new-password"
            invalid={Boolean(errors.password)}
            aria-describedby={describedBy("password", errors.password?.message, PASSWORD_HINT)}
            {...register("password")}
          />
        </FormField>

        <FormField
          id="passwordConfirmation"
          label="Yeni şifre (tekrar)"
          error={errors.passwordConfirmation?.message}
        >
          <PasswordInput
            id="passwordConfirmation"
            autoComplete="new-password"
            invalid={Boolean(errors.passwordConfirmation)}
            aria-describedby={describedBy(
              "passwordConfirmation",
              errors.passwordConfirmation?.message,
            )}
            {...register("passwordConfirmation")}
          />
        </FormField>

        <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
          Şifreyi güncelle
        </Button>
      </form>

      <p className="mt-8 text-center text-sm text-muted">
        <TextLink href="/login">Giriş sayfasına dön</TextLink>
      </p>
    </div>
  );
}

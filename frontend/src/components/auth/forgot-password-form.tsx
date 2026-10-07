"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField, describedBy } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { TextLink } from "@/components/ui/text-link";
import { authApi } from "@/lib/api/auth";
import { forgotPasswordSchema, type ForgotPasswordFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";

export function ForgotPasswordForm() {
  const [formError, setFormError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      await authApi.forgotPassword(values.email);
      setSubmitted(true);
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  });

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Şifremi unuttum</h1>
      <p className="mt-2 text-sm text-muted">
        Hesabınızın e-posta adresini girin, şifre sıfırlama bağlantısı gönderelim.
      </p>

      {submitted ? (
        <div className="mt-8">
          <Alert tone="success">
            Bu adresle kayıtlı bir hesap varsa şifre sıfırlama bağlantısını gönderdik. Gelen kutunuzu
            kontrol edin.
          </Alert>
        </div>
      ) : (
        <form onSubmit={onSubmit} noValidate className="mt-8 flex flex-col gap-5">
          {formError ? <Alert>{formError}</Alert> : null}

          <FormField id="email" label="E-posta" error={errors.email?.message}>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              placeholder="ad@sirket.com"
              invalid={Boolean(errors.email)}
              aria-describedby={describedBy("email", errors.email?.message)}
              {...register("email")}
            />
          </FormField>

          <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
            Bağlantı gönder
          </Button>
        </form>
      )}

      <p className="mt-8 text-center text-sm text-muted">
        <TextLink href="/login">Giriş sayfasına dön</TextLink>
      </p>
    </div>
  );
}

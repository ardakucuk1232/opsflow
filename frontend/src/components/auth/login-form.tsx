"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField, describedBy } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { useAuth } from "@/lib/auth/auth-provider";
import { loginSchema, type LoginFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";

export function LoginForm() {
  const { login } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      await login(values);
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  });

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Giriş yap</h1>
      <p className="mt-2 text-sm text-muted">Hesabınıza erişmek için bilgilerinizi girin.</p>

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

        <FormField id="password" label="Şifre" error={errors.password?.message}>
          <PasswordInput
            id="password"
            autoComplete="current-password"
            invalid={Boolean(errors.password)}
            aria-describedby={describedBy("password", errors.password?.message)}
            {...register("password")}
          />
        </FormField>

        <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
          Giriş yap
        </Button>
      </form>

      <p className="mt-8 text-center text-sm text-muted">
        Şirketiniz henüz kayıtlı değil mi?{" "}
        <Link href="/register" className="font-medium text-primary hover:text-primary-hover">
          Şirket hesabı oluşturun
        </Link>
      </p>
    </div>
  );
}

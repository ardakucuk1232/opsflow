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
import { ApiError } from "@/lib/api/errors";
import { useAuth } from "@/lib/auth/auth-provider";
import { PASSWORD_HINT, registerSchema, type RegisterFormValues } from "@/lib/auth/schemas";
import { getErrorMessage } from "@/lib/i18n/error-messages";

const SERVER_FIELD_ERROR = "Bu alanı kontrol edin.";

const SERVER_FIELDS = ["companyName", "firstName", "lastName", "email", "password"] as const;

export function RegisterForm() {
  const { register: registerAccount } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      companyName: "",
      firstName: "",
      lastName: "",
      email: "",
      password: "",
      passwordConfirmation: "",
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      await registerAccount({
        companyName: values.companyName,
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        password: values.password,
      });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.code === "auth.email_already_in_use") {
          setError("email", { message: getErrorMessage(error) }, { shouldFocus: true });
          return;
        }

        for (const field of SERVER_FIELDS) {
          if (error.fieldErrors[field]) {
            setError(field, { message: SERVER_FIELD_ERROR });
          }
        }
      }

      setFormError(getErrorMessage(error));
    }
  });

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Şirket hesabı oluştur</h1>
      <p className="mt-2 text-sm text-muted">
        Şirketiniz için bir çalışma alanı açın. Hesabı açan kişi Admin rolüyle eklenir.
      </p>

      <form onSubmit={onSubmit} noValidate className="mt-8 flex flex-col gap-5">
        {formError ? <Alert>{formError}</Alert> : null}

        <FormField id="companyName" label="Şirket adı" error={errors.companyName?.message}>
          <Input
            id="companyName"
            autoComplete="organization"
            invalid={Boolean(errors.companyName)}
            aria-describedby={describedBy("companyName", errors.companyName?.message)}
            {...register("companyName")}
          />
        </FormField>

        <div className="grid gap-5 sm:grid-cols-2">
          <FormField id="firstName" label="Ad" error={errors.firstName?.message}>
            <Input
              id="firstName"
              autoComplete="given-name"
              invalid={Boolean(errors.firstName)}
              aria-describedby={describedBy("firstName", errors.firstName?.message)}
              {...register("firstName")}
            />
          </FormField>

          <FormField id="lastName" label="Soyad" error={errors.lastName?.message}>
            <Input
              id="lastName"
              autoComplete="family-name"
              invalid={Boolean(errors.lastName)}
              aria-describedby={describedBy("lastName", errors.lastName?.message)}
              {...register("lastName")}
            />
          </FormField>
        </div>

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

        <FormField id="password" label="Şifre" error={errors.password?.message} hint={PASSWORD_HINT}>
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
          label="Şifre (tekrar)"
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
          Hesabı oluştur
        </Button>
      </form>

      <p className="mt-8 text-center text-sm text-muted">
        Zaten hesabınız var mı?{" "}
        <Link href="/login" className="font-medium text-primary hover:text-primary-hover">
          Giriş yapın
        </Link>
      </p>
    </div>
  );
}

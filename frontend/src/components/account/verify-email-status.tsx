"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { ButtonLink } from "@/components/ui/text-link";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/errors";
import { useAuth } from "@/lib/auth/auth-provider";
import { useFragmentParam } from "@/lib/hooks/use-fragment-param";
import { getErrorMessage } from "@/lib/i18n/error-messages";

type Outcome =
  | { kind: "verified" }
  | { kind: "rejected" }
  | { kind: "failed"; message: string };

export function VerifyEmailStatus() {
  const token = useFragmentParam("token");
  const { status, reloadUser } = useAuth();
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const submittedToken = useRef<string | null>(null);

  const verify = useCallback(
    async (value: string) => {
      try {
        await authApi.verifyEmail(value);
        setOutcome({ kind: "verified" });
        void reloadUser();
      } catch (error) {
        if (error instanceof ApiError && error.code === "auth.invalid_token") {
          setOutcome({ kind: "rejected" });
          return;
        }

        setOutcome({ kind: "failed", message: getErrorMessage(error) });
      }
    },
    [reloadUser],
  );

  useEffect(() => {
    if (!token || submittedToken.current === token) {
      return;
    }

    submittedToken.current = token;
    void verify(token);
  }, [token, verify]);

  const signedIn = status === "authenticated";
  const continueHref = signedIn ? "/dashboard" : "/login";
  const continueLabel = signedIn ? "Panele git" : "Giriş yap";

  if (token === null || outcome?.kind === "rejected") {
    return (
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">
          Bağlantı kullanılamıyor
        </h1>
        <div className="mt-6">
          <Alert>
            Doğrulama bağlantısı geçersiz veya süresi dolmuş. Giriş yaptıktan sonra panelden yeni
            bir doğrulama e-postası isteyebilirsiniz.
          </Alert>
        </div>
        <ButtonLink href={continueHref} className="mt-6">
          {continueLabel}
        </ButtonLink>
      </div>
    );
  }

  if (outcome?.kind === "verified") {
    return (
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">
          E-posta adresiniz doğrulandı
        </h1>
        <div className="mt-6">
          <Alert tone="success">Hesabınızı kullanmaya devam edebilirsiniz.</Alert>
        </div>
        <ButtonLink href={continueHref} className="mt-6">
          {continueLabel}
        </ButtonLink>
      </div>
    );
  }

  if (outcome?.kind === "failed") {
    return (
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">
          E-posta adresi doğrulanamadı
        </h1>
        <div className="mt-6">
          <Alert>{outcome.message}</Alert>
        </div>
        <Button
          size="lg"
          fullWidth
          className="mt-6"
          onClick={() => {
            if (token) {
              setOutcome(null);
              void verify(token);
            }
          }}
        >
          Tekrar dene
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-4 text-center" role="status">
      <Spinner className="size-6 text-primary" />
      <p className="text-sm text-muted">E-posta adresiniz doğrulanıyor</p>
    </div>
  );
}

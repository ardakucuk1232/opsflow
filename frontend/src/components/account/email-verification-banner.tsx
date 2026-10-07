"use client";

import { MailWarning } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { authApi } from "@/lib/api/auth";
import { useAuth } from "@/lib/auth/auth-provider";
import { getErrorMessage } from "@/lib/i18n/error-messages";

type ResendState =
  | { kind: "idle" }
  | { kind: "sending" }
  | { kind: "sent" }
  | { kind: "failed"; message: string };

export function EmailVerificationBanner() {
  const { user, reloadUser } = useAuth();
  const [resend, setResend] = useState<ResendState>({ kind: "idle" });

  const unverified = user !== null && !user.isEmailVerified;

  useEffect(() => {
    if (!unverified) {
      return;
    }

    function handleFocus() {
      void reloadUser();
    }

    window.addEventListener("focus", handleFocus);

    return () => window.removeEventListener("focus", handleFocus);
  }, [unverified, reloadUser]);

  if (!unverified) {
    return null;
  }

  async function handleResend() {
    setResend({ kind: "sending" });

    try {
      await authApi.resendVerification();
      setResend({ kind: "sent" });
    } catch (error) {
      setResend({ kind: "failed", message: getErrorMessage(error) });
    }
  }

  return (
    <div
      role="status"
      className="flex flex-col gap-3 border-b border-warning/30 bg-warning-soft px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-6"
    >
      <div className="flex items-start gap-2.5 text-sm text-warning">
        <MailWarning className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
        <p>
          {resend.kind === "sent" ? (
            <>
              Gelen kutunuzu kontrol edin. E-posta birkaç dakika içinde ulaşmazsa tekrar
              deneyebilirsiniz.
            </>
          ) : resend.kind === "failed" ? (
            resend.message
          ) : (
            <>
              E-posta adresiniz henüz doğrulanmadı. <span className="font-medium">{user.email}</span>{" "}
              adresine gönderdiğimiz bağlantıyı açın.
            </>
          )}
        </p>
      </div>
      <Button
        variant="secondary"
        size="sm"
        className="shrink-0 self-start sm:self-auto"
        loading={resend.kind === "sending"}
        onClick={() => void handleResend()}
      >
        Tekrar gönder
      </Button>
    </div>
  );
}

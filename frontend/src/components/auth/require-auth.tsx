"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { FullPageMessage, FullPageSpinner } from "@/components/ui/full-page-state";
import { useAuth } from "@/lib/auth/auth-provider";

export function RequireAuth({ children }: { children: ReactNode }) {
  const { status, retry } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (status === "unauthenticated") {
      router.replace(`/login?next=${encodeURIComponent(pathname)}`);
    }
  }, [status, router, pathname]);

  if (status === "unavailable") {
    return (
      <FullPageMessage
        title="Sunucuya ulaşılamıyor"
        description="Oturumunuz doğrulanamadı. Bağlantınızı kontrol edip tekrar deneyin."
        action={<Button onClick={() => void retry()}>Tekrar dene</Button>}
      />
    );
  }

  if (status !== "authenticated") {
    return <FullPageSpinner />;
  }

  return children;
}

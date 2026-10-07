"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { FullPageSpinner } from "@/components/ui/full-page-state";
import { useAuth } from "@/lib/auth/auth-provider";
import { getSafeRedirectPath } from "@/lib/utils/redirect";

export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const nextPath = getSafeRedirectPath(searchParams.get("next"));

  useEffect(() => {
    if (status === "authenticated") {
      router.replace(nextPath);
    }
  }, [status, router, nextPath]);

  if (status === "loading" || status === "authenticated") {
    return <FullPageSpinner />;
  }

  return children;
}

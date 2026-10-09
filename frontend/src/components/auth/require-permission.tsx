"use client";

import { ShieldAlert } from "lucide-react";
import type { ReactNode } from "react";
import { usePermission } from "@/lib/auth/use-permission";

export function RequirePermission({ permission, children }: { permission: string; children: ReactNode }) {
  const allowed = usePermission(permission);

  if (!allowed) {
    return (
      <div className="flex flex-col items-center rounded-xl border border-border bg-surface px-6 py-16 text-center">
        <ShieldAlert className="size-8 text-muted" aria-hidden="true" />
        <h1 className="mt-4 text-base font-medium text-foreground">Bu sayfayı görüntüleme yetkiniz yok</h1>
        <p className="mt-2 max-w-sm text-sm text-muted">
          Erişmeniz gerekiyorsa şirketinizin yöneticisinden rolünüzü güncellemesini isteyin.
        </p>
      </div>
    );
  }

  return children;
}

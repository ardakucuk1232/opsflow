import { Suspense, type ReactNode } from "react";
import { AuthSplitLayout } from "@/components/auth/auth-split-layout";
import { GuestOnly } from "@/components/auth/guest-only";
import { FullPageSpinner } from "@/components/ui/full-page-state";

export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <Suspense fallback={<FullPageSpinner />}>
      <GuestOnly>
        <AuthSplitLayout>{children}</AuthSplitLayout>
      </GuestOnly>
    </Suspense>
  );
}

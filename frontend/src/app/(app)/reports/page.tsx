import type { Metadata } from "next";
import { RequirePermission } from "@/components/auth/require-permission";
import { ReportsPage } from "@/components/reports/reports-page";
import { PERMISSIONS } from "@/lib/auth/permissions";

export const metadata: Metadata = {
  title: "Raporlar",
};

export default function Page() {
  return (
    <RequirePermission permission={PERMISSIONS.reportView}>
      <ReportsPage />
    </RequirePermission>
  );
}

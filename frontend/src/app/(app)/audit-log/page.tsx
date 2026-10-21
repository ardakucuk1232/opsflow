import type { Metadata } from "next";
import { AuditLogPage } from "@/components/audit/audit-log-page";
import { RequirePermission } from "@/components/auth/require-permission";
import { PERMISSIONS } from "@/lib/auth/permissions";

export const metadata: Metadata = {
  title: "Denetim kaydı",
};

export default function Page() {
  return (
    <RequirePermission permission={PERMISSIONS.auditLogView}>
      <AuditLogPage />
    </RequirePermission>
  );
}

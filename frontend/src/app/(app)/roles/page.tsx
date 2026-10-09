import type { Metadata } from "next";
import { RequirePermission } from "@/components/auth/require-permission";
import { RolesPage } from "@/components/roles/roles-page";
import { PERMISSIONS } from "@/lib/auth/permissions";

export const metadata: Metadata = {
  title: "Roller",
};

export default function Page() {
  return (
    <RequirePermission permission={PERMISSIONS.roleManage}>
      <RolesPage />
    </RequirePermission>
  );
}

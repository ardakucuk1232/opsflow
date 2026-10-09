import type { Metadata } from "next";
import { RequirePermission } from "@/components/auth/require-permission";
import { TeamPage } from "@/components/team/team-page";
import { PERMISSIONS } from "@/lib/auth/permissions";

export const metadata: Metadata = {
  title: "Ekip",
};

export default function Page() {
  return (
    <RequirePermission permission={PERMISSIONS.userView}>
      <TeamPage />
    </RequirePermission>
  );
}

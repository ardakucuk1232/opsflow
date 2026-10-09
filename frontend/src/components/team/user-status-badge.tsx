import { Badge } from "@/components/ui/badge";
import type { UserSummary } from "@/types/api";

export function UserStatusBadge({ user }: { user: UserSummary }) {
  if (!user.isActive) {
    return <Badge>Pasif</Badge>;
  }

  if (user.invitationPending) {
    return <Badge tone="warning">Davet bekliyor</Badge>;
  }

  return <Badge tone="success">Aktif</Badge>;
}

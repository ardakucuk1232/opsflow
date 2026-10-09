import { Badge } from "@/components/ui/badge";
import { getProjectStatusLabel, getProjectStatusTone } from "@/lib/i18n/projects";
import type { ProjectStatus } from "@/types/api";

export function ProjectStatusBadge({ status }: { status: ProjectStatus }) {
  return <Badge tone={getProjectStatusTone(status)}>{getProjectStatusLabel(status)}</Badge>;
}

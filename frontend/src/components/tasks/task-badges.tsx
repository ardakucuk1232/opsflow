import { Badge } from "@/components/ui/badge";
import {
  getTaskPriorityLabel,
  getTaskPriorityTone,
  getTaskStatusLabel,
  getTaskStatusTone,
} from "@/lib/i18n/tasks";
import type { TaskPriority, TaskStatus } from "@/types/api";

export function TaskStatusBadge({ status }: { status: TaskStatus }) {
  return <Badge tone={getTaskStatusTone(status)}>{getTaskStatusLabel(status)}</Badge>;
}

export function TaskPriorityBadge({ priority }: { priority: TaskPriority }) {
  return <Badge tone={getTaskPriorityTone(priority)}>{getTaskPriorityLabel(priority)}</Badge>;
}

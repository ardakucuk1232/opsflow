"use client";

import { AlertTriangle, CalendarClock, CheckCircle2, ListTodo } from "lucide-react";
import { useCallback, useMemo } from "react";
import { StatTile } from "@/components/charts/stat-tile";
import { reportsApi } from "@/lib/api/reports";
import { useApiData } from "@/lib/hooks/use-api-data";
import { todayIso } from "@/lib/tasks/board";

export function MySummary() {
  const today = useMemo(() => todayIso(), []);
  const load = useCallback((signal: AbortSignal) => reportsApi.me(today, signal), [today]);
  const summary = useApiData(load);
  const data = summary.data;

  if (!data) {
    return null;
  }

  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      <StatTile label="Açık görevlerim" value={data.open} icon={ListTodo} href="/tasks?assignee=me&status=open" />
      <StatTile label="Geciken" value={data.overdue} icon={AlertTriangle} tone="danger" href="/tasks?assignee=me&due=overdue" />
      <StatTile label="Bu hafta bitecek" value={data.dueThisWeek} icon={CalendarClock} href="/tasks?assignee=me&status=open&due=week" />
      <StatTile label="Son 7 günde bitirdiğim" value={data.completedLast7Days} icon={CheckCircle2} />
    </div>
  );
}

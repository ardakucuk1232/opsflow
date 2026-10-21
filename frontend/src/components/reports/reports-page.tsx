"use client";

import { AlertTriangle, CheckCircle2, FolderKanban, ListTodo, UserX } from "lucide-react";
import Link from "next/link";
import { useCallback, useMemo, useState, type ReactNode } from "react";
import { BarList } from "@/components/charts/bar-list";
import { Meter } from "@/components/charts/meter";
import { StatTile } from "@/components/charts/stat-tile";
import { WeeklyChart } from "@/components/charts/weekly-chart";
import { Alert } from "@/components/ui/alert";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { projectsApi } from "@/lib/api/projects";
import { reportsApi } from "@/lib/api/reports";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { getTaskPriorityLabel, getTaskStatusLabel } from "@/lib/i18n/tasks";
import { todayIso } from "@/lib/tasks/board";
import { cn } from "@/lib/utils/cn";

function Panel({ title, description, children, className }: { title: string; description?: string; children: ReactNode; className?: string }) {
  return (
    <section className={cn("rounded-xl border border-border bg-surface p-5 shadow-xs", className)} aria-label={title}>
      <h2 className="text-sm font-medium text-foreground">{title}</h2>
      {description ? <p className="mt-0.5 text-xs text-muted">{description}</p> : null}
      <div className="mt-4">{children}</div>
    </section>
  );
}

export function ReportsPage() {
  const today = useMemo(() => todayIso(), []);
  const [projectId, setProjectId] = useState("");

  const loadProjects = useCallback((signal: AbortSignal) => projectsApi.list({ page: 1, pageSize: 100 }, signal), []);
  const projects = useApiData(loadProjects);

  const loadReport = useCallback(
    (signal: AbortSignal) => reportsApi.overview({ projectId: projectId || undefined, today }, signal),
    [projectId, today],
  );
  const report = useApiData(loadReport);
  const data = report.data;
  const projectQuery = projectId ? `&project=${projectId}` : "";

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Raporlar" description="Görevlerin durumu, ekibin iş yükü ve son haftaların akışı." />

      <div className="flex">
        <Select aria-label="Proje" className="sm:w-72" value={projectId} onChange={(event) => setProjectId(event.target.value)}>
          <option value="">Tüm projeler</option>
          {projects.data?.items.map((project) => (
            <option key={project.id} value={project.id}>
              {project.key} · {project.name}
            </option>
          ))}
        </Select>
      </div>

      {report.error ? (
        <Alert>
          <span>{getErrorMessage(report.error)}</span>{" "}
          <button type="button" onClick={report.reload} className="font-medium underline">
            Tekrar dene
          </button>
        </Alert>
      ) : null}

      {data === undefined && report.loading ? (
        <div className="flex justify-center py-16" role="status">
          <Spinner className="size-5 text-primary" />
          <span className="sr-only">Rapor yükleniyor</span>
        </div>
      ) : null}

      {data ? (
        <div className={cn("flex flex-col gap-6 transition-opacity", report.loading && "opacity-60")} aria-busy={report.loading}>
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-5">
            <StatTile label="Aktif proje" value={data.totals.activeProjects} icon={FolderKanban} />
            <StatTile label="Açık görev" value={data.totals.openTasks} icon={ListTodo} href={`/tasks?status=open${projectQuery}`} />
            <StatTile
              label="Geciken"
              value={data.totals.overdueTasks}
              icon={AlertTriangle}
              tone="danger"
              href={`/tasks?due=overdue${projectQuery}`}
            />
            <StatTile label="Atanmamış" value={data.totals.unassignedOpenTasks} icon={UserX} href={`/tasks?status=open&assignee=none${projectQuery}`} />
            <StatTile label="Son 30 günde biten" value={data.totals.completedLast30Days} icon={CheckCircle2} />
          </div>

          <Panel title="Haftalık akış" description="Son 8 haftada oluşturulan ve tamamlanan görevler.">
            <WeeklyChart points={data.weekly} />
          </Panel>

          <div className="grid gap-6 lg:grid-cols-2">
            <Panel title="Duruma göre görevler">
              <BarList
                label="Duruma göre görev sayıları"
                items={data.byStatus.map((item) => ({ key: item.status, label: getTaskStatusLabel(item.status), value: item.count }))}
              />
            </Panel>
            <Panel title="Açık görevlerin önceliği">
              <BarList
                label="Önceliğe göre açık görev sayıları"
                items={[...data.openByPriority].reverse().map((item) => ({
                  key: item.priority,
                  label: getTaskPriorityLabel(item.priority),
                  value: item.count,
                }))}
              />
            </Panel>
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <Panel title="İş yükü" description="Kişi başına açık görevler.">
              <BarList
                label="Kişi başına açık görev sayıları"
                emptyText="Atanmış açık görev yok."
                items={data.workload.map((item) => ({
                  key: item.user.id,
                  label: `${item.user.firstName} ${item.user.lastName}`,
                  value: item.open,
                  detail: item.overdue > 0 ? `· ${item.overdue} gecikmiş` : undefined,
                }))}
              />
            </Panel>

            <Panel title="Proje ilerlemesi" description="Tamamlanan görevlerin oranı, iptal edilenler hariç.">
              {data.projects.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted">Gösterilecek proje yok.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-xs text-muted">
                        <th className="pb-2 font-medium">Proje</th>
                        <th className="pb-2 font-medium">İlerleme</th>
                        <th className="pb-2 pl-3 text-right font-medium">Açık</th>
                        <th className="pb-2 pl-3 text-right font-medium">Geciken</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {data.projects.map((project) => (
                        <tr key={project.id}>
                          <td className="max-w-40 py-2.5 pr-3">
                            <Link href={`/projects/${project.id}`} className="block truncate font-medium text-foreground hover:underline">
                              {project.name}
                            </Link>
                            <span className="font-mono text-xs text-muted">{project.key}</span>
                          </td>
                          <td className="w-2/5 py-2.5 pr-3">
                            <Meter value={project.done} max={project.total} label={`${project.name} ilerlemesi`} />
                          </td>
                          <td className="py-2.5 pl-3 text-right tabular-nums text-foreground">{project.open}</td>
                          <td className={cn("py-2.5 pl-3 text-right tabular-nums", project.overdue > 0 ? "font-medium text-danger" : "text-muted")}>
                            {project.overdue}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Panel>
          </div>
        </div>
      ) : null}
    </div>
  );
}

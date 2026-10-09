"use client";

import { ArrowLeft, Pencil, Trash2 } from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useState } from "react";
import { ProjectFormDialog } from "@/components/projects/project-form-dialog";
import { ProjectKeyBadge } from "@/components/projects/project-key-badge";
import { ProjectMembers } from "@/components/projects/project-members";
import { ProjectStatusBadge } from "@/components/projects/project-status-badge";
import { TaskBoard } from "@/components/tasks/task-board";
import { TaskDialog } from "@/components/tasks/task-dialog";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Spinner } from "@/components/ui/spinner";
import { ApiError } from "@/lib/api/errors";
import { projectsApi } from "@/lib/api/projects";
import { useAuth } from "@/lib/auth/auth-provider";
import { hasPermission, PERMISSIONS } from "@/lib/auth/permissions";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { cn } from "@/lib/utils/cn";
import { formatDate, formatDateOnly } from "@/lib/utils/format";

type Tab = "board" | "overview";

const TABS: { id: Tab; label: string }[] = [
  { id: "board", label: "Pano" },
  { id: "overview", label: "Genel bakış" },
];

function BackLink() {
  return (
    <Link
      href="/projects"
      className="inline-flex items-center gap-1.5 rounded text-sm font-medium text-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
    >
      <ArrowLeft className="size-4" aria-hidden="true" />
      Projeler
    </Link>
  );
}

export function ProjectDetailPage({ projectId }: { projectId: string }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const canDelete = hasPermission(user, PERMISSIONS.projectManage);
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [tab, setTab] = useState<Tab>("board");
  const [boardVersion, setBoardVersion] = useState(0);

  const openTaskId = searchParams.get("task");
  const projectPath = `/projects/${encodeURIComponent(projectId)}`;

  const openTask = (taskId: string) =>
    router.replace(`${projectPath}?task=${encodeURIComponent(taskId)}`, { scroll: false });
  const closeTask = () => router.replace(projectPath, { scroll: false });
  const refreshBoard = () => setBoardVersion((current) => current + 1);

  const loadProject = useCallback((signal: AbortSignal) => projectsApi.get(projectId, signal), [projectId]);
  const project = useApiData(loadProject);

  if (project.error instanceof ApiError && project.error.status === 404) {
    return (
      <div className="flex flex-col gap-6">
        <BackLink />
        <div className="rounded-xl border border-border bg-surface px-6 py-16 text-center">
          <h1 className="text-base font-medium text-foreground">Proje bulunamadı</h1>
          <p className="mt-2 text-sm text-muted">Proje silinmiş olabilir ya da bu projeye erişiminiz yok.</p>
        </div>
      </div>
    );
  }

  if (project.error) {
    return (
      <div className="flex flex-col gap-6">
        <BackLink />
        <Alert>
          <span>{getErrorMessage(project.error)}</span>{" "}
          <button type="button" onClick={project.reload} className="font-medium underline">
            Tekrar dene
          </button>
        </Alert>
      </div>
    );
  }

  const data = project.data;

  if (!data || data.id !== projectId) {
    return (
      <div className="flex justify-center py-16" role="status">
        <Spinner className="size-5 text-primary" />
        <span className="sr-only">Proje yükleniyor</span>
      </div>
    );
  }

  const details = [
    { label: "Başlangıç", value: data.startDate ? formatDateOnly(data.startDate) : "Belirtilmemiş" },
    { label: "Bitiş", value: data.endDate ? formatDateOnly(data.endDate) : "Belirtilmemiş" },
    { label: "Oluşturan", value: `${data.createdBy.firstName} ${data.createdBy.lastName}` },
    { label: "Oluşturulma", value: formatDate(data.createdAt) },
  ];

  return (
    <div className="flex flex-col gap-6">
      <BackLink />

      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <ProjectKeyBadge projectKey={data.key} />
            <ProjectStatusBadge status={data.status} />
          </div>
          <h1 className="mt-2 text-2xl font-semibold tracking-tight text-foreground">{data.name}</h1>
        </div>
        <div className="flex shrink-0 gap-2">
          {data.canManage ? (
            <Button variant="secondary" onClick={() => setEditing(true)}>
              <Pencil className="size-4" aria-hidden="true" />
              Düzenle
            </Button>
          ) : null}
          {canDelete ? (
            <Button variant="secondary" onClick={() => setDeleting(true)} aria-label="Projeyi sil">
              <Trash2 className="size-4 text-danger" aria-hidden="true" />
              Sil
            </Button>
          ) : null}
        </div>
      </div>

      <div role="tablist" aria-label="Proje bölümleri" className="flex gap-1 border-b border-border">
        {TABS.map((item) => (
          <button
            key={item.id}
            type="button"
            role="tab"
            id={`tab-${item.id}`}
            aria-selected={tab === item.id}
            aria-controls={`panel-${item.id}`}
            onClick={() => setTab(item.id)}
            className={cn(
              "-mb-px border-b-2 px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-ring",
              tab === item.id
                ? "border-primary text-primary"
                : "border-transparent text-muted hover:text-foreground",
            )}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === "board" ? (
        <div role="tabpanel" id="panel-board" aria-labelledby="tab-board">
          <TaskBoard project={data} refreshKey={boardVersion} onOpenTask={openTask} onChanged={refreshBoard} />
        </div>
      ) : (
      <div
        role="tabpanel"
        id="panel-overview"
        aria-labelledby="tab-overview"
        className="grid gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]"
      >
        <div className="flex flex-col gap-6">
          <section aria-labelledby="description-heading" className="rounded-xl border border-border bg-surface p-5 shadow-xs">
            <h2 id="description-heading" className="text-base font-medium text-foreground">
              Açıklama
            </h2>
            <p className="mt-2 whitespace-pre-line text-sm leading-relaxed text-muted">
              {data.description ?? "Bu proje için açıklama eklenmemiş."}
            </p>
          </section>

          <ProjectMembers project={data} onChanged={project.reload} onLeft={() => router.push("/projects")} />
        </div>

        <dl className="grid h-fit gap-4 rounded-xl border border-border bg-surface p-5 shadow-xs">
          {details.map((detail) => (
            <div key={detail.label}>
              <dt className="text-xs font-medium text-muted">{detail.label}</dt>
              <dd className="mt-0.5 text-sm text-foreground">{detail.value}</dd>
            </div>
          ))}
        </dl>
      </div>
      )}

      <TaskDialog taskId={openTaskId} project={data} onClose={closeTask} onChanged={refreshBoard} />

      <ProjectFormDialog
        open={editing}
        project={data}
        onClose={() => setEditing(false)}
        onSaved={() => {
          setEditing(false);
          project.reload();
        }}
      />

      <ConfirmDialog
        open={deleting}
        title="Projeyi sil"
        description={`${data.key} · ${data.name} silinecek ve listelerden kaldırılacak.`}
        confirmLabel="Projeyi sil"
        tone="danger"
        onConfirm={async () => {
          await projectsApi.remove(data.id);
          router.push("/projects");
        }}
        onClose={() => setDeleting(false)}
      />
    </div>
  );
}

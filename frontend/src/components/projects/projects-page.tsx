"use client";

import { CalendarDays, FolderKanban, Plus, Search, Users } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useState } from "react";
import { ProjectFormDialog } from "@/components/projects/project-form-dialog";
import { ProjectKeyBadge } from "@/components/projects/project-key-badge";
import { ProjectStatusBadge } from "@/components/projects/project-status-badge";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { Pagination } from "@/components/ui/pagination";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { projectsApi } from "@/lib/api/projects";
import { useAuth } from "@/lib/auth/auth-provider";
import { hasPermission, PERMISSIONS } from "@/lib/auth/permissions";
import { useApiData } from "@/lib/hooks/use-api-data";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { getProjectMemberRoleLabel, getProjectStatusLabel, PROJECT_STATUSES } from "@/lib/i18n/projects";
import { formatDateOnly } from "@/lib/utils/format";
import type { ProjectStatus, ProjectSummary } from "@/types/api";

const PAGE_SIZE = 12;

function DateRange({ project }: { project: ProjectSummary }) {
  if (!project.startDate && !project.endDate) {
    return <span>Tarih belirtilmemiş</span>;
  }

  return (
    <span>
      {project.startDate ? formatDateOnly(project.startDate) : "…"} – {project.endDate ? formatDateOnly(project.endDate) : "…"}
    </span>
  );
}

export function ProjectsPage() {
  const router = useRouter();
  const { user } = useAuth();
  const canCreate = hasPermission(user, PERMISSIONS.projectCreate);
  const canViewAll = hasPermission(user, PERMISSIONS.projectViewAll);

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<ProjectStatus | "">("");
  const [memberOnly, setMemberOnly] = useState(false);
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);

  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  const loadProjects = useCallback(
    (signal: AbortSignal) =>
      projectsApi.list(
        {
          page,
          pageSize: PAGE_SIZE,
          search: debouncedSearch || undefined,
          status: status || undefined,
          memberOnly,
        },
        signal,
      ),
    [page, debouncedSearch, status, memberOnly],
  );

  const projects = useApiData(loadProjects);
  const filtered = debouncedSearch !== "" || status !== "" || memberOnly;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Projeler"
        description={canViewAll ? "Şirketinizdeki tüm projeler." : "Üyesi olduğunuz projeler."}
        actions={
          canCreate ? (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="size-4" aria-hidden="true" />
              Yeni proje
            </Button>
          ) : null
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted"
            aria-hidden="true"
          />
          <Input
            type="search"
            aria-label="Proje ara"
            placeholder="Proje adı veya kısa ad ara"
            className="pl-9"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </div>
        <Select
          aria-label="Duruma göre filtrele"
          className="sm:w-48"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value as ProjectStatus | "");
            setPage(1);
          }}
        >
          <option value="">Tüm durumlar</option>
          {PROJECT_STATUSES.map((value) => (
            <option key={value} value={value}>
              {getProjectStatusLabel(value)}
            </option>
          ))}
        </Select>
        {canViewAll ? (
          <Checkbox
            label="Sadece üyesi olduklarım"
            className="shrink-0"
            checked={memberOnly}
            onChange={(event) => {
              setMemberOnly(event.target.checked);
              setPage(1);
            }}
          />
        ) : null}
      </div>

      {projects.error ? (
        <Alert>
          <span>{getErrorMessage(projects.error)}</span>{" "}
          <button type="button" onClick={projects.reload} className="font-medium underline">
            Tekrar dene
          </button>
        </Alert>
      ) : null}

      {projects.data === undefined && projects.loading ? (
        <div className="flex justify-center py-16" role="status">
          <Spinner className="size-5 text-primary" />
          <span className="sr-only">Projeler yükleniyor</span>
        </div>
      ) : null}

      {projects.data && projects.data.items.length === 0 ? (
        <div className="flex flex-col items-center rounded-xl border border-dashed border-border-strong bg-surface px-6 py-16 text-center">
          <FolderKanban className="size-8 text-muted" aria-hidden="true" />
          <h2 className="mt-4 text-base font-medium text-foreground">
            {filtered ? "Filtrelerle eşleşen proje yok" : "Henüz proje yok"}
          </h2>
          <p className="mt-2 max-w-sm text-sm text-muted">
            {filtered
              ? "Arama ya da filtreleri değiştirip tekrar deneyin."
              : canCreate
                ? "İlk projenizi oluşturup ekibinizi ekleyin."
                : "Bir projeye eklendiğinizde burada görünecek."}
          </p>
          {!filtered && canCreate ? (
            <Button className="mt-6" onClick={() => setCreateOpen(true)}>
              <Plus className="size-4" aria-hidden="true" />
              Yeni proje
            </Button>
          ) : null}
        </div>
      ) : null}

      {projects.data && projects.data.items.length > 0 ? (
        <ul className="grid gap-4 md:grid-cols-2 xl:grid-cols-3" aria-busy={projects.loading}>
          {projects.data.items.map((project) => (
            <li key={project.id}>
              <Link
                href={`/projects/${project.id}`}
                className="flex h-full flex-col rounded-xl border border-border bg-surface p-5 shadow-xs transition-colors hover:border-border-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
              >
                <div className="flex items-center justify-between gap-3">
                  <ProjectKeyBadge projectKey={project.key} />
                  <ProjectStatusBadge status={project.status} />
                </div>
                <h2 className="mt-3 line-clamp-2 text-base font-medium text-foreground">{project.name}</h2>
                <p className="mt-1 text-sm text-muted">
                  {project.lead ? `Lider: ${project.lead.firstName} ${project.lead.lastName}` : "Lider atanmamış"}
                </p>
                <div className="mt-auto flex flex-wrap items-center gap-x-4 gap-y-2 pt-5 text-sm text-muted">
                  <span className="flex items-center gap-1.5">
                    <CalendarDays className="size-4" aria-hidden="true" />
                    <DateRange project={project} />
                  </span>
                  <span className="flex items-center gap-1.5">
                    <Users className="size-4" aria-hidden="true" />
                    {project.memberCount} üye
                  </span>
                  {project.currentUserRole ? (
                    <Badge tone="primary">{getProjectMemberRoleLabel(project.currentUserRole)}</Badge>
                  ) : null}
                </div>
              </Link>
            </li>
          ))}
        </ul>
      ) : null}

      {projects.data ? (
        <Pagination
          page={projects.data.page}
          pageSize={projects.data.pageSize}
          totalCount={projects.data.totalCount}
          onPageChange={setPage}
        />
      ) : null}

      <ProjectFormDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSaved={(project) => {
          setCreateOpen(false);
          router.push(`/projects/${project.id}`);
        }}
      />
    </div>
  );
}

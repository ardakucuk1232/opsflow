"use client";

import { CalendarDays, ListChecks, Search } from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { TaskPriorityBadge, TaskStatusBadge } from "@/components/tasks/task-badges";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { Pagination } from "@/components/ui/pagination";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { projectsApi } from "@/lib/api/projects";
import { tasksApi } from "@/lib/api/tasks";
import { useAuth } from "@/lib/auth/auth-provider";
import { getInitials } from "@/lib/auth/roles";
import { useApiData } from "@/lib/hooks/use-api-data";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { getTaskPriorityLabel, getTaskSortLabel, getTaskStatusLabel, TASK_PRIORITIES, TASK_STATUSES } from "@/lib/i18n/tasks";
import { todayIso } from "@/lib/tasks/board";
import {
  DEFAULT_TASK_FILTERS,
  hasActiveFilters,
  isOverdue,
  readTaskFilters,
  TASK_PAGE_SIZE,
  TASK_SORTS,
  toTaskListParams,
  writeTaskFilters,
  type TaskFilters,
} from "@/lib/tasks/task-filters";
import { cn } from "@/lib/utils/cn";
import { formatDateOnly } from "@/lib/utils/format";
import type { TaskListItem } from "@/types/api";

const PROJECT_OPTIONS_LIMIT = 100;

function TaskRow({ task, today }: { task: TaskListItem; today: string }) {
  const overdue = isOverdue(task, today);

  return (
    <Link
      href={`/projects/${task.projectId}?task=${task.id}`}
      className="grid gap-x-4 gap-y-2 px-4 py-3 transition-colors hover:bg-surface-muted focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring md:grid-cols-[minmax(0,1fr)_8.5rem_6rem_9rem_7rem] md:items-center"
    >
      <span className="min-w-0">
        <span className="block truncate text-sm font-medium text-foreground">{task.title}</span>
        <span className="mt-0.5 block truncate text-xs text-muted">
          <span className="font-mono">{task.key}</span> · {task.projectName}
        </span>
      </span>
      <span className="flex flex-wrap items-center gap-2 md:contents">
        <span>
          <TaskStatusBadge status={task.status} />
        </span>
        <span>
          <TaskPriorityBadge priority={task.priority} />
        </span>
        <span className="flex min-w-0 items-center gap-2 text-sm text-muted">
          {task.assignee ? (
            <>
              <span
                className="flex size-6 shrink-0 items-center justify-center rounded-full bg-primary-soft text-[10px] font-semibold text-primary"
                aria-hidden="true"
              >
                {getInitials(task.assignee.firstName, task.assignee.lastName)}
              </span>
              <span className="truncate">
                {task.assignee.firstName} {task.assignee.lastName}
              </span>
            </>
          ) : (
            <span>Atanmamış</span>
          )}
        </span>
        <span className={cn("flex items-center gap-1.5 text-sm", overdue ? "font-medium text-danger" : "text-muted")}>
          <CalendarDays className="size-4 shrink-0 md:hidden" aria-hidden="true" />
          {task.dueDate ? formatDateOnly(task.dueDate) : <span className="md:invisible">Tarih yok</span>}
          {overdue ? <span className="sr-only"> (gecikti)</span> : null}
        </span>
      </span>
    </Link>
  );
}

export function TasksPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const filters = useMemo(() => readTaskFilters(new URLSearchParams(searchParams.toString())), [searchParams]);
  const today = useMemo(() => todayIso(), []);

  const [search, setSearch] = useState(filters.q);
  const [syncedQuery, setSyncedQuery] = useState(filters.q);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  if (filters.q !== syncedQuery) {
    setSyncedQuery(filters.q);

    if (filters.q !== search.trim()) {
      setSearch(filters.q);
    }
  }

  const update = useCallback(
    (changes: Partial<TaskFilters>) => {
      const query = writeTaskFilters({ ...filters, page: 1, ...changes });
      router.replace(query ? `${pathname}?${query}` : pathname, { scroll: false });
    },
    [filters, pathname, router],
  );

  const latest = useRef({ filters, update });

  useEffect(() => {
    latest.current = { filters, update };
  });

  useEffect(() => {
    const { filters: current, update: apply } = latest.current;

    if (debouncedSearch !== current.q.trim()) {
      apply({ q: debouncedSearch });
    }
  }, [debouncedSearch]);

  const loadProjects = useCallback(
    (signal: AbortSignal) => projectsApi.list({ page: 1, pageSize: PROJECT_OPTIONS_LIMIT }, signal),
    [],
  );
  const projects = useApiData(loadProjects);

  const params = useMemo(() => toTaskListParams(filters, user?.id ?? "", today), [filters, user?.id, today]);
  const loadTasks = useCallback((signal: AbortSignal) => tasksApi.list(params, signal), [params]);
  const tasks = useApiData(loadTasks);

  const filtered = hasActiveFilters(filters);

  function clearFilters() {
    setSearch("");
    update({ ...DEFAULT_TASK_FILTERS, sort: filters.sort });
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Görevler" description="Erişebildiğiniz projelerdeki tüm görevler." />

      <div className="flex flex-col gap-3">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" aria-hidden="true" />
          <Input
            type="search"
            aria-label="Görev ara"
            placeholder="Başlık veya görev kodu (ör. WEB-12) ara"
            className="pl-9"
            maxLength={100}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>

        <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
          <Select aria-label="Proje" value={filters.project} onChange={(event) => update({ project: event.target.value })}>
            <option value="">Tüm projeler</option>
            {projects.data?.items.map((project) => (
              <option key={project.id} value={project.id}>
                {project.key} · {project.name}
              </option>
            ))}
          </Select>
          <Select
            aria-label="Durum"
            value={filters.status}
            onChange={(event) => update({ status: event.target.value as TaskFilters["status"] })}
          >
            <option value="">Tüm durumlar</option>
            <option value="open">Açık işler</option>
            {TASK_STATUSES.map((status) => (
              <option key={status} value={status}>
                {getTaskStatusLabel(status)}
              </option>
            ))}
          </Select>
          <Select
            aria-label="Öncelik"
            value={filters.priority}
            onChange={(event) => update({ priority: event.target.value as TaskFilters["priority"] })}
          >
            <option value="">Tüm öncelikler</option>
            {TASK_PRIORITIES.map((priority) => (
              <option key={priority} value={priority}>
                {getTaskPriorityLabel(priority)}
              </option>
            ))}
          </Select>
          <Select
            aria-label="Atanan kişi"
            value={filters.assignee}
            onChange={(event) => update({ assignee: event.target.value as TaskFilters["assignee"] })}
          >
            <option value="">Herkes</option>
            <option value="me">Bana atananlar</option>
            <option value="none">Atanmamış</option>
          </Select>
          <Select
            aria-label="Bitiş tarihi"
            value={filters.due}
            onChange={(event) => update({ due: event.target.value as TaskFilters["due"] })}
          >
            <option value="">Tüm tarihler</option>
            <option value="overdue">Gecikmiş</option>
            <option value="week">Bu hafta bitecek</option>
          </Select>
          <Select
            aria-label="Sıralama"
            value={filters.sort}
            onChange={(event) => update({ sort: event.target.value as TaskFilters["sort"], page: filters.page })}
          >
            {TASK_SORTS.map((sort) => (
              <option key={sort} value={sort}>
                {getTaskSortLabel(sort)}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {tasks.error ? (
        <Alert>
          <span>{getErrorMessage(tasks.error)}</span>{" "}
          <button type="button" onClick={tasks.reload} className="font-medium underline">
            Tekrar dene
          </button>
        </Alert>
      ) : null}

      {tasks.data === undefined && tasks.loading ? (
        <div className="flex justify-center py-16" role="status">
          <Spinner className="size-5 text-primary" />
          <span className="sr-only">Görevler yükleniyor</span>
        </div>
      ) : null}

      {tasks.data && tasks.data.items.length === 0 ? (
        <div className="flex flex-col items-center rounded-xl border border-dashed border-border-strong bg-surface px-6 py-16 text-center">
          <ListChecks className="size-8 text-muted" aria-hidden="true" />
          <h2 className="mt-4 text-base font-medium text-foreground">
            {filtered ? "Filtrelerle eşleşen görev yok" : "Henüz görev yok"}
          </h2>
          <p className="mt-2 max-w-sm text-sm text-muted">
            {filtered
              ? "Arama ya da filtreleri değiştirip tekrar deneyin."
              : "Projelerde görev oluşturulduğunda burada listelenir."}
          </p>
          {filtered ? (
            <Button variant="secondary" className="mt-6" onClick={clearFilters}>
              Filtreleri temizle
            </Button>
          ) : null}
        </div>
      ) : null}

      {tasks.data && tasks.data.items.length > 0 ? (
        <div className="flex flex-col gap-4">
          <div className="overflow-hidden rounded-xl border border-border bg-surface shadow-xs" aria-busy={tasks.loading}>
            <div
              className="hidden gap-x-4 border-b border-border bg-surface-muted px-4 py-2 text-xs font-medium text-muted md:grid md:grid-cols-[minmax(0,1fr)_8.5rem_6rem_9rem_7rem]"
              aria-hidden="true"
            >
              <span>Görev</span>
              <span>Durum</span>
              <span>Öncelik</span>
              <span>Atanan</span>
              <span>Bitiş</span>
            </div>
            <ul className="divide-y divide-border">
              {tasks.data.items.map((task) => (
                <li key={task.id}>
                  <TaskRow task={task} today={today} />
                </li>
              ))}
            </ul>
          </div>
          <Pagination
            page={filters.page}
            pageSize={TASK_PAGE_SIZE}
            totalCount={tasks.data.totalCount}
            onPageChange={(page) => update({ page })}
          />
        </div>
      ) : null}
    </div>
  );
}

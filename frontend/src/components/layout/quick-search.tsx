"use client";

import { FolderKanban, ListChecks, Search, X } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { TaskStatusBadge } from "@/components/tasks/task-badges";
import { Spinner } from "@/components/ui/spinner";
import { SEARCH_MIN_LENGTH, searchApi } from "@/lib/api/search";
import { useApiData } from "@/lib/hooks/use-api-data";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { cn } from "@/lib/utils/cn";
import type { SearchResult } from "@/types/api";

type Option = {
  id: string;
  href: string;
  label: string;
  detail: string;
  kind: "project" | "task" | "all";
  result?: SearchResult["tasks"][number];
};

const EMPTY: SearchResult = { projects: [], tasks: [] };

function optionsFor(term: string, result: SearchResult | undefined): Option[] {
  const projects = (result?.projects ?? []).map<Option>((project) => ({
    id: `project-${project.id}`,
    href: `/projects/${project.id}`,
    label: project.name,
    detail: project.key,
    kind: "project",
  }));

  const tasks = (result?.tasks ?? []).map<Option>((task) => ({
    id: `task-${task.id}`,
    href: `/projects/${task.projectId}?task=${task.id}`,
    label: task.title,
    detail: `${task.key} · ${task.projectName}`,
    kind: "task",
    result: task,
  }));

  return [
    ...projects,
    ...tasks,
    {
      id: "all",
      href: `/tasks?${new URLSearchParams({ q: term })}`,
      label: `“${term}” için tüm görevleri listele`,
      detail: "",
      kind: "all",
    },
  ];
}

export function QuickSearch() {
  const router = useRouter();
  const [term, setTerm] = useState("");
  const [open, setOpen] = useState(false);
  const [expanded, setExpanded] = useState(false);
  const [active, setActive] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const listId = useId();

  const query = useDebouncedValue(term.trim(), 250);
  const ready = query.length >= SEARCH_MIN_LENGTH;

  const load = useCallback(
    (signal: AbortSignal) => (ready ? searchApi.search(query, signal) : Promise.resolve(EMPTY)),
    [query, ready],
  );
  const results = useApiData(load);

  const options = ready ? optionsFor(term.trim(), results.data) : [];
  const showPanel = open && term.trim().length >= SEARCH_MIN_LENGTH;
  const activeIndex = Math.min(active, Math.max(options.length - 1, 0));

  useEffect(() => {
    function handleShortcut(event: globalThis.KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setExpanded(true);
        inputRef.current?.focus();
        setOpen(true);
      }
    }

    document.addEventListener("keydown", handleShortcut);

    return () => document.removeEventListener("keydown", handleShortcut);
  }, []);

  useEffect(() => {
    if (!open && !expanded) {
      return;
    }

    function handlePointerDown(event: PointerEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
        setExpanded(false);
      }
    }

    document.addEventListener("pointerdown", handlePointerDown);

    return () => document.removeEventListener("pointerdown", handlePointerDown);
  }, [open, expanded]);

  function close() {
    setOpen(false);
    setExpanded(false);
    setTerm("");
    setActive(0);
  }

  function go(option: Option) {
    close();
    inputRef.current?.blur();
    router.push(option.href);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      close();
      inputRef.current?.blur();
      return;
    }

    if (!showPanel || options.length === 0) {
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActive((activeIndex + 1) % options.length);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActive((activeIndex - 1 + options.length) % options.length);
    } else if (event.key === "Enter") {
      event.preventDefault();
      go(options[activeIndex]);
    }
  }

  const searching = ready && (results.loading || query !== term.trim());
  const noMatches = !searching && results.data !== undefined && options.length === 1;

  return (
    <div ref={containerRef} className="sm:relative">
      <button
        type="button"
        aria-label="Ara"
        onClick={() => {
          setExpanded(true);
          requestAnimationFrame(() => inputRef.current?.focus());
        }}
        className={cn(
          "flex size-9 items-center justify-center rounded-lg text-muted hover:bg-surface-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring sm:hidden",
          expanded && "hidden",
        )}
      >
        <Search className="size-5" aria-hidden="true" />
      </button>

      <div
        className={cn(
          "items-center gap-2 sm:flex",
          expanded ? "fixed inset-x-0 top-0 z-30 flex h-16 border-b border-border bg-surface px-4 sm:static sm:h-auto sm:border-0 sm:bg-transparent sm:p-0" : "hidden",
        )}
      >
        <div className="relative flex-1 sm:w-64 lg:w-80">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" aria-hidden="true" />
          <input
            ref={inputRef}
            type="text"
            role="combobox"
            aria-label="Proje veya görev ara"
            aria-expanded={showPanel}
            aria-controls={listId}
            aria-autocomplete="list"
            aria-activedescendant={showPanel && options.length > 0 ? `${listId}-${options[activeIndex].id}` : undefined}
            placeholder="Ara… (Ctrl+K)"
            autoComplete="off"
            maxLength={100}
            value={term}
            onChange={(event) => {
              setTerm(event.target.value);
              setActive(0);
              setOpen(true);
            }}
            onFocus={() => setOpen(true)}
            onKeyDown={handleKeyDown}
            className="h-9 w-full rounded-lg border border-border-strong bg-surface pl-9 pr-3 text-sm text-foreground shadow-xs placeholder:text-muted/70 focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-ring"
          />
        </div>
        {expanded ? (
          <button
            type="button"
            aria-label="Aramayı kapat"
            onClick={close}
            className="flex size-9 shrink-0 items-center justify-center rounded-lg text-muted hover:bg-surface-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring sm:hidden"
          >
            <X className="size-5" aria-hidden="true" />
          </button>
        ) : null}
      </div>

      {showPanel ? (
        <div className="fixed inset-x-4 top-16 z-30 overflow-hidden rounded-xl border border-border bg-surface shadow-lg sm:absolute sm:inset-x-auto sm:right-0 sm:top-auto sm:mt-2 sm:w-96">
          {searching && results.data === undefined ? (
            <div className="flex justify-center py-6" role="status">
              <Spinner className="size-4 text-primary" />
              <span className="sr-only">Aranıyor</span>
            </div>
          ) : null}

          {noMatches ? <p className="px-4 pt-4 text-sm text-muted">Eşleşen proje veya görev yok.</p> : null}

          <ul id={listId} role="listbox" aria-label="Arama sonuçları" className="max-h-96 overflow-y-auto p-1.5" aria-busy={searching}>
            {options.map((option, index) => (
              <li
                key={option.id}
                id={`${listId}-${option.id}`}
                role="option"
                aria-selected={index === activeIndex}
                onPointerDown={(event) => event.preventDefault()}
                onClick={() => go(option)}
                onMouseMove={() => setActive(index)}
                className={cn(
                  "flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2 text-sm",
                  index === activeIndex ? "bg-surface-muted" : null,
                  option.kind === "all" && "text-primary",
                )}
              >
                {option.kind === "project" ? <FolderKanban className="size-4 shrink-0 text-muted" aria-hidden="true" /> : null}
                {option.kind === "task" ? <ListChecks className="size-4 shrink-0 text-muted" aria-hidden="true" /> : null}
                {option.kind === "all" ? <Search className="size-4 shrink-0" aria-hidden="true" /> : null}
                <span className="min-w-0 flex-1">
                  <span className="block truncate font-medium">{option.label}</span>
                  {option.detail ? <span className="block truncate text-xs text-muted">{option.detail}</span> : null}
                </span>
                {option.result ? <TaskStatusBadge status={option.result.status} /> : null}
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}

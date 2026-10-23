"use client";

import { ChevronDown, History } from "lucide-react";
import { useCallback, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { Pagination } from "@/components/ui/pagination";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { auditLogsApi } from "@/lib/api/audit-logs";
import { useApiData } from "@/lib/hooks/use-api-data";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import {
  AUDIT_ACTIONS,
  AUDIT_ENTITIES,
  formatAuditValue,
  getAuditActionLabel,
  getAuditActionTone,
  getAuditEntityLabel,
  getAuditFieldLabel,
} from "@/lib/i18n/audit";
import { cn } from "@/lib/utils/cn";
import { formatDateTime } from "@/lib/utils/format";
import type { AuditAction, AuditLogEntry } from "@/types/api";

const PAGE_SIZE = 25;

function AuditRow({ entry }: { entry: AuditLogEntry }) {
  const [open, setOpen] = useState(false);
  const actor = entry.user ? `${entry.user.firstName} ${entry.user.lastName}` : "Sistem";
  const detailsId = `audit-${entry.id}`;

  return (
    <li>
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        aria-controls={detailsId}
        className="grid w-full gap-x-4 gap-y-1 px-4 py-3 text-left transition-colors hover:bg-surface-muted focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring md:grid-cols-[9.5rem_minmax(0,1fr)_10rem_1rem] md:items-center"
      >
        <span className="text-xs tabular-nums text-muted">{formatDateTime(entry.createdAt)}</span>
        <span className="flex min-w-0 items-center gap-2">
          <Badge tone={getAuditActionTone(entry.action)}>{getAuditActionLabel(entry.action)}</Badge>
          <span className="text-sm text-muted">{getAuditEntityLabel(entry.entityName)}</span>
          <span className="min-w-0 truncate text-sm font-medium text-foreground">{entry.entityLabel ?? `#${entry.entityId.slice(0, 8)}`}</span>
        </span>
        <span className="truncate text-sm text-muted">{actor}</span>
        <ChevronDown className={cn("hidden size-4 text-muted transition-transform md:block", open && "rotate-180")} aria-hidden="true" />
      </button>

      {open ? (
        <div id={detailsId} className="border-t border-border bg-surface-muted/50 px-4 py-3">
          {entry.changes.length === 0 ? (
            <p className="text-sm text-muted">Kayıtlı alan değişikliği yok.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted">
                    <th className="py-1.5 pr-4 font-medium">Alan</th>
                    {entry.action !== "Created" ? <th className="py-1.5 pr-4 font-medium">Önce</th> : null}
                    {entry.action !== "Deleted" ? <th className="py-1.5 font-medium">Sonra</th> : null}
                  </tr>
                </thead>
                <tbody>
                  {entry.changes.map((change) => (
                    <tr key={change.field} className="align-top">
                      <td className="py-1.5 pr-4 text-muted">{getAuditFieldLabel(change.field)}</td>
                      {entry.action !== "Created" ? (
                        <td className="break-words py-1.5 pr-4 text-foreground">{formatAuditValue(change.oldValue, change.field)}</td>
                      ) : null}
                      {entry.action !== "Deleted" ? (
                        <td className="break-words py-1.5 text-foreground">{formatAuditValue(change.newValue, change.field)}</td>
                      ) : null}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <p className="mt-2 text-xs text-muted">
            {entry.user ? `${actor} (${entry.user.email})` : "Sistem işlemi"}
            {entry.ipAddress ? ` · IP ${entry.ipAddress}` : ""}
          </p>
        </div>
      ) : null}
    </li>
  );
}

export function AuditLogPage() {
  const [entityName, setEntityName] = useState("");
  const [action, setAction] = useState<"" | AuditAction>("");
  const [search, setSearch] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  const load = useCallback(
    (signal: AbortSignal) =>
      auditLogsApi.list(
        {
          page,
          pageSize: PAGE_SIZE,
          entityName: entityName || undefined,
          action: action || undefined,
          search: debouncedSearch || undefined,
          from: from || undefined,
          to: to || undefined,
        },
        signal,
      ),
    [page, entityName, action, debouncedSearch, from, to],
  );
  const logs = useApiData(load);

  function change<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value);
      setPage(1);
    };
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Denetim kaydı" description="Şirketinizde kimin, neyi, ne zaman değiştirdiği." />

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <Input
          type="search"
          aria-label="Kayıt ara"
          placeholder="Görev, proje veya kişi ara"
          maxLength={100}
          value={search}
          onChange={(event) => change(setSearch)(event.target.value)}
        />
        <Select aria-label="Kayıt türü" value={entityName} onChange={(event) => change(setEntityName)(event.target.value)}>
          <option value="">Tüm kayıt türleri</option>
          {AUDIT_ENTITIES.map((entity) => (
            <option key={entity.value} value={entity.value}>
              {entity.label}
            </option>
          ))}
        </Select>
        <Select aria-label="İşlem" value={action} onChange={(event) => change(setAction)(event.target.value as "" | AuditAction)}>
          <option value="">Tüm işlemler</option>
          {AUDIT_ACTIONS.map((value) => (
            <option key={value} value={value}>
              {getAuditActionLabel(value)}
            </option>
          ))}
        </Select>
        <Input type="date" aria-label="Başlangıç tarihi" value={from} max={to || undefined} onChange={(event) => change(setFrom)(event.target.value)} />
        <Input type="date" aria-label="Bitiş tarihi" value={to} min={from || undefined} onChange={(event) => change(setTo)(event.target.value)} />
      </div>

      {logs.error ? (
        <Alert>
          <span>{getErrorMessage(logs.error)}</span>{" "}
          <button type="button" onClick={logs.reload} className="font-medium underline">
            Tekrar dene
          </button>
        </Alert>
      ) : null}

      {logs.data === undefined && logs.loading ? (
        <div className="flex justify-center py-16" role="status">
          <Spinner className="size-5 text-primary" />
          <span className="sr-only">Kayıtlar yükleniyor</span>
        </div>
      ) : null}

      {logs.data && logs.data.items.length === 0 ? (
        <div className="flex flex-col items-center rounded-xl border border-dashed border-border-strong bg-surface px-6 py-16 text-center">
          <History className="size-8 text-muted" aria-hidden="true" />
          <h2 className="mt-4 text-base font-medium text-foreground">Kayıt bulunamadı</h2>
          <p className="mt-2 max-w-sm text-sm text-muted">Filtreleri değiştirip tekrar deneyin.</p>
        </div>
      ) : null}

      {logs.data && logs.data.items.length > 0 ? (
        <div className="flex flex-col gap-4">
          <ul
            className={cn("divide-y divide-border overflow-hidden rounded-xl border border-border bg-surface shadow-xs", logs.loading && "opacity-60")}
            aria-busy={logs.loading}
          >
            {logs.data.items.map((entry) => (
              <AuditRow key={entry.id} entry={entry} />
            ))}
          </ul>
          <Pagination page={page} pageSize={PAGE_SIZE} totalCount={logs.data.totalCount} onPageChange={setPage} />
        </div>
      ) : null}
    </div>
  );
}

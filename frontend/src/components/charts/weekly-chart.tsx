"use client";

import { useId, useState } from "react";
import { formatShortDate } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";

export type WeeklyPoint = { weekStart: string; created: number; completed: number };

const SERIES = [
  { key: "created", label: "Oluşturulan", className: "bg-chart-1" },
  { key: "completed", label: "Tamamlanan", className: "bg-chart-2" },
] as const;

export function ticksFor(max: number): number[] {
  const target = Math.max(max, 1) / 3;
  const magnitude = 10 ** Math.floor(Math.log10(target));
  const nice = [1, 2, 5, 10].map((factor) => factor * magnitude).find((candidate) => candidate >= target) ?? magnitude * 10;
  const step = Math.max(nice, 1);
  const count = Math.max(Math.ceil(max / step), 1);

  return Array.from({ length: count + 1 }, (_, index) => index * step);
}

export function WeeklyChart({ points }: { points: WeeklyPoint[] }) {
  const [active, setActive] = useState<number | null>(null);
  const [showTable, setShowTable] = useState(false);
  const tableId = useId();

  const ticks = ticksFor(Math.max(...points.flatMap((p) => [p.created, p.completed]), 0));
  const top = ticks[ticks.length - 1];

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <ul className="flex flex-wrap gap-4 text-xs text-muted" aria-label="Seriler">
          {SERIES.map((series) => (
            <li key={series.key} className="flex items-center gap-1.5">
              <span className={cn("size-2.5 rounded-[2px]", series.className)} aria-hidden="true" />
              {series.label}
            </li>
          ))}
        </ul>
        <button
          type="button"
          onClick={() => setShowTable((value) => !value)}
          aria-expanded={showTable}
          aria-controls={tableId}
          className="rounded-md text-xs font-medium text-primary hover:underline focus-visible:outline-2 focus-visible:outline-ring"
        >
          {showTable ? "Grafiği göster" : "Tablo olarak göster"}
        </button>
      </div>

      {showTable ? (
        <div id={tableId} className="mt-4 overflow-x-auto">
          <table className="w-full text-sm">
            <caption className="sr-only">Haftalık oluşturulan ve tamamlanan görevler</caption>
            <thead>
              <tr className="text-left text-xs text-muted">
                <th className="py-2 font-medium">Hafta</th>
                <th className="py-2 text-right font-medium">Oluşturulan</th>
                <th className="py-2 text-right font-medium">Tamamlanan</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {points.map((point) => (
                <tr key={point.weekStart}>
                  <td className="py-2 text-foreground">{formatShortDate(point.weekStart)} haftası</td>
                  <td className="py-2 text-right tabular-nums text-foreground">{point.created}</td>
                  <td className="py-2 text-right tabular-nums text-foreground">{point.completed}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="mt-4 grid grid-cols-[auto_minmax(0,1fr)] gap-x-2">
          <div className="relative h-44 text-right text-[11px] tabular-nums text-muted" aria-hidden="true">
            {ticks.map((tick) => (
              <span key={tick} className="absolute right-0 translate-y-1/2 leading-none" style={{ bottom: `${(tick / top) * 100}%` }}>
                {tick}
              </span>
            ))}
          </div>
          <div className="relative h-44">
            {ticks.map((tick) => (
              <span
                key={tick}
                className={cn("absolute inset-x-0 h-px", tick === 0 ? "bg-border-strong" : "bg-border")}
                style={{ bottom: `${(tick / top) * 100}%` }}
                aria-hidden="true"
              />
            ))}
            <ol className="relative flex h-full items-end" aria-label="Haftalık oluşturulan ve tamamlanan görevler">
              {points.map((point, index) => (
                <li
                  key={point.weekStart}
                  tabIndex={0}
                  aria-label={`${formatShortDate(point.weekStart)} haftası: ${point.created} oluşturulan, ${point.completed} tamamlanan`}
                  onPointerEnter={() => setActive(index)}
                  onPointerLeave={() => setActive(null)}
                  onFocus={() => setActive(index)}
                  onBlur={() => setActive(null)}
                  className="relative flex h-full flex-1 items-end justify-center gap-0.5 rounded-sm outline-none focus-visible:bg-surface-muted"
                >
                  <span
                    className={cn("w-[min(12px,30%)] rounded-t-[4px] bg-chart-1 transition-opacity", active !== null && active !== index && "opacity-40")}
                    style={{ height: `${(point.created / top) * 100}%` }}
                    aria-hidden="true"
                  />
                  <span
                    className={cn("w-[min(12px,30%)] rounded-t-[4px] bg-chart-2 transition-opacity", active !== null && active !== index && "opacity-40")}
                    style={{ height: `${(point.completed / top) * 100}%` }}
                    aria-hidden="true"
                  />
                  {active === index ? (
                    <span
                      role="tooltip"
                      className={cn(
                        "pointer-events-none absolute bottom-full z-10 mb-2 w-max rounded-lg border border-border bg-surface px-3 py-2 text-xs shadow-lg",
                        index < points.length / 2 ? "left-0" : "right-0",
                      )}
                    >
                      <span className="block font-medium text-foreground">{formatShortDate(point.weekStart)} haftası</span>
                      {SERIES.map((series) => (
                        <span key={series.key} className="mt-1 flex items-center gap-2">
                          <span className={cn("h-0.5 w-3", series.className)} aria-hidden="true" />
                          <span className="font-semibold tabular-nums text-foreground">{point[series.key]}</span>
                          <span className="text-muted">{series.label}</span>
                        </span>
                      ))}
                    </span>
                  ) : null}
                </li>
              ))}
            </ol>
          </div>
          <span aria-hidden="true" />
          <div className="mt-2 flex text-[11px] text-muted" aria-hidden="true">
            {points.map((point, index) => (
              <span key={point.weekStart} className={cn("flex-1 text-center", index % 2 === 1 && "hidden sm:block")}>
                {formatShortDate(point.weekStart)}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

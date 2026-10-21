import { cn } from "@/lib/utils/cn";

export type BarListItem = {
  key: string;
  label: string;
  value: number;
  detail?: string;
};

type BarListProps = {
  items: BarListItem[];
  label: string;
  emptyText?: string;
};

export function BarList({ items, label, emptyText = "Gösterilecek veri yok." }: BarListProps) {
  const max = Math.max(...items.map((item) => item.value), 0);

  if (items.length === 0) {
    return <p className="py-6 text-center text-sm text-muted">{emptyText}</p>;
  }

  return (
    <ul aria-label={label} className="flex flex-col gap-3">
      {items.map((item) => (
        <li key={item.key} className="grid grid-cols-[minmax(0,9rem)_minmax(0,1fr)] items-center gap-3 text-sm">
          <span className="truncate text-muted" title={item.label}>
            {item.label}
          </span>
          <span className="flex min-w-0 items-center gap-2">
            <span className="h-3 min-w-0 flex-1" aria-hidden="true">
              <span
                className={cn("block h-full rounded-r-[4px] bg-chart-1", item.value === 0 && "opacity-0")}
                style={{ width: max === 0 ? "0%" : `${(item.value / max) * 100}%` }}
              />
            </span>
            <span className="shrink-0 font-medium tabular-nums text-foreground">
              {item.value}
              {item.detail ? <span className="ml-1 font-normal text-muted">{item.detail}</span> : null}
            </span>
          </span>
        </li>
      ))}
    </ul>
  );
}

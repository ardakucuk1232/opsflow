export function Meter({ value, max, label }: { value: number; max: number; label: string }) {
  const percent = max === 0 ? 0 : Math.round((value / max) * 100);

  return (
    <span className="flex items-center gap-2">
      <span
        role="meter"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        aria-valuetext={`%${percent}`}
        className="h-2 w-full min-w-16 overflow-hidden rounded-full bg-chart-track"
      >
        <span className="block h-full rounded-full bg-chart-1" style={{ width: `${percent}%` }} />
      </span>
      <span className="w-10 shrink-0 text-right text-xs tabular-nums text-muted">%{percent}</span>
    </span>
  );
}

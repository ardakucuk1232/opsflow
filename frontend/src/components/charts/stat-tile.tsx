import type { LucideIcon } from "lucide-react";
import Link from "next/link";
import { cn } from "@/lib/utils/cn";

type StatTileProps = {
  label: string;
  value: number;
  icon: LucideIcon;
  href?: string;
  tone?: "default" | "danger";
  hint?: string;
};

const numberFormatter = new Intl.NumberFormat("tr-TR");

export function StatTile({ label, value, icon: Icon, href, tone = "default", hint }: StatTileProps) {
  const body = (
    <>
      <span className={cn("flex items-center gap-2 text-sm", tone === "danger" && value > 0 ? "text-danger" : "text-muted")}>
        <Icon className="size-4 shrink-0" aria-hidden="true" />
        {label}
      </span>
      <span className="mt-2 block text-2xl font-semibold tracking-tight text-foreground">{numberFormatter.format(value)}</span>
      {hint ? <span className="mt-1 block text-xs text-muted">{hint}</span> : null}
    </>
  );

  const className = "block rounded-xl border border-border bg-surface p-5 shadow-xs";

  return href ? (
    <Link
      href={href}
      className={cn(className, "transition-colors hover:border-border-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring")}
    >
      {body}
    </Link>
  ) : (
    <div className={className}>{body}</div>
  );
}

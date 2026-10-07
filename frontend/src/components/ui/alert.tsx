import { CircleAlert, CircleCheck, Info } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

type Tone = "danger" | "success" | "info";

const TONE_CLASSES: Record<Tone, string> = {
  danger: "border-danger/30 bg-danger-soft text-danger",
  success: "border-success/30 bg-success-soft text-success",
  info: "border-primary/30 bg-primary-soft text-primary",
};

const TONE_ICONS = {
  danger: CircleAlert,
  success: CircleCheck,
  info: Info,
} as const;

export function Alert({ tone = "danger", children }: { tone?: Tone; children: ReactNode }) {
  const Icon = TONE_ICONS[tone];

  return (
    <div
      role={tone === "danger" ? "alert" : "status"}
      className={cn("flex items-start gap-2.5 rounded-lg border px-3.5 py-3 text-sm", TONE_CLASSES[tone])}
    >
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
      <div>{children}</div>
    </div>
  );
}

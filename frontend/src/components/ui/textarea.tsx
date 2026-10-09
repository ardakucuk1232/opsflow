import type { TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/utils/cn";

type TextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  invalid?: boolean;
};

export function Textarea({ invalid = false, className, ...props }: TextareaProps) {
  return (
    <textarea
      aria-invalid={invalid || undefined}
      className={cn(
        "min-h-24 w-full rounded-lg border bg-surface px-3 py-2 text-sm text-foreground shadow-xs transition-colors",
        "placeholder:text-muted/70",
        "focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-ring",
        invalid ? "border-danger" : "border-border-strong",
        className,
      )}
      {...props}
    />
  );
}

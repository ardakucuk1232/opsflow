import { cn } from "@/lib/utils/cn";

type LogoProps = {
  className?: string;
  tone?: "default" | "inverted";
};

export function Logo({ className, tone = "default" }: LogoProps) {
  const inverted = tone === "inverted";

  return (
    <span className={cn("inline-flex items-center gap-2.5", className)}>
      <span
        className={cn(
          "flex size-8 items-center justify-center rounded-lg",
          inverted ? "bg-white/15" : "bg-primary",
        )}
        aria-hidden="true"
      >
        <svg viewBox="0 0 24 24" className="size-5" fill="none">
          <path
            d="M4 7.5h9.5a3.5 3.5 0 0 1 0 7H9"
            stroke="white"
            strokeWidth="2.2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path
            d="M20 16.5h-9.5a3.5 3.5 0 0 1-3.2-2"
            stroke="white"
            strokeOpacity="0.7"
            strokeWidth="2.2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </span>
      <span
        className={cn(
          "text-lg font-semibold tracking-tight",
          inverted ? "text-white" : "text-foreground",
        )}
      >
        OpsFlow
      </span>
    </span>
  );
}

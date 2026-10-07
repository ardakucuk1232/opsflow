import Link from "next/link";
import { FullPageMessage } from "@/components/ui/full-page-state";

export default function NotFound() {
  return (
    <FullPageMessage
      title="Sayfa bulunamadı"
      description="Aradığınız sayfa taşınmış ya da hiç var olmamış olabilir."
      action={
        <Link
          href="/"
          className="inline-flex h-10 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary-hover"
        >
          Ana sayfaya dön
        </Link>
      }
    />
  );
}

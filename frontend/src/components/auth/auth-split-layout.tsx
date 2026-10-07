import { CheckCircle2 } from "lucide-react";
import type { ReactNode } from "react";
import { Logo } from "@/components/ui/logo";

const HIGHLIGHTS = [
  "Şirketinize özel, diğerlerinden yalıtılmış çalışma alanı",
  "Projeler, görevler ve ekip tek panelde",
  "Rol ve izin tabanlı yetkilendirme",
];

export function AuthSplitLayout({ children }: { children: ReactNode }) {
  return (
    <div className="grid min-h-dvh lg:grid-cols-[minmax(0,5fr)_minmax(0,6fr)]">
      <aside className="relative hidden overflow-hidden bg-[#312e81] lg:flex lg:flex-col lg:justify-between lg:p-12">
        <div
          className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_10%,rgba(129,140,248,0.45),transparent_55%),radial-gradient(circle_at_90%_90%,rgba(67,56,202,0.9),transparent_60%)]"
          aria-hidden="true"
        />
        <Logo tone="inverted" className="relative" />
        <div className="relative max-w-md">
          <h2 className="text-3xl font-semibold leading-tight tracking-tight text-white">
            Projelerinizi ve ekibinizi tek yerden yönetin.
          </h2>
          <ul className="mt-8 space-y-4">
            {HIGHLIGHTS.map((highlight) => (
              <li key={highlight} className="flex items-start gap-3 text-sm text-indigo-100">
                <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-indigo-300" aria-hidden="true" />
                {highlight}
              </li>
            ))}
          </ul>
        </div>
        <p className="relative text-xs text-indigo-200/80">Çok kiracılı proje ve şirket yönetimi</p>
      </aside>

      <main className="flex flex-col justify-center bg-surface px-6 py-12 sm:px-12">
        <div className="mx-auto w-full max-w-sm">
          <Logo className="mb-10 lg:hidden" />
          {children}
        </div>
      </main>
    </div>
  );
}

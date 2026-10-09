"use client";

import { Building2, Mail, ShieldCheck, type LucideIcon } from "lucide-react";
import { AssignedTasks } from "@/components/dashboard/assigned-tasks";
import { getRoleLabel } from "@/lib/auth/roles";
import { useAuth } from "@/lib/auth/auth-provider";

type Detail = {
  label: string;
  value: string;
  icon: LucideIcon;
};

export function DashboardOverview() {
  const { user } = useAuth();

  if (!user) {
    return null;
  }

  const details: Detail[] = [
    { label: "Şirket", value: user.companyName, icon: Building2 },
    { label: "Rol", value: user.roles.map(getRoleLabel).join(", "), icon: ShieldCheck },
    { label: "E-posta", value: user.email, icon: Mail },
  ];

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">
        Merhaba, {user.firstName}
      </h1>
      <p className="mt-1 text-sm text-muted">{user.companyName} çalışma alanındasınız.</p>

      <dl className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {details.map(({ label, value, icon: Icon }) => (
          <div key={label} className="rounded-xl border border-border bg-surface p-5 shadow-xs">
            <dt className="flex items-center gap-2 text-sm text-muted">
              <Icon className="size-4" aria-hidden="true" />
              {label}
            </dt>
            <dd className="mt-2 truncate text-base font-medium text-foreground">{value}</dd>
          </div>
        ))}
      </dl>

      <div className="mt-6">
        <AssignedTasks />
      </div>
    </div>
  );
}

"use client";

import { AssignedTasks } from "@/components/dashboard/assigned-tasks";
import { MySummary } from "@/components/dashboard/my-summary";
import { useAuth } from "@/lib/auth/auth-provider";

export function DashboardOverview() {
  const { user } = useAuth();

  if (!user) {
    return null;
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Merhaba, {user.firstName}</h1>
      <p className="mt-1 text-sm text-muted">{user.companyName} çalışma alanındasınız.</p>

      <div className="mt-8">
        <MySummary />
      </div>

      <div className="mt-6">
        <AssignedTasks />
      </div>
    </div>
  );
}

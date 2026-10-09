"use client";

import { useAuth } from "@/lib/auth/auth-provider";
import { hasPermission } from "@/lib/auth/permissions";

export function usePermission(permission: string): boolean {
  const { user } = useAuth();

  return hasPermission(user, permission);
}

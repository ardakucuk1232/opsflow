import { LayoutDashboard, ShieldCheck, Users, type LucideIcon } from "lucide-react";
import { PERMISSIONS } from "@/lib/auth/permissions";

export type NavigationItem = {
  href: string;
  label: string;
  icon: LucideIcon;
  permission?: string;
};

export const NAVIGATION: NavigationItem[] = [
  { href: "/dashboard", label: "Panel", icon: LayoutDashboard },
  { href: "/team", label: "Ekip", icon: Users, permission: PERMISSIONS.userView },
  { href: "/roles", label: "Roller", icon: ShieldCheck, permission: PERMISSIONS.roleManage },
];

export function getVisibleNavigation(permissions: readonly string[]): NavigationItem[] {
  return NAVIGATION.filter((item) => !item.permission || permissions.includes(item.permission));
}

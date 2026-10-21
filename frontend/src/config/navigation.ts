import {
  BarChart3,
  FolderKanban,
  History,
  LayoutDashboard,
  ListChecks,
  ShieldCheck,
  Users,
  type LucideIcon,
} from "lucide-react";
import { PERMISSIONS } from "@/lib/auth/permissions";

export type NavigationItem = {
  href: string;
  label: string;
  icon: LucideIcon;
  permission?: string;
};

export const NAVIGATION: NavigationItem[] = [
  { href: "/dashboard", label: "Panel", icon: LayoutDashboard },
  { href: "/projects", label: "Projeler", icon: FolderKanban },
  { href: "/tasks", label: "Görevler", icon: ListChecks },
  { href: "/team", label: "Ekip", icon: Users, permission: PERMISSIONS.userView },
  { href: "/roles", label: "Roller", icon: ShieldCheck, permission: PERMISSIONS.roleManage },
  { href: "/reports", label: "Raporlar", icon: BarChart3, permission: PERMISSIONS.reportView },
  { href: "/audit-log", label: "Denetim kaydı", icon: History, permission: PERMISSIONS.auditLogView },
];

export function getVisibleNavigation(permissions: readonly string[]): NavigationItem[] {
  return NAVIGATION.filter((item) => !item.permission || permissions.includes(item.permission));
}

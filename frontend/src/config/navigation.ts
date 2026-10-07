import { LayoutDashboard, type LucideIcon } from "lucide-react";

export type NavigationItem = {
  href: string;
  label: string;
  icon: LucideIcon;
};

export const NAVIGATION: NavigationItem[] = [
  { href: "/dashboard", label: "Panel", icon: LayoutDashboard },
];

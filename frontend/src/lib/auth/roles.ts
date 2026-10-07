const ROLE_LABELS: Record<string, string> = {
  Admin: "Admin",
  Manager: "Yönetici",
  Employee: "Çalışan",
};

export function getRoleLabel(role: string): string {
  return ROLE_LABELS[role] ?? role;
}

export function getInitials(firstName: string, lastName: string): string {
  const first = firstName.trim().charAt(0);
  const last = lastName.trim().charAt(0);

  return `${first}${last}`.toLocaleUpperCase("tr-TR");
}

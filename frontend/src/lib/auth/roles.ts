const ROLE_LABELS: Record<string, string> = {
  Admin: "Admin",
  Manager: "Yönetici",
  Employee: "Çalışan",
};

const SYSTEM_ROLE_DESCRIPTIONS: Record<string, string> = {
  Admin: "Şirketin tüm ayarlarına, kullanıcılarına ve verilerine erişir.",
  Manager: "Projeleri ve görevleri yönetir, ekibi ve raporları görüntüler.",
  Employee: "Görevlerde çalışır, yorum yazar ve dosya yükler.",
};

export function getRoleLabel(role: string): string {
  return ROLE_LABELS[role] ?? role;
}

export function getSystemRoleDescription(role: string): string | null {
  return SYSTEM_ROLE_DESCRIPTIONS[role] ?? null;
}

export function getInitials(firstName: string, lastName: string): string {
  const first = firstName.trim().charAt(0);
  const last = lastName.trim().charAt(0);

  return `${first}${last}`.toLocaleUpperCase("tr-TR");
}

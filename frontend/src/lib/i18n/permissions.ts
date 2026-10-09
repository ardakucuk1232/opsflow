const PERMISSION_LABELS: Record<string, string> = {
  "company.manage": "Şirket ayarlarını yönetme",
  "user.view": "Kullanıcıları görüntüleme",
  "user.invite": "Kullanıcı davet etme",
  "user.manage": "Kullanıcıların rolünü ve durumunu yönetme",
  "role.manage": "Rolleri ve izinleri yönetme",
  "project.view_all": "Tüm projeleri görüntüleme",
  "project.create": "Proje oluşturma",
  "project.manage": "Projeleri ve üyelerini yönetme",
  "task.create": "Görev oluşturma",
  "task.assign": "Görev atama",
  "task.update": "Görevleri güncelleme",
  "task.delete": "Görev silme",
  "comment.create": "Yorum yazma",
  "attachment.upload": "Dosya yükleme",
  "report.view": "Raporları görüntüleme",
  "audit_log.view": "Denetim kayıtlarını görüntüleme",
};

const GROUP_LABELS: Record<string, string> = {
  Company: "Şirket",
  Users: "Kullanıcılar",
  Projects: "Projeler",
  Tasks: "Görevler",
  Insights: "Raporlar",
};

const GROUP_ORDER = ["Company", "Users", "Projects", "Tasks", "Insights"];

export function getPermissionLabel(code: string): string {
  return PERMISSION_LABELS[code] ?? code;
}

export function getPermissionGroupLabel(group: string): string {
  return GROUP_LABELS[group] ?? group;
}

export function comparePermissionGroups(left: string, right: string): number {
  const leftIndex = GROUP_ORDER.indexOf(left);
  const rightIndex = GROUP_ORDER.indexOf(right);

  return (leftIndex === -1 ? GROUP_ORDER.length : leftIndex) - (rightIndex === -1 ? GROUP_ORDER.length : rightIndex);
}

import type { ProjectDetail, ProjectSummary } from "@/types/api";
import { TEST_USER } from "@/test/fixtures";

export const PROJECT_DETAIL: ProjectDetail = {
  id: "project-1",
  key: "WEB",
  name: "Web sitesi",
  description: "Kurumsal web sitesinin yenilenmesi",
  status: "Active",
  startDate: "2026-10-01",
  endDate: "2026-12-31",
  createdBy: { id: TEST_USER.id, firstName: "Arda", lastName: "Küçük", email: TEST_USER.email },
  members: [
    {
      userId: TEST_USER.id,
      firstName: "Arda",
      lastName: "Küçük",
      email: TEST_USER.email,
      isActive: true,
      role: "Lead",
      joinedAt: "2026-10-01T09:00:00Z",
    },
    {
      userId: "member-2",
      firstName: "Ayşe",
      lastName: "Yılmaz",
      email: "ayse@abc.com",
      isActive: true,
      role: "Member",
      joinedAt: "2026-10-02T09:00:00Z",
    },
  ],
  currentUserRole: "Lead",
  canManage: true,
  createdAt: "2026-10-01T09:00:00Z",
  updatedAt: null,
};

export const PROJECT_SUMMARY: ProjectSummary = {
  id: "project-1",
  key: "WEB",
  name: "Web sitesi",
  status: "Active",
  startDate: "2026-10-01",
  endDate: "2026-12-31",
  memberCount: 2,
  lead: { id: TEST_USER.id, firstName: "Arda", lastName: "Küçük", email: TEST_USER.email },
  currentUserRole: "Lead",
  canManage: true,
  createdAt: "2026-10-01T09:00:00Z",
};

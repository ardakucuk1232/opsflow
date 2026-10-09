"use client";

import { Trash2, UserPlus } from "lucide-react";
import { useState } from "react";
import { AddMemberDialog } from "@/components/projects/add-member-dialog";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Select } from "@/components/ui/select";
import { projectsApi } from "@/lib/api/projects";
import { useAuth } from "@/lib/auth/auth-provider";
import { getInitials } from "@/lib/auth/roles";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { getProjectMemberRoleLabel } from "@/lib/i18n/projects";
import type { ProjectDetail, ProjectMember, ProjectMemberRole } from "@/types/api";

type ProjectMembersProps = {
  project: ProjectDetail;
  onChanged: () => void;
  onLeft: () => void;
};

export function ProjectMembers({ project, onChanged, onLeft }: ProjectMembersProps) {
  const { user } = useAuth();
  const [adding, setAdding] = useState(false);
  const [removing, setRemoving] = useState<ProjectMember | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function changeRole(member: ProjectMember, role: ProjectMemberRole) {
    setError(null);

    try {
      await projectsApi.updateMember(project.id, member.userId, role);
      onChanged();
    } catch (exception) {
      setError(getErrorMessage(exception));
    }
  }

  async function remove(member: ProjectMember) {
    await projectsApi.removeMember(project.id, member.userId);
    setRemoving(null);

    if (member.userId === user?.id) {
      onLeft();
    } else {
      onChanged();
    }
  }

  return (
    <section aria-labelledby="members-heading" className="rounded-xl border border-border bg-surface shadow-xs">
      <div className="flex items-center justify-between gap-4 border-b border-border px-5 py-4">
        <h2 id="members-heading" className="text-base font-medium text-foreground">
          Üyeler <span className="text-muted">({project.members.length})</span>
        </h2>
        {project.canManage ? (
          <Button variant="secondary" size="sm" onClick={() => setAdding(true)}>
            <UserPlus className="size-4" aria-hidden="true" />
            Üye ekle
          </Button>
        ) : null}
      </div>

      {error ? (
        <div className="px-5 pt-4">
          <Alert>{error}</Alert>
        </div>
      ) : null}

      <ul className="divide-y divide-border">
        {project.members.map((member) => (
          <li key={member.userId} className="flex flex-col gap-3 px-5 py-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex min-w-0 items-center gap-3">
              <span
                className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary-soft text-xs font-semibold text-primary"
                aria-hidden="true"
              >
                {getInitials(member.firstName, member.lastName)}
              </span>
              <div className="min-w-0">
                <p className="flex flex-wrap items-center gap-2 text-sm font-medium text-foreground">
                  <span className="truncate">
                    {member.firstName} {member.lastName}
                  </span>
                  {member.userId === user?.id ? <Badge tone="primary">Siz</Badge> : null}
                  {member.isActive ? null : <Badge>Pasif</Badge>}
                </p>
                <p className="truncate text-sm text-muted">{member.email}</p>
              </div>
            </div>

            {project.canManage ? (
              <div className="flex items-center gap-2">
                <Select
                  aria-label={`${member.firstName} ${member.lastName} projedeki rolü`}
                  className="w-40"
                  value={member.role}
                  onChange={(event) => void changeRole(member, event.target.value as ProjectMemberRole)}
                >
                  <option value="Member">{getProjectMemberRoleLabel("Member")}</option>
                  <option value="Lead">{getProjectMemberRoleLabel("Lead")}</option>
                </Select>
                <button
                  type="button"
                  aria-label={`${member.firstName} ${member.lastName} projeden çıkar`}
                  onClick={() => setRemoving(member)}
                  className="flex size-9 items-center justify-center rounded-lg text-muted hover:bg-danger-soft hover:text-danger focus-visible:outline-2 focus-visible:outline-ring"
                >
                  <Trash2 className="size-4" aria-hidden="true" />
                </button>
              </div>
            ) : (
              <Badge tone={member.role === "Lead" ? "primary" : "neutral"}>
                {getProjectMemberRoleLabel(member.role)}
              </Badge>
            )}
          </li>
        ))}
      </ul>

      <AddMemberDialog
        open={adding}
        project={project}
        onClose={() => setAdding(false)}
        onAdded={() => {
          setAdding(false);
          onChanged();
        }}
      />

      <ConfirmDialog
        open={removing !== null}
        title="Projeden çıkar"
        description={
          removing?.userId === user?.id
            ? "Kendinizi bu projeden çıkarıyorsunuz. Tüm projeleri görme yetkiniz yoksa projeye erişiminiz kalmaz."
            : `${removing?.firstName ?? ""} ${removing?.lastName ?? ""} bu projeden çıkarılacak.`
        }
        confirmLabel="Çıkar"
        tone="danger"
        onConfirm={() => (removing ? remove(removing) : Promise.resolve())}
        onClose={() => setRemoving(null)}
      />
    </section>
  );
}

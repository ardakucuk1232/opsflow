"use client";

import { useCallback, useState, type FormEvent } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogActions } from "@/components/ui/dialog";
import { FormField } from "@/components/ui/form-field";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { projectsApi } from "@/lib/api/projects";
import { usersApi } from "@/lib/api/users";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { getProjectMemberRoleLabel } from "@/lib/i18n/projects";
import type { ProjectDetail, ProjectMemberRole } from "@/types/api";

type AddMemberDialogProps = {
  open: boolean;
  project: ProjectDetail;
  onClose: () => void;
  onAdded: (project: ProjectDetail) => void;
};

export function AddMemberDialog({ open, project, onClose, onAdded }: AddMemberDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} title="Üye ekle" description={`${project.key} · ${project.name}`}>
      <AddMemberForm project={project} onCancel={onClose} onAdded={onAdded} />
    </Dialog>
  );
}

function AddMemberForm({
  project,
  onCancel,
  onAdded,
}: {
  project: ProjectDetail;
  onCancel: () => void;
  onAdded: (project: ProjectDetail) => void;
}) {
  const loadUsers = useCallback(
    (signal: AbortSignal) => usersApi.list({ page: 1, pageSize: 100, status: "Active" }, signal),
    [],
  );
  const users = useApiData(loadUsers);
  const [userId, setUserId] = useState("");
  const [role, setRole] = useState<ProjectMemberRole>("Member");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const memberIds = new Set(project.members.map((member) => member.userId));
  const candidates = (users.data?.items ?? []).filter((user) => !memberIds.has(user.id));

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (!userId) {
      setError("Eklemek istediğiniz kişiyi seçin.");
      return;
    }

    setPending(true);
    setError(null);

    try {
      onAdded(await projectsApi.addMember(project.id, userId, role));
    } catch (exception) {
      setError(getErrorMessage(exception));
      setPending(false);
    }
  }

  if (users.error) {
    return <Alert>{getErrorMessage(users.error)}</Alert>;
  }

  if (!users.data) {
    return (
      <div className="flex justify-center py-6" role="status">
        <Spinner className="size-5 text-primary" />
        <span className="sr-only">Kullanıcılar yükleniyor</span>
      </div>
    );
  }

  if (candidates.length === 0) {
    return (
      <div className="flex flex-col gap-5">
        <p className="text-sm text-muted">
          Şirketteki bütün aktif kullanıcılar zaten bu projede. Yeni kişiler eklemek için önce Ekip sayfasından davet
          gönderin.
        </p>
        <DialogActions>
          <Button variant="secondary" onClick={onCancel}>
            Kapat
          </Button>
        </DialogActions>
      </div>
    );
  }

  return (
    <form onSubmit={(event) => void handleSubmit(event)} noValidate className="flex flex-col gap-5">
      {error ? <Alert>{error}</Alert> : null}

      <FormField id="member-user" label="Kişi">
        <Select id="member-user" value={userId} onChange={(event) => setUserId(event.target.value)}>
          <option value="">Seçin</option>
          {candidates.map((user) => (
            <option key={user.id} value={user.id}>
              {user.firstName} {user.lastName} ({user.email})
            </option>
          ))}
        </Select>
      </FormField>

      <FormField id="member-role" label="Projedeki rolü">
        <Select id="member-role" value={role} onChange={(event) => setRole(event.target.value as ProjectMemberRole)}>
          <option value="Member">{getProjectMemberRoleLabel("Member")}</option>
          <option value="Lead">{getProjectMemberRoleLabel("Lead")}</option>
        </Select>
      </FormField>

      <DialogActions>
        <Button variant="secondary" onClick={onCancel} disabled={pending}>
          Vazgeç
        </Button>
        <Button type="submit" loading={pending}>
          Ekle
        </Button>
      </DialogActions>
    </form>
  );
}

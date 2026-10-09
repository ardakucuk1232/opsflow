"use client";

import { Search, UserPlus } from "lucide-react";
import { useCallback, useState } from "react";
import { EditRolesDialog } from "@/components/team/edit-roles-dialog";
import { InviteUserDialog } from "@/components/team/invite-user-dialog";
import { UserStatusBadge } from "@/components/team/user-status-badge";
import { ActionMenu, type ActionMenuItem } from "@/components/ui/action-menu";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { Pagination } from "@/components/ui/pagination";
import { Select } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { rolesApi } from "@/lib/api/roles";
import { usersApi, type UserStatusFilter } from "@/lib/api/users";
import { useAuth } from "@/lib/auth/auth-provider";
import { hasPermission, PERMISSIONS } from "@/lib/auth/permissions";
import { getInitials, getRoleLabel } from "@/lib/auth/roles";
import { useApiData } from "@/lib/hooks/use-api-data";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { formatDateTime } from "@/lib/utils/format";
import type { UserSummary } from "@/types/api";

const PAGE_SIZE = 20;

const STATUS_OPTIONS: { value: UserStatusFilter | ""; label: string }[] = [
  { value: "", label: "Tüm durumlar" },
  { value: "Active", label: "Aktif" },
  { value: "Invited", label: "Davet bekliyor" },
  { value: "Inactive", label: "Pasif" },
];

type Notice = { tone: "success" | "danger"; text: string };

export function TeamPage() {
  const { user: currentUser } = useAuth();
  const canInvite = hasPermission(currentUser, PERMISSIONS.userInvite);
  const canManage = hasPermission(currentUser, PERMISSIONS.userManage);

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<UserStatusFilter | "">("");
  const [roleId, setRoleId] = useState("");
  const [page, setPage] = useState(1);
  const [notice, setNotice] = useState<Notice | null>(null);
  const [inviteOpen, setInviteOpen] = useState(false);
  const [editingRoles, setEditingRoles] = useState<UserSummary | null>(null);
  const [changingStatus, setChangingStatus] = useState<UserSummary | null>(null);

  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  const loadUsers = useCallback(
    (signal: AbortSignal) =>
      usersApi.list(
        {
          page,
          pageSize: PAGE_SIZE,
          search: debouncedSearch || undefined,
          status: status || undefined,
          roleId: roleId || undefined,
        },
        signal,
      ),
    [page, debouncedSearch, status, roleId],
  );

  const users = useApiData(loadUsers);
  const roles = useApiData(rolesApi.list);
  const roleList = roles.data ?? [];
  const filtered = debouncedSearch !== "" || status !== "" || roleId !== "";

  async function resendInvitation(user: UserSummary) {
    try {
      await usersApi.resendInvitation(user.id);
      setNotice({ tone: "success", text: `${user.email} adresine davet tekrar gönderildi.` });
    } catch (error) {
      setNotice({ tone: "danger", text: getErrorMessage(error) });
    }
  }

  async function toggleStatus(user: UserSummary) {
    if (user.isActive) {
      await usersApi.deactivate(user.id);
      setNotice({ tone: "success", text: `${user.firstName} ${user.lastName} pasif duruma alındı.` });
    } else {
      await usersApi.activate(user.id);
      setNotice({ tone: "success", text: `${user.firstName} ${user.lastName} yeniden aktif.` });
    }

    setChangingStatus(null);
    users.reload();
  }

  function actionsFor(user: UserSummary): ActionMenuItem[] {
    if (user.id === currentUser?.id) {
      return [];
    }

    const items: ActionMenuItem[] = [];

    if (canManage) {
      items.push({ label: "Rolleri düzenle", onSelect: () => setEditingRoles(user) });
    }

    if (canInvite && user.invitationPending && user.isActive) {
      items.push({ label: "Daveti tekrar gönder", onSelect: () => void resendInvitation(user) });
    }

    if (canManage) {
      items.push(
        user.isActive
          ? { label: "Pasifleştir", tone: "danger", onSelect: () => setChangingStatus(user) }
          : { label: "Aktifleştir", onSelect: () => setChangingStatus(user) },
      );
    }

    return items;
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Ekip"
        description="Şirketinizdeki kullanıcıları görüntüleyin, davet edin ve rollerini yönetin."
        actions={
          canInvite ? (
            <Button onClick={() => setInviteOpen(true)} disabled={roles.data === undefined}>
              <UserPlus className="size-4" aria-hidden="true" />
              Kullanıcı davet et
            </Button>
          ) : null
        }
      />

      {notice ? <Alert tone={notice.tone}>{notice.text}</Alert> : null}

      <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_12rem_12rem]">
        <div className="relative">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted"
            aria-hidden="true"
          />
          <Input
            type="search"
            aria-label="Kullanıcı ara"
            placeholder="Ad, soyad veya e-posta ara"
            className="pl-9"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </div>
        <Select
          aria-label="Duruma göre filtrele"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value as UserStatusFilter | "");
            setPage(1);
          }}
        >
          {STATUS_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
        <Select
          aria-label="Role göre filtrele"
          value={roleId}
          onChange={(event) => {
            setRoleId(event.target.value);
            setPage(1);
          }}
        >
          <option value="">Tüm roller</option>
          {roleList.map((role) => (
            <option key={role.id} value={role.id}>
              {getRoleLabel(role.name)}
            </option>
          ))}
        </Select>
      </div>

      {users.error ? (
        <Alert>
          <span>{getErrorMessage(users.error)}</span>{" "}
          <button type="button" onClick={users.reload} className="font-medium underline">
            Tekrar dene
          </button>
        </Alert>
      ) : null}

      <div className="overflow-hidden rounded-xl border border-border bg-surface shadow-xs">
        <div className="relative overflow-x-auto">
          <table className="w-full min-w-[760px] text-sm" aria-busy={users.loading}>
            <thead className="border-b border-border bg-surface-muted text-left text-xs font-medium text-muted">
              <tr>
                <th scope="col" className="px-4 py-3 font-medium">Kullanıcı</th>
                <th scope="col" className="px-4 py-3 font-medium">Roller</th>
                <th scope="col" className="px-4 py-3 font-medium">Durum</th>
                <th scope="col" className="px-4 py-3 font-medium">Son giriş</th>
                <th scope="col" className="w-14 px-4 py-3">
                  <span className="sr-only">İşlemler</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {users.data === undefined && users.loading ? (
                <tr>
                  <td colSpan={5} className="px-4 py-12">
                    <div className="flex justify-center" role="status">
                      <Spinner className="size-5 text-primary" />
                      <span className="sr-only">Kullanıcılar yükleniyor</span>
                    </div>
                  </td>
                </tr>
              ) : users.data && users.data.items.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-12 text-center text-sm text-muted">
                    {filtered ? "Filtrelerle eşleşen kullanıcı bulunamadı." : "Henüz kullanıcı yok."}
                  </td>
                </tr>
              ) : (
                users.data?.items.map((user) => (
                  <tr key={user.id} className={user.isActive ? undefined : "bg-surface-muted/50"}>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <span
                          className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary-soft text-xs font-semibold text-primary"
                          aria-hidden="true"
                        >
                          {getInitials(user.firstName, user.lastName)}
                        </span>
                        <div className="min-w-0">
                          <p className="flex items-center gap-2 truncate font-medium text-foreground">
                            {user.firstName} {user.lastName}
                            {user.id === currentUser?.id ? <Badge tone="primary">Siz</Badge> : null}
                          </p>
                          <p className="truncate text-muted">{user.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1.5">
                        {user.roles.map((role) => (
                          <Badge key={role.id}>{getRoleLabel(role.name)}</Badge>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <UserStatusBadge user={user} />
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-muted">
                      {user.lastLoginAt ? formatDateTime(user.lastLoginAt) : "—"}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <ActionMenu
                        label={`${user.firstName} ${user.lastName} için işlemler`}
                        items={actionsFor(user)}
                      />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {users.data ? (
        <Pagination
          page={users.data.page}
          pageSize={users.data.pageSize}
          totalCount={users.data.totalCount}
          onPageChange={setPage}
        />
      ) : null}

      <InviteUserDialog
        open={inviteOpen}
        roles={roleList}
        onClose={() => setInviteOpen(false)}
        onInvited={(user) => {
          setInviteOpen(false);
          setNotice({ tone: "success", text: `${user.email} adresine davet gönderildi.` });
          users.reload();
        }}
      />

      <EditRolesDialog
        user={editingRoles}
        roles={roleList}
        onClose={() => setEditingRoles(null)}
        onSaved={(user) => {
          setEditingRoles(null);
          setNotice({ tone: "success", text: `${user.firstName} ${user.lastName} kullanıcısının rolleri güncellendi.` });
          users.reload();
        }}
      />

      <ConfirmDialog
        open={changingStatus !== null}
        title={changingStatus?.isActive ? "Kullanıcıyı pasifleştir" : "Kullanıcıyı aktifleştir"}
        description={
          changingStatus?.isActive
            ? `${changingStatus.firstName} ${changingStatus.lastName} artık giriş yapamayacak ve açık oturumları hemen kapatılacak.`
            : `${changingStatus?.firstName ?? ""} ${changingStatus?.lastName ?? ""} yeniden giriş yapabilecek.`
        }
        confirmLabel={changingStatus?.isActive ? "Pasifleştir" : "Aktifleştir"}
        tone={changingStatus?.isActive ? "danger" : "primary"}
        onConfirm={() => (changingStatus ? toggleStatus(changingStatus) : Promise.resolve())}
        onClose={() => setChangingStatus(null)}
      />
    </div>
  );
}

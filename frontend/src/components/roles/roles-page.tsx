"use client";

import { Plus, ShieldCheck, Users } from "lucide-react";
import { useState } from "react";
import { RoleEditorDialog, type RoleEditorTarget } from "@/components/roles/role-editor-dialog";
import { ActionMenu } from "@/components/ui/action-menu";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { PageHeader } from "@/components/ui/page-header";
import { Spinner } from "@/components/ui/spinner";
import { rolesApi } from "@/lib/api/roles";
import { getRoleLabel, getSystemRoleDescription } from "@/lib/auth/roles";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import type { Role } from "@/types/api";

export function RolesPage() {
  const roles = useApiData(rolesApi.list);
  const catalog = useApiData(rolesApi.permissions);
  const [editor, setEditor] = useState<RoleEditorTarget | null>(null);
  const [deleting, setDeleting] = useState<Role | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const error = roles.error ?? catalog.error;
  const ready = roles.data !== undefined && catalog.data !== undefined;

  async function deleteRole(role: Role) {
    await rolesApi.remove(role.id);
    setDeleting(null);
    setNotice(`${role.name} rolü silindi.`);
    roles.reload();
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Roller"
        description="Rollerin hangi işlemleri yapabileceğini belirleyin. Sistem rolleri değiştirilemez."
        actions={
          <Button onClick={() => setEditor({ mode: "create" })} disabled={!ready}>
            <Plus className="size-4" aria-hidden="true" />
            Yeni rol
          </Button>
        }
      />

      {notice ? <Alert tone="success">{notice}</Alert> : null}

      {error ? (
        <Alert>
          <span>{getErrorMessage(error)}</span>{" "}
          <button
            type="button"
            onClick={() => {
              roles.reload();
              catalog.reload();
            }}
            className="font-medium underline"
          >
            Tekrar dene
          </button>
        </Alert>
      ) : null}

      {!ready && !error ? (
        <div className="flex justify-center py-16" role="status">
          <Spinner className="size-5 text-primary" />
          <span className="sr-only">Roller yükleniyor</span>
        </div>
      ) : null}

      {roles.data ? (
        <ul className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {roles.data.map((role) => {
            const description = role.isSystemRole ? getSystemRoleDescription(role.name) : role.description;

            return (
              <li key={role.id} className="flex flex-col rounded-xl border border-border bg-surface p-5 shadow-xs">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h2 className="flex flex-wrap items-center gap-2 text-base font-medium text-foreground">
                      <span className="truncate">{getRoleLabel(role.name)}</span>
                      {role.isSystemRole ? <Badge tone="primary">Sistem rolü</Badge> : null}
                    </h2>
                    <p className="mt-1 text-sm text-muted">{description || "Açıklama eklenmemiş."}</p>
                  </div>
                  {role.isSystemRole ? null : (
                    <ActionMenu
                      label={`${role.name} rolü için işlemler`}
                      items={[
                        { label: "Düzenle", onSelect: () => setEditor({ mode: "edit", role }) },
                        { label: "Sil", tone: "danger", onSelect: () => setDeleting(role) },
                      ]}
                    />
                  )}
                </div>
                <div className="mt-auto flex items-center justify-between gap-3 pt-5 text-sm text-muted">
                  <span className="flex items-center gap-4">
                    <span className="flex items-center gap-1.5">
                      <Users className="size-4" aria-hidden="true" />
                      {role.userCount} kullanıcı
                    </span>
                    <span className="flex items-center gap-1.5">
                      <ShieldCheck className="size-4" aria-hidden="true" />
                      {role.permissions.length} izin
                    </span>
                  </span>
                  {role.isSystemRole ? (
                    <Button variant="ghost" size="sm" disabled={!ready} onClick={() => setEditor({ mode: "edit", role })}>
                      İzinleri gör
                    </Button>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>
      ) : null}

      <RoleEditorDialog
        target={editor}
        catalog={catalog.data ?? []}
        onClose={() => setEditor(null)}
        onSaved={(role) => {
          setNotice(editor?.mode === "create" ? `${role.name} rolü oluşturuldu.` : `${role.name} rolü güncellendi.`);
          setEditor(null);
          roles.reload();
        }}
      />

      <ConfirmDialog
        open={deleting !== null}
        title="Rolü sil"
        description={`${deleting?.name ?? ""} rolü kalıcı olarak silinecek.`}
        confirmLabel="Sil"
        tone="danger"
        onConfirm={() => (deleting ? deleteRole(deleting) : Promise.resolve())}
        onClose={() => setDeleting(null)}
      />
    </div>
  );
}

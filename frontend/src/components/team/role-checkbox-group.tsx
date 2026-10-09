"use client";

import { Checkbox } from "@/components/ui/checkbox";
import { useAuth } from "@/lib/auth/auth-provider";
import { holdsAll } from "@/lib/auth/permissions";
import { getRoleLabel, getSystemRoleDescription } from "@/lib/auth/roles";
import type { Role } from "@/types/api";

type RoleCheckboxGroupProps = {
  roles: Role[];
  value: string[];
  onChange: (value: string[]) => void;
  error?: string;
};

export function RoleCheckboxGroup({ roles, value, onChange, error }: RoleCheckboxGroupProps) {
  const { user } = useAuth();

  function toggle(roleId: string, checked: boolean) {
    onChange(checked ? [...value, roleId] : value.filter((id) => id !== roleId));
  }

  return (
    <fieldset aria-describedby={error ? "roles-error" : undefined}>
      <legend className="text-sm font-medium text-foreground">Roller</legend>
      <div className="mt-2 flex flex-col gap-1">
        {roles.map((role) => {
          const grantable = holdsAll(user, role.permissions);
          const description = grantable
            ? (role.isSystemRole ? getSystemRoleDescription(role.name) : role.description)
            : "Bu roldeki izinlerin hepsine sahip olmadığınız için atayamazsınız.";

          return (
            <Checkbox
              key={role.id}
              label={getRoleLabel(role.name)}
              description={description ?? undefined}
              checked={value.includes(role.id)}
              disabled={!grantable && !value.includes(role.id)}
              onChange={(event) => toggle(role.id, event.target.checked)}
            />
          );
        })}
      </div>
      {error ? (
        <p id="roles-error" role="alert" className="mt-1.5 text-sm text-danger">
          {error}
        </p>
      ) : null}
    </fieldset>
  );
}

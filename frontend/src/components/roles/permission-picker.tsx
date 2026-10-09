"use client";

import { Checkbox } from "@/components/ui/checkbox";
import {
  comparePermissionGroups,
  getPermissionGroupLabel,
  getPermissionLabel,
} from "@/lib/i18n/permissions";
import type { Permission } from "@/types/api";

type PermissionPickerProps = {
  catalog: Permission[];
  value: string[];
  onChange: (value: string[]) => void;
  isAllowed: (code: string) => boolean;
  readOnly?: boolean;
};

export function PermissionPicker({ catalog, value, onChange, isAllowed, readOnly = false }: PermissionPickerProps) {
  const groups = [...new Set(catalog.map((permission) => permission.group))].sort(comparePermissionGroups);

  function toggle(code: string, checked: boolean) {
    onChange(checked ? [...value, code] : value.filter((item) => item !== code));
  }

  return (
    <div className="flex flex-col gap-5">
      {groups.map((group) => (
        <fieldset key={group}>
          <legend className="text-xs font-semibold uppercase tracking-wide text-muted">
            {getPermissionGroupLabel(group)}
          </legend>
          <div className="mt-2 grid gap-1 sm:grid-cols-2">
            {catalog
              .filter((permission) => permission.group === group)
              .sort((left, right) =>
                getPermissionLabel(left.code).localeCompare(getPermissionLabel(right.code), "tr"),
              )
              .map((permission) => {
                const checked = value.includes(permission.code);
                const allowed = isAllowed(permission.code);

                return (
                  <Checkbox
                    key={permission.code}
                    label={getPermissionLabel(permission.code)}
                    description={!readOnly && !allowed ? "Bu izne sahip değilsiniz" : undefined}
                    checked={checked}
                    disabled={readOnly || !allowed}
                    onChange={(event) => toggle(permission.code, event.target.checked)}
                  />
                );
              })}
          </div>
        </fieldset>
      ))}
    </div>
  );
}

const dateFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export function formatDate(value: string): string {
  return dateFormatter.format(new Date(value));
}

export function formatDateTime(value: string): string {
  return dateTimeFormatter.format(new Date(value));
}

export function formatDateOnly(value: string): string {
  const [year, month, day] = value.split("-").map(Number);

  return dateFormatter.format(new Date(year, month - 1, day));
}

const relativeFormatter = new Intl.RelativeTimeFormat("tr-TR", { numeric: "auto" });

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["day", 86_400],
  ["hour", 3_600],
  ["minute", 60],
];

export function formatRelativeTime(value: string, now: number = Date.now()): string {
  const seconds = Math.round((new Date(value).getTime() - now) / 1000);

  if (Math.abs(seconds) < 60) {
    return "az önce";
  }

  if (Math.abs(seconds) >= 7 * 86_400) {
    return formatDate(value);
  }

  for (const [unit, size] of RELATIVE_UNITS) {
    if (Math.abs(seconds) >= size) {
      return relativeFormatter.format(Math.trunc(seconds / size), unit);
    }
  }

  return formatDateTime(value);
}

import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

const currency = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  maximumFractionDigits: 0,
});

export function formatCurrency(value: number): string {
  return currency.format(value);
}

const exactCurrency = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/** Full precision for the deep-dive drawer, where rounding to millions would hide the real number. */
export function formatExactCurrency(value: number): string {
  return exactCurrency.format(value);
}

/** Institutional shorthand: $185.4M, $1.2B. */
export function formatCompactCurrency(value: number): string {
  const abs = Math.abs(value);

  if (abs >= 1_000_000_000) {
    return `$${(value / 1_000_000_000).toFixed(1)}B`;
  }

  if (abs >= 1_000_000) {
    return `$${(value / 1_000_000).toFixed(1)}M`;
  }

  if (abs >= 1_000) {
    return `$${(value / 1_000).toFixed(0)}K`;
  }

  return formatCurrency(value);
}

export function formatPercent(value: number | null, fractionDigits = 1): string {
  if (value === null || Number.isNaN(value)) {
    return "n/a";
  }

  return `${(value * 100).toFixed(fractionDigits)}%`;
}

export function formatRatio(value: number | null, fractionDigits = 2): string {
  if (value === null || Number.isNaN(value)) {
    return "n/a";
  }

  return `${value.toFixed(fractionDigits)}x`;
}

export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  });
}

export function formatYears(value: number | null): string {
  if (value === null || Number.isNaN(value)) {
    return "n/a";
  }

  return `${value.toFixed(1)} yrs`;
}

/** The API returns the domain enum name, so "MixedUse" becomes "Mixed Use". */
export function formatPropertyType(propertyType: string): string {
  return propertyType.replace(/([a-z])([A-Z])/g, "$1 $2");
}

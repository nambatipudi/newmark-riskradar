import { describe, expect, it } from "vitest";

import {
  cn,
  formatCompactCurrency,
  formatCurrency,
  formatDate,
  formatExactCurrency,
  formatPercent,
  formatPropertyType,
  formatRatio,
  formatYears,
} from "@/lib/utils";

describe("formatCompactCurrency", () => {
  it.each([
    [2594157494.7, "$2.6B"],
    [185_400_000, "$185.4M"],
    [26_880_000, "$26.9M"],
    [45_000, "$45K"],
    [750, "$750"],
    [0, "$0"],
  ])("formats %s as %s", (value, expected) => {
    expect(formatCompactCurrency(value)).toBe(expected);
  });

  it("keeps the sign on a negative balance", () => {
    expect(formatCompactCurrency(-1_500_000)).toBe("$-1.5M");
  });
});

describe("formatExactCurrency", () => {
  it("keeps full precision for the deep dive drawer", () => {
    expect(formatExactCurrency(26_880_000)).toBe("$26,880,000.00");
    expect(formatExactCurrency(2_167_637.31)).toBe("$2,167,637.31");
  });
});

describe("formatCurrency", () => {
  it("rounds to whole dollars", () => {
    expect(formatCurrency(1234.56)).toBe("$1,235");
  });
});

describe("formatRatio", () => {
  it.each([
    [1.25, "1.25x"],
    [0.8489, "0.85x"],
    [null, "n/a"],
  ])("formats %s as %s", (value, expected) => {
    expect(formatRatio(value)).toBe(expected);
  });
});

describe("formatPercent", () => {
  it.each([
    [0.4244, 1, "42.4%"],
    [0.0574, 2, "5.74%"],
    [null, 1, "n/a"],
  ])("formats %s as %s", (value, digits, expected) => {
    expect(formatPercent(value, digits)).toBe(expected);
  });
});

describe("formatYears", () => {
  it("labels a term in years", () => {
    expect(formatYears(3.0317)).toBe("3.0 yrs");
    expect(formatYears(null)).toBe("n/a");
  });
});

describe("formatDate", () => {
  it("renders the API date without shifting across timezones", () => {
    // A naive new Date("2026-01-01") renders as Dec 31 west of UTC.
    expect(formatDate("2026-01-01")).toBe("Jan 1, 2026");
    expect(formatDate("2026-10-12")).toBe("Oct 12, 2026");
  });
});

describe("formatPropertyType", () => {
  it("splits the domain enum name", () => {
    expect(formatPropertyType("MixedUse")).toBe("Mixed Use");
    expect(formatPropertyType("Office")).toBe("Office");
  });
});

describe("cn", () => {
  it("lets a later tailwind class win", () => {
    expect(cn("text-foreground", "text-destructive")).toBe("text-destructive");
  });

  it("drops falsy values", () => {
    expect(cn("px-2", false && "hidden", undefined)).toBe("px-2");
  });
});

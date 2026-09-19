import { describe, expect, it } from "vitest";

import { parseTriageQuery, toSearchString, type TriageQuery } from "@/lib/api/params";

describe("parseTriageQuery", () => {
  it("falls back to neutral defaults for an empty URL", () => {
    expect(parseTriageQuery({})).toEqual({
      risk: "all",
      assetClass: "all",
      stressRate: null,
      search: "",
      page: 1,
    });
  });

  it("reads a fully specified scenario", () => {
    expect(
      parseTriageQuery({ risk: "Critical", assetClass: "Office", stressRate: "8.5", q: "Miami", page: "3" }),
    ).toEqual({
      risk: "Critical",
      assetClass: "Office",
      stressRate: 8.5,
      search: "Miami",
      page: 3,
    });
  });

  it.each(["Exploding", "critical", ""])("ignores an unknown risk value: %s", (risk) => {
    expect(parseTriageQuery({ risk }).risk).toBe("all");
  });

  it.each(["Spaceport", "office"])("ignores an unknown asset class: %s", (assetClass) => {
    expect(parseTriageQuery({ assetClass }).assetClass).toBe("all");
  });

  it.each([
    ["below the floor", "2.9"],
    ["above the ceiling", "10.1"],
    ["not a number", "abc"],
    ["empty", ""],
  ])("drops a stress rate %s", (_label, stressRate) => {
    expect(parseTriageQuery({ stressRate }).stressRate).toBeNull();
  });

  it.each([
    ["3", 3],
    ["10", 10],
    ["7.25", 7.3],
  ])("keeps an in-range stress rate %s", (input, expected) => {
    expect(parseTriageQuery({ stressRate: input }).stressRate).toBe(expected);
  });

  it.each(["0", "-2", "1.5", "abc"])("falls back to page 1 for %s", (page) => {
    expect(parseTriageQuery({ page }).page).toBe(1);
  });

  it("trims the search term", () => {
    expect(parseTriageQuery({ q: "  Harborview  " }).search).toBe("Harborview");
  });

  it("uses the first value when a param is repeated", () => {
    expect(parseTriageQuery({ assetClass: ["Retail", "Office"] }).assetClass).toBe("Retail");
  });
});

describe("toSearchString", () => {
  it("omits neutral defaults", () => {
    expect(
      toSearchString({ risk: "all", assetClass: "all", stressRate: null, search: "", page: 1 }),
    ).toBe("");
  });

  it("serialises an applied scenario", () => {
    expect(
      toSearchString({ risk: "Critical", assetClass: "Office", stressRate: 8.5, search: "Miami", page: 2 }),
    ).toBe("risk=Critical&assetClass=Office&stressRate=8.5&q=Miami&page=2");
  });

  it("formats the stress rate to one decimal", () => {
    expect(toSearchString({ stressRate: 9 })).toBe("stressRate=9.0");
  });

  it("round trips through parseTriageQuery", () => {
    const query: TriageQuery = {
      risk: "Warning",
      assetClass: "Multifamily",
      stressRate: 7.4,
      search: "Seattle",
      page: 4,
    };

    const params = Object.fromEntries(new URLSearchParams(toSearchString(query)));

    expect(parseTriageQuery(params)).toEqual(query);
  });
});

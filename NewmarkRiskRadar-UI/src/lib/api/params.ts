import { riskCategorySchema } from "@/lib/api/schemas";

/** Asset classes mirror the PropertyType enum on the .NET side. */
export const ASSET_CLASSES = [
  "Office",
  "Retail",
  "Industrial",
  "Multifamily",
  "Hospitality",
  "MixedUse",
] as const;

export type AssetClass = (typeof ASSET_CLASSES)[number];

export const STRESS_RATE_MIN = 3.0;
export const STRESS_RATE_MAX = 10.0;
export const STRESS_RATE_STEP = 0.1;
export const TRIAGE_PAGE_SIZE = 10;

/** Raw shape Next hands a page: a value can arrive repeated, hence the array case. */
export type RawSearchParams = Record<string, string | string[] | undefined>;

export interface TriageQuery {
  risk: "all" | "Performing" | "Warning" | "Critical";
  assetClass: AssetClass | "all";
  /** Pro-forma rate as a percentage, e.g. 8.5. Null means in-place note rates. */
  stressRate: number | null;
  search: string;
  page: number;
}

function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

/**
 * The URL is untrusted input as much as any API payload, so unparseable values fall back to the
 * neutral default instead of propagating into a request.
 */
export function parseTriageQuery(params: RawSearchParams): TriageQuery {
  const risk = riskCategorySchema.safeParse(first(params.risk));
  const rawAssetClass = first(params.assetClass);
  const assetClass = ASSET_CLASSES.find((option) => option === rawAssetClass) ?? "all";

  const rawRate = Number(first(params.stressRate));
  const stressRate =
    Number.isFinite(rawRate) && rawRate >= STRESS_RATE_MIN && rawRate <= STRESS_RATE_MAX
      ? Math.round(rawRate * 10) / 10
      : null;

  const rawPage = Number(first(params.page));
  const page = Number.isInteger(rawPage) && rawPage > 0 ? rawPage : 1;

  return {
    risk: risk.success ? risk.data : "all",
    assetClass,
    stressRate,
    search: first(params.q)?.trim() ?? "",
    page,
  };
}

/** Serialises a query back to a URL search string, omitting neutral defaults. */
export function toSearchString(query: Partial<TriageQuery>): string {
  const params = new URLSearchParams();

  if (query.risk && query.risk !== "all") {
    params.set("risk", query.risk);
  }

  if (query.assetClass && query.assetClass !== "all") {
    params.set("assetClass", query.assetClass);
  }

  if (query.stressRate != null) {
    params.set("stressRate", query.stressRate.toFixed(1));
  }

  if (query.search) {
    params.set("q", query.search);
  }

  if (query.page && query.page > 1) {
    params.set("page", String(query.page));
  }

  return params.toString();
}

/**
 * Public DTO shapes for the UI. The Zod schemas in src/lib/api/schemas.ts are the single
 * source of truth, so these aliases cannot drift from what is validated at runtime.
 */

import type {
  LoanSummary as LoanSummaryModel,
  LoanTriagePage,
  MaturityWallBucket as MaturityWallBucketModel,
  PortfolioSummary as PortfolioSummaryModel,
  RiskCategory as RiskCategoryModel,
} from "@/lib/api/schemas";

export type RiskCategory = RiskCategoryModel;
export type PortfolioSummary = PortfolioSummaryModel;
export type MaturityWallBucket = MaturityWallBucketModel;
export type LoanSummary = LoanSummaryModel;
export type PagedResult<TItem> = Omit<LoanTriagePage, "items"> & { items: TItem[] };
export type RiskFilter = RiskCategory | "all";

export interface LoanTriageParams {
  page?: number;
  pageSize?: number;
  risk?: RiskFilter;
  search?: string;
}

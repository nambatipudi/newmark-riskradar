import { z } from "zod";

/** Runtime contracts for every payload the .NET API can return. Nothing reaches React unparsed. */

export const riskCategorySchema = z.enum(["Performing", "Warning", "Critical"]);

export const portfolioSummarySchema = z.object({
  asOfDate: z.string(),
  appliedStressRate: z.number().nullable(),
  loanCount: z.number().int(),
  totalServicingVolume: z.number(),
  weightedAverageDscr: z.number().nullable(),
  stressedWeightedAverageDscr: z.number().nullable(),
  weightedAverageLoanTermYears: z.number().nullable(),
  criticalDefaultExposure: z.number(),
  criticalDefaultExposureShare: z.number(),
  warningExposure: z.number(),
  performingExposure: z.number(),
  criticalLoanCount: z.number().int(),
  warningLoanCount: z.number().int(),
  performingLoanCount: z.number().int(),
  maturingWithinTwelveMonths: z.number(),
});

export const maturityWallBucketSchema = z.object({
  year: z.number().int(),
  loanCount: z.number().int(),
  totalBalance: z.number(),
  criticalBalance: z.number(),
  warningBalance: z.number(),
  performingBalance: z.number(),
  shareOfPortfolio: z.number(),
});

export const maturityWallSchema = z.array(maturityWallBucketSchema);

export const loanSummarySchema = z.object({
  id: z.string(),
  loanNumber: z.string(),
  borrowerName: z.string(),
  propertyName: z.string(),
  market: z.string(),
  propertyType: z.string(),
  originationDate: z.string(),
  maturityDate: z.string(),
  maturityYear: z.number().int(),
  monthsToMaturity: z.number().int(),
  outstandingBalance: z.number(),
  originalBalance: z.number(),
  netOperatingIncome: z.number(),
  annualDebtService: z.number(),
  appraisedValue: z.number(),
  interestRate: z.number(),
  amortizationYears: z.number().int(),
  dscr: z.number().nullable(),
  stressedAnnualDebtService: z.number().nullable(),
  stressedDscr: z.number().nullable(),
  debtYield: z.number().nullable(),
  loanToValue: z.number().nullable(),
  riskCategory: riskCategorySchema,
  riskReasons: z.array(z.string()),
});

export function pagedResultSchema<TItem extends z.ZodTypeAny>(itemSchema: TItem) {
  return z.object({
    items: z.array(itemSchema),
    page: z.number().int(),
    pageSize: z.number().int(),
    totalCount: z.number().int(),
    totalPages: z.number().int(),
    hasNextPage: z.boolean(),
  });
}

export const loanTriagePageSchema = pagedResultSchema(loanSummarySchema);

/** RFC 7807 document emitted by the API's global exception middleware. */
export const problemDocumentSchema = z.object({
  title: z.string().optional(),
  detail: z.string().nullish(),
  status: z.number().optional(),
  traceId: z.string().optional(),
});

export const tapeSyncResultSchema = z.object({
  message: z.string(),
  recordsInserted: z.number().int(),
  syncedAtUtc: z.string(),
});

export type RiskCategory = z.infer<typeof riskCategorySchema>;
export type PortfolioSummary = z.infer<typeof portfolioSummarySchema>;
export type MaturityWallBucket = z.infer<typeof maturityWallBucketSchema>;
export type LoanSummary = z.infer<typeof loanSummarySchema>;
export type LoanTriagePage = z.infer<typeof loanTriagePageSchema>;
export type ProblemDocument = z.infer<typeof problemDocumentSchema>;
export type TapeSyncResult = z.infer<typeof tapeSyncResultSchema>;

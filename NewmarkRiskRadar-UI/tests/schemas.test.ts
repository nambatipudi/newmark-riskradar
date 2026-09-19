import { describe, expect, it } from "vitest";

import {
  loanSummarySchema,
  loanTriagePageSchema,
  maturityWallSchema,
  portfolioSummarySchema,
  problemDocumentSchema,
  tapeSyncResultSchema,
} from "@/lib/api/schemas";

const validSummary = {
  asOfDate: "2026-09-19",
  appliedStressRate: null,
  loanCount: 50,
  totalServicingVolume: 2594157494.7,
  weightedAverageDscr: 1.2704,
  stressedWeightedAverageDscr: null,
  weightedAverageLoanTermYears: 3.0317,
  criticalDefaultExposure: 1100976659.5,
  criticalDefaultExposureShare: 0.4244,
  warningExposure: 939614112.7,
  performingExposure: 553566722.5,
  criticalLoanCount: 21,
  warningLoanCount: 18,
  performingLoanCount: 11,
  maturingWithinTwelveMonths: 818102771.4,
};

const validLoan = {
  id: "01352836-0000-0000-0000-000001000000",
  loanNumber: "NMK-2020-1000",
  borrowerName: "Kestrel Industrial Partners",
  propertyName: "Union Tower",
  market: "Dallas, TX",
  propertyType: "Office",
  originationDate: "2019-10-12",
  maturityDate: "2026-10-12",
  maturityYear: 2026,
  monthsToMaturity: 0,
  outstandingBalance: 26880000,
  originalBalance: 26880000,
  netOperatingIncome: 2167637.31,
  annualDebtService: 1542912.0,
  appraisedValue: 44800000,
  interestRate: 0.0574,
  amortizationYears: 0,
  dscr: 1.4048,
  stressedAnnualDebtService: 2553600,
  stressedDscr: 0.8489,
  debtYield: 0.0806,
  loanToValue: 0.6,
  riskCategory: "Critical",
  riskReasons: ["Stressed DSCR of 0.85 is below the 1.00 break-even threshold."],
};

describe("portfolioSummarySchema", () => {
  it("accepts a well formed payload", () => {
    expect(portfolioSummarySchema.parse(validSummary)).toEqual(validSummary);
  });

  it("rejects a payload missing a required field", () => {
    const { totalServicingVolume, ...incomplete } = validSummary;
    void totalServicingVolume;

    const result = portfolioSummarySchema.safeParse(incomplete);

    expect(result.success).toBe(false);
    expect(result.error?.issues[0]?.path).toEqual(["totalServicingVolume"]);
  });

  it("rejects a numeric field arriving as a string", () => {
    const result = portfolioSummarySchema.safeParse({ ...validSummary, loanCount: "50" });

    expect(result.success).toBe(false);
  });

  it("keeps nullable coverage fields nullable", () => {
    const result = portfolioSummarySchema.safeParse({
      ...validSummary,
      weightedAverageDscr: null,
    });

    expect(result.success).toBe(true);
  });
});

describe("loanSummarySchema", () => {
  it("accepts a well formed loan", () => {
    expect(loanSummarySchema.parse(validLoan).loanNumber).toBe("NMK-2020-1000");
  });

  it("rejects a payload missing a required field", () => {
    const { riskCategory, ...incomplete } = validLoan;
    void riskCategory;

    const result = loanSummarySchema.safeParse(incomplete);

    expect(result.success).toBe(false);
    expect(result.error?.issues[0]?.path).toEqual(["riskCategory"]);
  });

  it("rejects a risk category outside the domain enum", () => {
    const result = loanSummarySchema.safeParse({ ...validLoan, riskCategory: "Exploding" });

    expect(result.success).toBe(false);
  });
});

describe("loanTriagePageSchema", () => {
  it("validates the envelope and every item", () => {
    const page = loanTriagePageSchema.parse({
      items: [validLoan],
      page: 1,
      pageSize: 10,
      totalCount: 50,
      totalPages: 5,
      hasNextPage: true,
    });

    expect(page.items).toHaveLength(1);
  });

  it("rejects the page when a single item is malformed", () => {
    const result = loanTriagePageSchema.safeParse({
      items: [{ ...validLoan, outstandingBalance: "lots" }],
      page: 1,
      pageSize: 10,
      totalCount: 50,
      totalPages: 5,
      hasNextPage: true,
    });

    expect(result.success).toBe(false);
  });
});

describe("maturityWallSchema", () => {
  it("accepts an array of buckets", () => {
    const buckets = maturityWallSchema.parse([
      {
        year: 2027,
        loanCount: 14,
        totalBalance: 730479212.22,
        criticalBalance: 476801403.15,
        warningBalance: 253677809.07,
        performingBalance: 0,
        shareOfPortfolio: 0.2816,
      },
    ]);

    expect(buckets[0].year).toBe(2027);
  });

  it("rejects a bare object", () => {
    expect(maturityWallSchema.safeParse({ year: 2027 }).success).toBe(false);
  });
});

describe("tapeSyncResultSchema", () => {
  it("accepts the sync envelope", () => {
    const result = tapeSyncResultSchema.parse({
      message: "Servicing tape successfully synced.",
      recordsInserted: 50,
      syncedAtUtc: "2026-09-19T02:51:39.694823+00:00",
    });

    expect(result.recordsInserted).toBe(50);
  });

  it("rejects a missing record count", () => {
    const result = tapeSyncResultSchema.safeParse({
      message: "Servicing tape successfully synced.",
      syncedAtUtc: "2026-09-19T02:51:39.694823+00:00",
    });

    expect(result.success).toBe(false);
  });
});

describe("problemDocumentSchema", () => {
  it("parses an RFC 7807 document from the API", () => {
    const problem = problemDocumentSchema.parse({
      title: "Invalid financial input.",
      detail: "Stress rate must be between 1% and 25%, but was 80%.",
      status: 400,
      parameterName: "percent",
    });

    expect(problem.title).toBe("Invalid financial input.");
  });

  it("tolerates a document with no detail", () => {
    expect(problemDocumentSchema.safeParse({ title: "Servicing tape is locked." }).success).toBe(
      true,
    );
  });
});

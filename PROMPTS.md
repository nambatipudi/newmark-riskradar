1. The Contract-First TDD Harness

@workspace /tests Read the requirements for FinancialMathService. Write an xUnit test suite in the Domain.Tests project covering DSCR, Debt Yield, and Months to Maturity. You must include explicit test cases asserting that an InvalidFinancialInputException is thrown if debt service or loan balance is zero or negative.

2. The EF Core SQLite Scaffolding Harness
@workspace Generate the RiskRadarDbContext in the Infrastructure layer and the SqliteLoanRepository. Follow the exact Entity boundaries defined in the Domain layer. Include a Fluent API configuration for CommercialLoan that maps the Money and Dscr value objects to scalar columns.

3. The Staff Architecture Code Review (Run before committing)
@workspace Act as a Principal .NET Architect performing a PR review on the latest staged changes. Check strictly for three things: 1) Did any EF Core or ASP.NET dependencies leak into the Domain project? 2) Are there any raw floating-point calculations instead of decimal? 3) Are domain exceptions handled in the API layer via a global exception middleware? List your findings.

NewmarkRiskRadar-UI
1. The RSC / Client Boundary Scaffold
@workspace Generate the LoanTriageGrid feature. Separate this into two files: a Server Component (page.tsx) that extracts searchParams and fetches the triaged loans using the Zod API client, and a Client Component (TriageFilters.tsx) that renders the select dropdowns and pushes new searchParams to the URL. Use shadcn/ui components.

2. The Zod Contract Test Harness
@workspace /tests Generate a Vitest suite for src/lib/api/schemas.ts. Write tests proving that the TriagedLoanSchema successfully parses a valid backend JSON payload, and correctly throws a ZodError if a required field like 'dscr' or 'monthsToMaturity' is missing or returned as a string instead of a number.

3. The Render Optimization Code Review (Run before committing)
@workspace Act as a Principal Frontend Engineer reviewing the Next.js App Router code. Audit my components and flag any instances where: 1) I used "use client" too high in the tree, unnecessarily forcing child components into client-side rendering. 2) A Zod schema does not match the TypeScript interface. 3) Any hydration mismatches could occur from date formatting.

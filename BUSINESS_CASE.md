# The Business Case: Why RiskRadar?

**To:** Sunil, Jessica, and the Newmark Engineering Team
**From:** Narayan Ambatipudi
**Re:** Engineering Assignment – Rationale & Business Context

When evaluating the open-ended prompt to "identify a meaningful problem worth solving," I stepped back from the technology stack to look at the macroeconomic reality of commercial real estate in 2026.

Software engineering at the Staff level is not about writing clever algorithms; it is about building leverage for the business. Here is the story of how I arrived at this specific solution and why it directly aligns with Newmark's profit engines.

### The Macro Problem: The Commercial Real Estate Maturity Wall

The commercial real estate industry is currently navigating a historic debt maturity wall. Trillions of dollars in CRE loans originated during the zero-interest-rate environment are coming due.

When these loans mature, borrowers must refinance at today's significantly higher interest rates. An office or industrial asset that was perfectly healthy at a 3.5% interest rate often instantly defaults at a 7.5% rate because the property's Net Operating Income (NOI) can no longer cover the doubled debt service. Asset managers are racing to identify which loans will survive refinancing and which require immediate distress workouts.

### Why This is Highly Relevant to Newmark

Newmark is not just a brokerage; it is a transactional and servicing powerhouse. With a massive loan servicing portfolio and a dominant Capital Markets and Debt Placement business (exemplified by winning mandates like the Signature Bank portfolio liquidation), Newmark sits right at the center of this maturity crisis.

However, commercial loan data is notoriously trapped in fragmented Excel tapes, legacy systems, and unstructured PDFs. When an asset manager or debt broker needs to stress-test a $2 billion portfolio against a 100-basis-point rate hike, it historically requires days of manual spreadsheet modeling. By the time the data is compiled, the market—or the client's patience—has moved on.

### The Solution: From Static Reporting to Active Triage

I built **RiskRadar** to solve this specific bottleneck.

Instead of building a generic CRM or a basic AI chat wrapper, I wanted to build an active underwriting and triage engine. RiskRadar ingests raw loan data and runs deterministic financial mathematics to instantly categorize a portfolio into actionable risk tiers (Critical Default, Elevated Risk, Performing).

More importantly, it includes a **Pro-Forma Stress Test Simulator**. By moving a slider, a Newmark asset manager can instantly visualize how a 1.5% interest rate hike expands the "Critical Default" red zone across the maturity timeline. This turns a static data table into a dynamic advisory tool that brokers can use to win restructuring mandates.

### The Architectural Philosophy

To build this right, the architecture had to reflect the business requirements:

1. **Deterministic Math over Hallucination:** In institutional finance, AI cannot be trusted to calculate a Debt Service Coverage Ratio (DSCR) or Debt Yield. I implemented a strict Domain-Driven Design (DDD) in the .NET backend. The financial math is isolated, pure, and exhaustively testable, ensuring zero precision loss or division-by-zero errors.
2. **Shareable Enterprise State:** In the Next.js frontend, I deliberately avoided hidden client state (like Redux) for the triage filters. By binding the stress-test parameters and asset class filters directly to the URL SearchParams, I built a tool where a Newmark analyst can simulate a disaster scenario and instantly email that exact URL to a Chief Risk Officer or Investment Committee.
3. **Decoupled Scale:** By keeping the UI and API as two strictly independent, containerized projects, I ensured that the backend financial engine could eventually be consumed by other Newmark platforms—whether an internal mobile app or a client-facing portal—without untangling frontend logic.

I chose this problem because it sits exactly where technology creates the most value in commercial real estate today: compressing the time it takes to turn messy financial data into actionable, mandate-winning advisory intelligence.

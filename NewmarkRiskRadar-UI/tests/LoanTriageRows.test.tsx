import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";

import { LoanTriageRows } from "@/components/features/LoanTriageRows";
import type { LoanSummary } from "@/types/api";

function buildLoan(overrides: Partial<LoanSummary> = {}): LoanSummary {
  return {
    id: "01352836-0000-0000-0000-000001000000",
    loanNumber: "NMK-2020-1000",
    borrowerName: "Kestrel Industrial Partners",
    propertyName: "Union Tower",
    market: "Dallas, TX",
    propertyType: "MixedUse",
    originationDate: "2019-10-12",
    maturityDate: "2026-10-12",
    maturityYear: 2026,
    monthsToMaturity: 0,
    outstandingBalance: 26_880_000,
    originalBalance: 26_880_000,
    netOperatingIncome: 2_167_637.31,
    annualDebtService: 1_542_912,
    appraisedValue: 44_800_000,
    interestRate: 0.0574,
    amortizationYears: 0,
    dscr: 1.4048,
    stressedAnnualDebtService: 2_553_600,
    stressedDscr: 0.8489,
    debtYield: 0.0806,
    loanToValue: 0.6,
    riskCategory: "Critical",
    riskReasons: ["Stressed DSCR of 0.85 is below the 1.00 break-even threshold."],
    ...overrides,
  };
}

function renderRows(loans: LoanSummary[], stressed = false) {
  const user = userEvent.setup();
  render(
    <table>
      <tbody>
        <LoanTriageRows loans={loans} stressed={stressed} />
      </tbody>
    </table>,
  );
  return user;
}

describe("LoanTriageRows", () => {
  it("renders one actionable row per loan", () => {
    renderRows([buildLoan(), buildLoan({ id: "b", loanNumber: "NMK-2021-1001" })]);

    expect(screen.getAllByRole("button", { name: /Open details for loan/ })).toHaveLength(2);
  });

  it("shows the in-place coverage when no scenario is applied", () => {
    renderRows([buildLoan()]);

    expect(screen.getByText("1.40x")).toBeInTheDocument();
    expect(screen.queryByText("0.85x")).not.toBeInTheDocument();
  });

  it("shows stressed coverage with the in-place ratio struck through", () => {
    renderRows([buildLoan()], true);

    expect(screen.getByText("0.85x")).toBeInTheDocument();
    expect(screen.getByText("1.40x")).toBeInTheDocument();
  });

  it("humanises the asset class enum", () => {
    renderRows([buildLoan()]);

    expect(screen.getByText("Mixed Use")).toBeInTheDocument();
  });

  it("opens the deep dive drawer on click", async () => {
    const user = renderRows([buildLoan()]);

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /NMK-2020-1000/ }));

    const drawer = await screen.findByRole("dialog");
    expect(within(drawer).getByText("NMK-2020-1000")).toBeInTheDocument();
    expect(within(drawer).getByText("Union Tower")).toBeInTheDocument();
    expect(within(drawer).getByText("$26,880,000.00")).toBeInTheDocument();
    expect(within(drawer).getByText("$2,167,637.31")).toBeInTheDocument();
    expect(within(drawer).getByText("5.74%")).toBeInTheDocument();
    expect(within(drawer).getByText("Oct 12, 2026")).toBeInTheDocument();
    expect(within(drawer).getByText("Interest only")).toBeInTheDocument();
  });

  it("explains the risk rating inside the drawer", async () => {
    const user = renderRows([buildLoan()]);

    await user.click(screen.getByRole("button", { name: /NMK-2020-1000/ }));

    const drawer = await screen.findByRole("dialog");
    expect(within(drawer).getByText("Why this rating")).toBeInTheDocument();
    expect(
      within(drawer).getByText(/below the 1.00 break-even threshold/),
    ).toBeInTheDocument();
  });

  it("opens the drawer from the keyboard", async () => {
    const user = renderRows([buildLoan()]);

    screen.getByRole("button", { name: /NMK-2020-1000/ }).focus();
    await user.keyboard("{Enter}");

    expect(await screen.findByRole("dialog")).toBeInTheDocument();
  });

  it("closes the drawer on Escape", async () => {
    const user = renderRows([buildLoan()]);

    await user.click(screen.getByRole("button", { name: /NMK-2020-1000/ }));
    await screen.findByRole("dialog");

    await user.keyboard("{Escape}");

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("shows the selected loan when a different row is opened", async () => {
    const user = renderRows([
      buildLoan(),
      buildLoan({ id: "b", loanNumber: "NMK-2021-1001", propertyName: "Cedar Point Flats" }),
    ]);

    await user.click(screen.getByRole("button", { name: /NMK-2021-1001/ }));

    const drawer = await screen.findByRole("dialog");
    expect(within(drawer).getByText("Cedar Point Flats")).toBeInTheDocument();
  });
});

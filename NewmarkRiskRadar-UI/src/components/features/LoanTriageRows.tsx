"use client";

import { useState } from "react";

import { Badge } from "@/components/ui/badge";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { TableCell, TableRow } from "@/components/ui/table";
import {
  cn,
  formatDate,
  formatExactCurrency,
  formatCompactCurrency,
  formatPercent,
  formatPropertyType,
  formatRatio,
} from "@/lib/utils";
import type { LoanSummary, RiskCategory } from "@/types/api";

/** Coverage below this cannot clear a refinance, so the desk wants it to shout. */
const COVERAGE_ALERT_THRESHOLD = 1.2;

const RISK_VARIANT: Record<RiskCategory, "critical" | "warning" | "performing"> = {
  Critical: "critical",
  Warning: "warning",
  Performing: "performing",
};

interface LoanTriageRowsProps {
  loans: LoanSummary[];
  stressed: boolean;
}

/**
 * Client leaf of the triage table. The rows are handed down already fetched, so opening the
 * drawer is pure local state and never touches the URL or refetches the book.
 */
export function LoanTriageRows({ loans, stressed }: LoanTriageRowsProps) {
  const [selectedLoan, setSelectedLoan] = useState<LoanSummary | null>(null);

  return (
    <>
      {loans.map((loan) => {
        const coverage = stressed ? loan.stressedDscr : loan.dscr;

        return (
          <TableRow
            key={loan.id}
            role="button"
            tabIndex={0}
            aria-label={`Open details for loan ${loan.loanNumber}`}
            className="cursor-pointer hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
            onClick={() => setSelectedLoan(loan)}
            onKeyDown={(event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                setSelectedLoan(loan);
              }
            }}
          >
            <TableCell>
              <p className="font-bold text-foreground">{loan.loanNumber}</p>
              <p className="text-xs text-muted-foreground">{loan.borrowerName}</p>
            </TableCell>
            <TableCell>
              <p className="text-foreground">{formatPropertyType(loan.propertyType)}</p>
              <p className="text-xs text-muted-foreground">{loan.market}</p>
            </TableCell>
            <TableCell className="text-right tabular-nums text-foreground">
              {formatCompactCurrency(loan.netOperatingIncome)}
            </TableCell>
            <TableCell className="text-right tabular-nums">
              <span className="text-foreground">{loan.monthsToMaturity}</span>
              <span className="ml-2 text-xs text-muted-foreground">
                {formatDate(loan.maturityDate)}
              </span>
            </TableCell>
            <TableCell
              className={cn(
                "text-right font-medium tabular-nums text-foreground",
                coverage != null && coverage < COVERAGE_ALERT_THRESHOLD && "text-destructive",
              )}
            >
              {formatRatio(coverage)}
              {stressed && loan.dscr != null ? (
                <span className="ml-2 text-xs font-normal text-muted-foreground line-through">
                  {formatRatio(loan.dscr)}
                </span>
              ) : null}
            </TableCell>
            <TableCell>
              <Badge variant={RISK_VARIANT[loan.riskCategory]}>{loan.riskCategory}</Badge>
            </TableCell>
          </TableRow>
        );
      })}

      <Sheet open={selectedLoan !== null} onOpenChange={(open) => !open && setSelectedLoan(null)}>
        <SheetContent>
          {selectedLoan ? <LoanDetail loan={selectedLoan} stressed={stressed} /> : null}
        </SheetContent>
      </Sheet>
    </>
  );
}

function LoanDetail({ loan, stressed }: { loan: LoanSummary; stressed: boolean }) {
  return (
    <>
      <SheetHeader>
        <div className="flex items-center gap-3">
          <SheetTitle>{loan.loanNumber}</SheetTitle>
          <Badge variant={RISK_VARIANT[loan.riskCategory]}>{loan.riskCategory}</Badge>
        </div>
        <SheetDescription>
          {loan.borrowerName} &middot; {loan.market}
        </SheetDescription>
      </SheetHeader>

      <dl className="grid grid-cols-2 gap-x-6 gap-y-4">
        <Field label="Property name" value={loan.propertyName} className="col-span-2" />
        <Field label="Asset class" value={formatPropertyType(loan.propertyType)} />
        <Field label="Amortization" value={loan.amortizationYears === 0 ? "Interest only" : `${loan.amortizationYears} years`} />
        <Field label="Outstanding balance" value={formatExactCurrency(loan.outstandingBalance)} />
        <Field label="Net operating income" value={formatExactCurrency(loan.netOperatingIncome)} />
        <Field label="Annual debt service" value={formatExactCurrency(loan.annualDebtService)} />
        <Field label="Appraised value" value={formatExactCurrency(loan.appraisedValue)} />
        <Field label="Interest rate" value={formatPercent(loan.interestRate, 2)} />
        <Field label="Origination date" value={formatDate(loan.originationDate)} />
        <Field label="Maturity date" value={formatDate(loan.maturityDate)} />
        <Field label="Months to maturity" value={String(loan.monthsToMaturity)} />
        <Field label="DSCR" value={formatRatio(loan.dscr)} />
        <Field label="Debt yield" value={formatPercent(loan.debtYield, 2)} />
        <Field label="Loan to value" value={formatPercent(loan.loanToValue, 2)} className="col-span-2" />

        {stressed ? (
          <>
            <Field
              label="Stressed debt service"
              value={formatExactCurrency(loan.stressedAnnualDebtService ?? 0)}
            />
            <Field
              label="Stressed DSCR"
              value={formatRatio(loan.stressedDscr)}
              className={
                loan.stressedDscr != null && loan.stressedDscr < COVERAGE_ALERT_THRESHOLD
                  ? "text-destructive"
                  : undefined
              }
            />
          </>
        ) : null}
      </dl>

      <div className="border-t border-border pt-4">
        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          Why this rating
        </p>
        <ul className="mt-2 space-y-1.5">
          {loan.riskReasons.map((reason) => (
            <li key={reason} className="text-sm text-foreground">
              {reason}
            </li>
          ))}
        </ul>
      </div>
    </>
  );
}

function Field({
  label,
  value,
  className,
}: {
  label: string;
  value: string;
  className?: string;
}) {
  return (
    <div className={className}>
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className={cn("mt-0.5 text-sm font-medium tabular-nums text-foreground", className)}>
        {value}
      </dd>
    </div>
  );
}

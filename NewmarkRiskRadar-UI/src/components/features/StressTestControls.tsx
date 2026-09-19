"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Activity, Search } from "lucide-react";

import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Slider } from "@/components/ui/slider";
import {
  ASSET_CLASSES,
  STRESS_RATE_MAX,
  STRESS_RATE_MIN,
  STRESS_RATE_STEP,
  type TriageQuery,
} from "@/lib/api/params";

const DEBOUNCE_MS = 500;

const ASSET_CLASS_OPTIONS = [
  { value: "all", label: "All asset classes" },
  ...ASSET_CLASSES.map((assetClass) => ({
    value: assetClass,
    label: assetClass === "MixedUse" ? "Mixed Use" : assetClass,
  })),
];

interface StressTestControlsProps {
  query: TriageQuery;
}

export function StressTestControls({ query }: StressTestControlsProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  // Local state keeps the thumb and the caret responsive; the URL is updated on a trailing debounce.
  const [rate, setRate] = useState(query.stressRate ?? STRESS_RATE_MIN);
  const [stressEnabled, setStressEnabled] = useState(query.stressRate != null);
  const [search, setSearch] = useState(query.search);

  const commit = useCallback(
    (changes: Record<string, string | null>) => {
      const params = new URLSearchParams(searchParams.toString());

      for (const [key, value] of Object.entries(changes)) {
        if (value === null || value === "") {
          params.delete(key);
        } else {
          params.set(key, value);
        }
      }

      // Any filter change invalidates the current page offset.
      params.delete("page");

      const queryString = params.toString();
      router.push(queryString ? `${pathname}?${queryString}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams],
  );

  const debouncedCommit = useDebouncedCallback(commit, DEBOUNCE_MS);

  return (
    <Card>
      <CardContent className="grid gap-5 p-5 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1fr)] lg:items-end">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <label htmlFor="stress-rate" className="flex items-center gap-2 text-sm font-medium">
              <Activity className="h-4 w-4 text-primary" aria-hidden />
              Stress test rate
            </label>
            <span className="flex items-center gap-3">
              <span
                className={
                  stressEnabled
                    ? "text-sm font-semibold tabular-nums text-warning"
                    : "text-sm font-semibold tabular-nums text-muted-foreground"
                }
              >
                {stressEnabled ? `${rate.toFixed(1)}%` : "In place"}
              </span>
              <button
                type="button"
                className="text-xs text-muted-foreground underline underline-offset-2 hover:text-foreground"
                onClick={() => {
                  const next = !stressEnabled;
                  setStressEnabled(next);
                  commit({ stressRate: next ? rate.toFixed(1) : null });
                }}
              >
                {stressEnabled ? "Reset" : "Apply"}
              </button>
            </span>
          </div>
          <Slider
            id="stress-rate"
            aria-label="Pro-forma refinancing rate"
            min={STRESS_RATE_MIN}
            max={STRESS_RATE_MAX}
            step={STRESS_RATE_STEP}
            value={[rate]}
            onValueChange={([next]) => {
              setRate(next);
              setStressEnabled(true);
              debouncedCommit({ stressRate: next.toFixed(1) });
            }}
          />
          <p className="text-xs text-muted-foreground">
            Re-prices every loan at this take-out rate, holding NOI flat, then re-runs triage.
          </p>
        </div>

        <div className="space-y-2">
          <label htmlFor="asset-class" className="text-sm font-medium">
            Asset class
          </label>
          <Select
            id="asset-class"
            options={ASSET_CLASS_OPTIONS}
            value={query.assetClass}
            onChange={(event) =>
              commit({ assetClass: event.target.value === "all" ? null : event.target.value })
            }
          />
        </div>

        <div className="space-y-2">
          <label htmlFor="loan-search" className="text-sm font-medium">
            Search
          </label>
          <div className="relative">
            <Search
              className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground"
              aria-hidden
            />
            <Input
              id="loan-search"
              value={search}
              placeholder="Borrower, property, market"
              className="pl-8"
              onChange={(event) => {
                setSearch(event.target.value);
                debouncedCommit({ q: event.target.value });
              }}
            />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function useDebouncedCallback<TArgs extends unknown[]>(
  callback: (...args: TArgs) => void,
  delayMs: number,
) {
  const timeout = useRef<ReturnType<typeof setTimeout>>(undefined);
  const latest = useRef(callback);

  useEffect(() => {
    latest.current = callback;
  }, [callback]);

  useEffect(() => () => clearTimeout(timeout.current), []);

  return useCallback(
    (...args: TArgs) => {
      clearTimeout(timeout.current);
      timeout.current = setTimeout(() => latest.current(...args), delayMs);
    },
    [delayMs],
  );
}

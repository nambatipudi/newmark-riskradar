import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { StressTestControls } from "@/components/features/StressTestControls";
import type { TriageQuery } from "@/lib/api/params";

const push = vi.fn();
let searchParams = new URLSearchParams();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push }),
  usePathname: () => "/",
  useSearchParams: () => searchParams,
}));

const inPlace: TriageQuery = {
  risk: "all",
  assetClass: "all",
  stressRate: null,
  search: "",
  page: 1,
};

/** Comfortably longer than the component's 500ms debounce. */
const DEBOUNCE_GRACE_MS = 1500;

beforeEach(() => {
  push.mockClear();
  searchParams = new URLSearchParams();
});

function setup(query: TriageQuery = inPlace) {
  const user = userEvent.setup();
  render(<StressTestControls query={query} />);
  return user;
}

describe("StressTestControls", () => {
  it("shows the in-place state when no scenario is applied", () => {
    setup();

    expect(screen.getByText("In place")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Apply" })).toBeInTheDocument();
  });

  it("renders the applied rate from the URL", () => {
    setup({ ...inPlace, stressRate: 8.5 });

    expect(screen.getByText("8.5%")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reset" })).toBeInTheDocument();
  });

  it("updates the slider label instantly but debounces the URL write", async () => {
    const user = setup();

    screen.getByRole("slider").focus();
    await user.keyboard("{ArrowRight}{ArrowRight}{ArrowRight}");

    // Local state is immediate so the thumb never lags the pointer.
    expect(screen.getByText("3.3%")).toBeInTheDocument();
    expect(push).not.toHaveBeenCalled();

    await waitFor(() => expect(push).toHaveBeenCalledTimes(1), { timeout: DEBOUNCE_GRACE_MS });
    expect(push).toHaveBeenCalledWith("/?stressRate=3.3", { scroll: false });
  });

  it("collapses a burst of typing into a single navigation", async () => {
    const user = setup();

    await user.type(screen.getByLabelText("Search"), "Miami");
    expect(push).not.toHaveBeenCalled();

    await waitFor(() => expect(push).toHaveBeenCalledTimes(1), { timeout: DEBOUNCE_GRACE_MS });
    expect(push).toHaveBeenCalledWith("/?q=Miami", { scroll: false });
  });

  it("applies the asset class filter without waiting for the debounce", async () => {
    const user = setup();

    await user.selectOptions(screen.getByLabelText("Asset class"), "Office");

    expect(push).toHaveBeenCalledWith("/?assetClass=Office", { scroll: false });
  });

  it("clears the asset class filter back to a bare URL", async () => {
    searchParams = new URLSearchParams("assetClass=Office");
    const user = setup({ ...inPlace, assetClass: "Office" });

    await user.selectOptions(screen.getByLabelText("Asset class"), "all");

    expect(push).toHaveBeenCalledWith("/", { scroll: false });
  });

  it("resets the page offset whenever a filter changes", async () => {
    searchParams = new URLSearchParams("page=4");
    const user = setup({ ...inPlace, page: 4 });

    await user.selectOptions(screen.getByLabelText("Asset class"), "Retail");

    expect(push).toHaveBeenCalledWith("/?assetClass=Retail", { scroll: false });
  });

  it("drops the stress rate from the URL on reset", async () => {
    searchParams = new URLSearchParams("stressRate=8.5");
    const user = setup({ ...inPlace, stressRate: 8.5 });

    await user.click(screen.getByRole("button", { name: "Reset" }));

    expect(push).toHaveBeenCalledWith("/", { scroll: false });
  });

  it("preserves unrelated filters when applying a scenario", async () => {
    searchParams = new URLSearchParams("risk=Critical");
    const user = setup({ ...inPlace, risk: "Critical" });

    await user.click(screen.getByRole("button", { name: "Apply" }));

    expect(push).toHaveBeenCalledWith("/?risk=Critical&stressRate=3.0", { scroll: false });
  });
});

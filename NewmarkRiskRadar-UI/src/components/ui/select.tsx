import * as React from "react";
import { ChevronDown } from "lucide-react";

import { cn } from "@/lib/utils";

export interface SelectOption {
  value: string;
  label: string;
}

export interface SelectProps
  extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, "children"> {
  options: SelectOption[];
}

/**
 * Styled native select. A listbox this small gains nothing from a JS popover and keeps
 * platform keyboard, screen reader and mobile behaviour for free.
 */
const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, options, ...props }, ref) => (
    <div className="relative">
      <select
        ref={ref}
        className={cn(
          "h-9 w-full appearance-none rounded-md border border-input bg-background px-3 pr-8 text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50",
          className,
        )}
        {...props}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      <ChevronDown
        className="pointer-events-none absolute right-2.5 top-2.5 h-4 w-4 text-muted-foreground"
        aria-hidden
      />
    </div>
  ),
);
Select.displayName = "Select";

export { Select };

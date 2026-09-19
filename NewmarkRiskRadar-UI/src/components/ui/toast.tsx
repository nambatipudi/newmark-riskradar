"use client";

import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from "react";
import { X } from "lucide-react";

import { cn } from "@/lib/utils";

type ToastVariant = "default" | "destructive";

interface Toast {
  id: number;
  title: string;
  description?: string;
  variant?: ToastVariant;
}

interface ToastContextValue {
  toast: (t: Omit<Toast, "id">) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast must be used within a ToastProvider");
  return ctx;
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextId = useRef(0);

  const toast = useCallback((t: Omit<Toast, "id">) => {
    const id = nextId.current++;
    setToasts((prev) => [...prev, { ...t, id }]);
    setTimeout(() => setToasts((prev) => prev.filter((x) => x.id !== id)), 4000);
  }, []);

  const dismiss = useCallback((id: number) => {
    setToasts((prev) => prev.filter((x) => x.id !== id));
  }, []);

  return (
    <ToastContext value={{ toast }}>
      {children}
      <div
        className="fixed bottom-4 right-4 z-50 flex flex-col gap-2"
        role="status"
        aria-live="polite"
      >
        {toasts.map((t) => (
          <div
            key={t.id}
            className={cn(
              "animate-in slide-in-from-bottom-2 fade-in pointer-events-auto flex w-80 items-start gap-3 rounded-lg border p-4",
              t.variant === "destructive"
                ? "border-destructive/40 bg-destructive/10 text-foreground"
                : "border-border bg-card text-card-foreground",
            )}
          >
            <div className="flex-1">
              <p className="text-sm font-semibold">{t.title}</p>
              {t.description && (
                <p className="mt-1 text-xs text-muted-foreground">{t.description}</p>
              )}
            </div>
            <button
              type="button"
              onClick={() => dismiss(t.id)}
              className="shrink-0 text-muted-foreground hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext>
  );
}

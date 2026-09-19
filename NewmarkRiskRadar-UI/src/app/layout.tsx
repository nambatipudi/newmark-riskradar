import type { Metadata } from "next";
import { Inter } from "next/font/google";

import { ToastProvider } from "@/components/ui/toast";
import { SyncTapeButton } from "@/components/features/SyncTapeButton";

import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Newmark RiskRadar",
  description: "Commercial real estate servicing book triage and maturity wall analytics.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={`${inter.variable} font-sans`}>
        <ToastProvider>
          <div className="min-h-screen bg-background">
            <header className="border-b border-border bg-card">
              <div className="container flex h-16 items-center justify-between">
                <div className="flex items-baseline gap-3">
                  <span className="text-lg font-semibold tracking-tight">Newmark</span>
                  <span className="text-lg font-light tracking-tight text-primary">RiskRadar</span>
                </div>
                <div className="flex items-center gap-4">
                  <SyncTapeButton />
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">
                    CRE Servicing Portfolio
                  </span>
                </div>
              </div>
            </header>
            <main className="container space-y-8 py-8">{children}</main>
          </div>
        </ToastProvider>
      </body>
    </html>
  );
}

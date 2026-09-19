import type { NextConfig } from "next";

/**
 * The browser always talks to same-origin /api, and Next rewrites that to the .NET container.
 * Keeps the API host a runtime concern instead of baking it into the client bundle.
 */
const apiProxyTarget = process.env.API_PROXY_TARGET ?? "http://localhost:5000";

const nextConfig: NextConfig = {
  output: "standalone",
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiProxyTarget}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;

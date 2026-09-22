import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "node",
    include: ["test/**/*.test.ts"], // Add this line
    isolate: false,
    testTimeout: 20_000
  },
});

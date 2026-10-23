import { expectDiagnostics } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { EmitTester } from "./tester.js";

describe("emitter tests", () => {
  it("should reject versioned fallback type", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Test;

      enum Versions {
        v2025_07_01: "2025-07-01";
      }

      @bicepFallbackType()
      model Fail {}
    `);
    // TODO: Need a good way to assert on diagnostic locations
    expectDiagnostics(diagnostics, {
      severity: "error",
      code: "@azure/bicep-typespec-emitter/fallback-type-must-not-be-versioned",
      message: "A model decorated with @bicepFallbackType must not be versioned.",
    });
  });

  it("should reject duplicate fallback types", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      namespace Test;

      @bicepFallbackType()
      model Fail {}

      @bicepFallbackType()
      model Fail2 {}
    `);
    // TODO: Need a good way to assert on diagnostic locations
    expectDiagnostics(diagnostics, [
      {
        severity: "error",
        code: "@azure/bicep-typespec-emitter/fallback-type-must-be-singleton",
        message: "Only one model may be decorated with @bicepFallbackType.",
      },
      {
        severity: "error",
        code: "@azure/bicep-typespec-emitter/fallback-type-must-be-singleton",
        message: "Only one model may be decorated with @bicepFallbackType.",
      },
    ]);
  });
});

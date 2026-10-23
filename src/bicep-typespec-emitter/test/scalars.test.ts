import { expectDiagnosticEmpty, expectDiagnostics } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { EmitTester } from "./tester.js";

describe("scalars", () => {
  it("should emit error for boolean literal types", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.Scalars")
      namespace ScalarTest;

      enum Versions {
        v1: "2025-08-23",
      }

      @bicepResourceType("BooleanLiteral")
      model BooleanLiteral {
        prop: true
      }
    `);
    // TODO: Need a good way to assert on diagnostic locations
    expectDiagnostics(diagnostics, {
      severity: "error",
      code: "@azure/bicep-typespec-emitter/type-not-supported",
      message: "Type true is not supported by Bicep.",
    });
  });

  it("should handle custom scalars", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.Scalars")
      namespace ScalarTest;

      enum Versions {
        v1: "2025-08-23",
      }

      @minLength(1)
      scalar TestScalar extends string;

      @bicepResourceType("BooleanLiteral")
      model BooleanLiteral {
        prop: TestScalar
      }
    `);
    expectDiagnosticEmpty(diagnostics);
  });
});

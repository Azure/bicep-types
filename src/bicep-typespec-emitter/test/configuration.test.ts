import { expectDiagnosticEmpty, expectDiagnostics } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { expectOutputsToMatchSnapshots } from "./assertions.js";
import { EmitTester } from "./tester.js";

describe("configuration tests", () => {
  it("can generate an extension with configuration", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @bicepConfigurationType()
      model Config {
        configProperty: boolean;
      }
      
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Example
      {
        enum Versions {
          v2025_11_11: "2025-11-11",
        }

        @bicepResourceType("ConfigurableExtension")
        model Minimal {
          /** A string property */
          StringProperty: string;
        }
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "configuration", ["index.json", "index.md", "Example/2025-11-11/types.json", "Example/2025-11-11/types.md", "types.json", "types.md"]);
  });

  it("should reject versioned configuration type", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Test;

      enum Versions {
        v2025_07_01: "2025-07-01";
      }

      @bicepConfigurationType()
      model Fail {}
    `);
    // TODO: Need a good way to assert on diagnostic locations
    expectDiagnostics(diagnostics, {
      severity: "error",
      code: "@azure/bicep-typespec-emitter/configuration-type-must-not-be-versioned",
      message: "A model decorated with @bicepConfigurationType must not be versioned.",
    });
  });

  it("should reject duplicate configuration types", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      namespace Test;

      @bicepConfigurationType()
      model Fail {}

      @bicepConfigurationType()
      model Fail2 {}
    `);
    // TODO: Need a good way to assert on diagnostic locations
    expectDiagnostics(diagnostics, [
      {
        severity: "error",
        code: "@azure/bicep-typespec-emitter/configuration-type-must-be-singleton",
        message: "Only one model may be decorated with @bicepConfigurationType.",
      },
      {
        severity: "error",
        code: "@azure/bicep-typespec-emitter/configuration-type-must-be-singleton",
        message: "Only one model may be decorated with @bicepConfigurationType.",
      },
    ]);
  });
});

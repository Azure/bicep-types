import { expectDiagnosticEmpty, expectDiagnostics } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { expectOutputsToMatchSnapshots } from "./assertions.js";
import { EmitTester } from "./tester.js";

describe("versioning tests", () => {
  it("should generate warning for zero versions", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Example;

      enum Versions {}

      @bicepResourceType("exampleExtension")
      model ExampleExtension {
        One: string;
        Two: string;
      }
  `);
    expectDiagnostics(
      diagnostics,
      {
        code: "@azure/bicep-typespec-emitter/namespace-must-be-versioned",
        message: "Namespace Example must be versioned with @versioned or must reside inside a namespace with @versioned.",
        severity: "error",
      },
      {
        strict: true,
      },
    );
  });

  it("can generate types for a single version", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Example;

      enum Versions {
        v2025_08_09: "2025-08-09",
      }

      @bicepResourceType("exampleExtension")
      model ExampleExtension {
        One: string;
        Two: string;
      }
  `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "one-version", ["index.json", "index.md", "Example/2025-08-09/types.json", "Example/2025-08-09/types.md"]);
  });

  it("can generate types for three versions", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Example;

      enum Versions {
        v2025_07_01: "2025-07-01",
        v2025_08_01: "2025-08-01",
        v2025_09_01: "2025-09-01",
      }

      @bicepResourceType("exampleExtension")
      model ExampleExtension {
        Zero: string;

        @removed(Versions.v2025_09_01)
        One: string;

        @added(Versions.v2025_08_01)
        Two: string;

        @added(Versions.v2025_09_01)
        Three: string;
      }
  `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "three-versions", [
      "index.json",
      "index.md",
      "Example/2025-07-01/types.json",
      "Example/2025-07-01/types.md",
      "Example/2025-08-01/types.json",
      "Example/2025-08-01/types.md",
      "Example/2025-09-01/types.json",
      "Example/2025-09-01/types.md",
    ]);
  });

  it("can generate types when versioned namespace is not the extension namespace", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      namespace VersionedNamespace {
        enum Versions {
          v2025_07_01: "2025-07-01",
          v2025_08_01: "2025-08-01",
        }

        @bicepResourceNamespace("Microsoft.Example")
        namespace ExtensionNamespace {
          @bicepResourceType("exampleExtension")
          model ExampleExtension {
            Zero: string;

            @added(Versions.v2025_08_01)
            One: string;
          }
        }
      }
  `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "inside-versioned-ns", [
      "index.json",
      "index.md",
      "VersionedNamespace/2025-07-01/types.json",
      "VersionedNamespace/2025-07-01/types.md",
      "VersionedNamespace/2025-08-01/types.json",
      "VersionedNamespace/2025-08-01/types.md",
    ]);
  });
});

import { expectDiagnosticEmpty, expectDiagnostics } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { expectOutputsToMatchSnapshots } from "./assertions.js";
import { EmitTester } from "./tester.js";

describe("polymorphism", () => {
  it("should reject polymorphism with zero variants", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.Polymorphism")
      namespace PolymorhismTest;

      enum Versions {
        v1: "2025-08-15",
      }

      @discriminator("petKind")
      model Pet {
        name: string;
      }

      @bicepResourceType("ZeroVariants")
      model ZeroVariants {
        default: Pet;
      }
    `);
    expectDiagnostics(diagnostics, {
      severity: "error",
      code: "@azure/bicep-typespec-emitter/discriminated-union-must-have-at-least-one-variant",
      message: "The polymorphic model Pet must have at least one variant.",
    });
  });

  it("can generate single-variant polymorphic models", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.Polymorphism")
      namespace PolymorhismTest;

      enum Versions {
        v1: "2025-08-15",
      }

      @discriminator("petKind")
      model Pet {
        name: string;
      }

      model Dog extends Pet {
        petKind: "dog";
        bark: "yes";
      }

      @bicepResourceType("OneVariant")
      model OneVariant {
        default: Pet;
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "degenerate-polymorphism", ["index.json", "index.md", "PolymorhismTest/2025-08-15/types.json", "PolymorhismTest/2025-08-15/types.md"]);
  });

  it("can generate polymorphic models", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.Polymorphism")
      namespace PolymorhismTest;

      enum Versions {
        v1: "2025-08-15",
      }

      @discriminator("petKind")
      model Pet {
        name: string;
      }

      model Dog extends Pet {
        petKind: "dog";
        bark: "yes";
      }

      model Cat extends Pet {
        petKind: "cat";
        meow: "yes";
      }

      @bicepResourceType("PolymorphismTest")
      model PolymorphismTest {
        default: Pet;
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "polymorphism", ["index.json", "index.md", "PolymorhismTest/2025-08-15/types.json", "PolymorhismTest/2025-08-15/types.md"]);
  });
});

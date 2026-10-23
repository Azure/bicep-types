import { expectDiagnosticEmpty, expectDiagnostics } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { expectOutputsToMatchSnapshots } from "./assertions.js";
import { EmitTester } from "./tester.js";

describe("discriminated unions", () => {
  it("can emit diagnostic for zero-variant discriminated union", async () => {
    const [, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.DiscriminatedUnions")
      namespace DiscriminatedUnionTest;

      enum Versions {
        v1: "2025-08-15",
      }

      @discriminated
      union EmptyDiscriminatedUnion {}

      @bicepResourceType("DiscriminatedUnionTest")
      model DiscriminatedUnionTest {
        empty: EmptyDiscriminatedUnion;
      }
    `);
    expectDiagnostics(diagnostics, {
      code: "@azure/bicep-typespec-emitter/discriminated-union-must-have-at-least-one-variant",
      severity: "error",
      message: "The discriminated union EmptyDiscriminatedUnion must have at least one variant.",
    });
  });

  it("can simplify degenerate discriminated unions", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.DegenerateUnions")
      namespace DegenerateUnionTest;

      enum Versions {
        v1: "2025-08-15",
      }

      model DefaultVariant {
        name: string;
        test: "hello";
      }

      // The default TypeSpec discriminated union serialization uses the "kind" property as the discriminator.
      // The variants are also serialized as values of the "value" property.
      // See https://typespec.io/docs/standard-library/discriminated-types/ for more details.
      @discriminated
      union DefaultDegenerateUnion {
        One: DefaultVariant,
      }

      model CustomVariant {
        name: string;
        test: "hello there";
      }

      // Change "kind" to "customKind" and "value" to "custom".
      @discriminated(#{
        discriminatorPropertyName: "customKind",
        envelopePropertyName: "custom",
      })
      union CustomDegenerateUnion {
        One: CustomVariant,
      }

      // Should be the same as CustomDicriminatorAndEnvelopeProperty, but with an explicit envelope to make sure
      // we are reacting correctly to the value in the decorator
      @discriminated(#{
        discriminatorPropertyName: "customKind",
        envelopePropertyName: "custom",
        envelope: "object",
      })
      union InlineDegenerateUnion {
        One: CustomVariant,
      }

      model InlineVariant {
        customKind: "One";
        name: string;
        test: "hello world";
      }

      // No envelope property. This is how discriminated unions are typically modeled in Azure.
      @discriminated(#{ discriminatorPropertyName: "customKind", envelope: "none" })
      union InlineDiscriminator {
        One: InlineVariant,
      }

      @bicepResourceType("DegenerateUnion")
      model DiscriminatedUnionTest {
        default: DefaultDegenerateUnion;
        customDiscriminatorAndEnvelope: CustomDegenerateUnion;
        customDiscriminatorAndEnvelopeWithExplicitEnvelope: InlineDegenerateUnion;
        inline: InlineDiscriminator;
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "degenerate-unions", ["index.json", "index.md", "DegenerateUnionTest/2025-08-15/types.json", "DegenerateUnionTest/2025-08-15/types.md"]);
  });

  it("can generate discriminated unions", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Test.DiscriminatedUnions")
      namespace DiscriminatedUnionTest;

      enum Versions {
        v1: "2025-08-15",
      }

      model DefaultCat {
        name: string;
        meow: "yes";
      }

      model DefaultDog {
        name: string;
        bark: "yes";
      }

      // The default TypeSpec discriminated union serialization uses the "kind" property as the discriminator.
      // The variants are also serialized as values of the "value" property.
      // See https://typespec.io/docs/standard-library/discriminated-types/ for more details.
      @discriminated
      union DefaultDiscriminatedUnion {
        Cat: DefaultCat,
        Dog: DefaultDog,
      }

      model CustomCat {
        name: string;
        meow: "yes";
      }

      model CustomDog {
        name: string;
        bark: "yes";
      }

      // Change "kind" to "petKind" and "value" to "pet".
      @discriminated(#{
        discriminatorPropertyName: "petKind",
        envelopePropertyName: "pet",
      })
      union CustomDicriminatorAndEnvelopeProperty {
        Cat: CustomCat,
        Dog: CustomDog,
      }

      // Should be the same as CustomDicriminatorAndEnvelopeProperty, but with an explicit envelope to make sure
      // we are reacting correctly to the value in the decorator
      @discriminated(#{
        discriminatorPropertyName: "petKind",
        envelopePropertyName: "pet",
        envelope: "object",
      })
      union CustomDicriminatorAndEnvelopePropertyWithExplicitEnvelope {
        Cat: CustomCat,
        Dog: CustomDog,
      }

      model InlineCat {
        petKind: "Cat";
        name: string;
        meow: "yes";
      }

      model InlineDog {
        petKind: "Dog";
        name: string;
        bark: "yes";
      }

      // No envelope property. This is how discriminated unions are typically modeled in Azure.
      @discriminated(#{ discriminatorPropertyName: "petKind", envelope: "none" })
      union InlineDiscriminator {
        Cat: InlineCat,
        Dog: InlineDog,
      }

      @bicepResourceType("DiscriminatedUnionTest")
      model DiscriminatedUnionTest {
        default: DefaultDiscriminatedUnion;
        customDiscriminatorAndEnvelope: CustomDicriminatorAndEnvelopeProperty;
        customDiscriminatorAndEnvelopeWithExplicitEnvelope: CustomDicriminatorAndEnvelopePropertyWithExplicitEnvelope;
        inline: InlineDiscriminator;
      }
  `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "discriminated-unions", ["index.json", "index.md", "DiscriminatedUnionTest/2025-08-15/types.json", "DiscriminatedUnionTest/2025-08-15/types.md"]);
  });
});

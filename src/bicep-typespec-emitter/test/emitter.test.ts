import { expectDiagnosticEmpty } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { expectOutputsToMatchSnapshots } from "./assertions.js";
import { BaseTester, createCustomEmitTester, EmitTester } from "./tester.js";

const MinimalBicepExtensionTypeSpec = `
  @versioned(Versions)
  @bicepResourceNamespace("Microsoft.Example")
  namespace Example;

  enum Versions {
    v2025_11_11: "2025-11-11",
  }

  @bicepResourceType("ExampleExtension")
  model Minimal {
    /** A string property */
    StringProperty: string;
  }
`;

describe("emitter tests", () => {
  it("will not throw on empty program", async () => {
    // we don't want to include any imports by default
    const [, diagnostics] = await BaseTester.compileAndDiagnose(``);
    expectDiagnosticEmpty(diagnostics);
  });

  it("it will not throw on empty program when the emitter library is imported", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(``);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "empty-program", ["index.json", "index.md"]);
  });

  it("it can generate just the fallback type", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @bicepFallbackType()
      model TestFallBackType {
        prop1: string;
        prop2: int32;
      };
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "fallback-type", ["index.json", "index.md", "types.json", "types.md"]);
  });

  it("can generate bicep type with basic property types and property descriptions", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Example;

      enum Versions {
        v2020_10_01: "2020-10-01",
      }

      model PayloadProperty<ValueType> {
        value: ValueType;

        type?: string;
      }

      @bicepResourceType("ExampleExtension")
      model SetCertificateIssuer {
        /** A string property */
        StringProperty: string;

        /** 
         * A boolean property 
         */
        BoolProperty: boolean;

        @doc("A string boolean property")
        StringBoolProperty: "true" | "false";

        @doc("An array property")
        StringArrayProperty: string[];

        /** 
         * An int64 property 
         */
        Int64Property: int64;

        TemplatizedProperty: PayloadProperty<string>;
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "basic-property-types", ["index.json", "index.md", "Example/2020-10-01/types.json", "Example/2020-10-01/types.md"]);
  });

  it("can generate a write-only resource", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Example;

      enum Versions {
        v2020_10_01: "2020-10-01",
      }

      @bicepResourceType("ExampleExtension")
      @bicepResourceTypeOptions(BicepResourceCapability.Writeable)
      model WriteOnlyResource {
        @bicepIdentifierProperty()
        StringProperty: string;
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "write-only-resource", ["index.json", "index.md", "Example/2020-10-01/types.json", "Example/2020-10-01/types.md"]);
  });

  it("can generate a read-only resource", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @versioned(Versions)
      @bicepResourceNamespace("Microsoft.Example")
      namespace Example;

      enum Versions {
        v2020_10_01: "2020-10-01",
      }

      @bicepResourceType("ExampleExtension")
      @bicepResourceTypeOptions(BicepResourceCapability.Readable)
      model ReadOnlyResource {
        @bicepIdentifierProperty()
        StringProperty: string;
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "read-only-resource", ["index.json", "index.md", "Example/2020-10-01/types.json", "Example/2020-10-01/types.md"]);
  });

  it("can generate a preview extension", async () => {
    const tester = createCustomEmitTester({
      "bicep-extension-name": "PreviewExtension",
      "bicep-extension-version": "1.0.0-preview",
      "bicep-extension-is-preview": true,
    });
    const [result, diagnostics] = await tester.compileAndDiagnose(MinimalBicepExtensionTypeSpec);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "preview-extension", ["index.json", "index.md", "Example/2025-11-11/types.json", "Example/2025-11-11/types.md"]);
  });

  it("can generate a deprecated extension", async () => {
    const tester = createCustomEmitTester({
      "bicep-extension-name": "DeprecatedExtension",
      "bicep-extension-version": "1.0.0",
      "bicep-extension-is-deprecated": true,
    });
    const [result, diagnostics] = await tester.compileAndDiagnose(MinimalBicepExtensionTypeSpec);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "deprecated-extension", ["index.json", "index.md", "Example/2025-11-11/types.json", "Example/2025-11-11/types.md"]);
  });

  it("can generate a singleton extension", async () => {
    const tester = createCustomEmitTester({
      "bicep-extension-name": "SingletonExtension",
      "bicep-extension-version": "1.0.0-justone",
      "bicep-extension-is-singleton": true,
    });
    const [result, diagnostics] = await tester.compileAndDiagnose(MinimalBicepExtensionTypeSpec);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "singleton-extension", ["index.json", "index.md", "Example/2025-11-11/types.json", "Example/2025-11-11/types.md"]);
  });

  it("can generate a namespace function", async () => {
    const [result, diagnostics] = await EmitTester.compileAndDiagnose(`
      @bicepNamespaceFunctionType()
      /** A function that can be used in bicep files */
      model ExampleFunction {
        name: "exampleFunction";
        parameters: {
          /** A compile-time constant parameter */
          @bicepNamespaceFunctionParameterFlags(BicepNamespaceFunctionParameterFlag.CompileTimeConstant)
          config: string;
        };
        outputType: unknown;
      }
    `);
    expectDiagnosticEmpty(diagnostics);
    await expectOutputsToMatchSnapshots(result, "namespace-function", ["index.json", "index.md", "types.json", "types.md"]);
  });
});

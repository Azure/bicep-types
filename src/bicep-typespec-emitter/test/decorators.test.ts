import { expectDiagnosticEmpty, expectDiagnostics } from "@typespec/compiler/testing";
import { describe, it } from "vitest";
import { Tester } from "./tester.js";

describe("decorator tests", () => {
  it("can compile decorators", async () => {
    const diagnostics = await Tester.diagnose(`
      @bicepResourceNamespace("Hello.There")
      namespace Hello;

      @bicepResourceType("doAction")
      model Foo {};
    `);
    expectDiagnostics(diagnostics, {
      code: "@azure/bicep-typespec-emitter/namespace-must-be-versioned",
      message: "Namespace Hello must be versioned with @versioned or must reside inside a namespace with @versioned.",
    });
  });

  // Check diagnostics are emitted
  it("can log errors on empty namespace", async () => {
    const diagnostics = await Tester.diagnose(`
    @bicepResourceNamespace("")
    namespace Hello;
  `);
    expectDiagnostics(
      diagnostics,
      {
        code: "invalid-argument",
        message: "Argument of type '\"\"' is not assignable to parameter of type 'valueof Bicep.Extensibility.BicepResourceNamespaceString'",
        severity: "error",
      },
      {
        strict: true,
      },
    );
  });

  // Check diagnostics are emitted
  it("can log errors on empty extension type", async () => {
    const diagnostics = await Tester.diagnose(`
      namespace Hello;

      @bicepResourceType("")
      model Foo {};
  `);
    expectDiagnostics(
      diagnostics,
      {
        code: "invalid-argument",
        message: "Argument of type '\"\"' is not assignable to parameter of type 'valueof Bicep.Extensibility.BicepResourceTypeString'",
        severity: "error",
      },
      {
        strict: true,
      },
    );
  });

  it("can compile bicep fallback type decorator", async () => {
    const diagnostics = await Tester.diagnose(`
      namespace Hello;

      @bicepFallbackType()
      model Foo {};
  `);
    expectDiagnosticEmpty(diagnostics);
  });

  it("can compile bicep namespace function decorator", async () => {
    const diagnostics = await Tester.diagnose(`
      namespace Hello;

      @bicepNamespaceFunctionType()
      model MyFunction {
        name: "myFunction";
        parameters: {};
        outputType: string;
      };
    `);
    expectDiagnosticEmpty(diagnostics);
  });

  it("can compile bicep namespace function with parameter flag decorator", async () => {
    const diagnostics = await Tester.diagnose(`
      namespace Hello;

      @bicepNamespaceFunctionType()
      model MyFunction {
        name: "myFunction";
        parameters: {
          @bicepNamespaceFunctionParameterFlags(BicepNamespaceFunctionParameterFlag.CompileTimeConstant)
          findKey: string;
        };
        outputType: string;
      };
    `);
    expectDiagnosticEmpty(diagnostics);
  });

  it("rejects empty model with @bicepNamespaceFunctionType", async () => {
    const diagnostics = await Tester.diagnose(`
      namespace Hello;

      @bicepNamespaceFunctionType()
      model Test {};
    `);
    expectDiagnostics(diagnostics, {
      code: "decorator-wrong-target",
      message: "Cannot apply @bicepNamespaceFunctionType decorator to Hello.Test since it is not assignable to Bicep.Extensibility.BicepNamespaceFunction",
      severity: "error",
    });
  });

  it("rejects model missing a property with @bicepNamespaceFunctionType", async () => {
    const diagnostics = await Tester.diagnose(`
      namespace Hello;

      @bicepNamespaceFunctionType()
      model MyFunc {
        parameters: {};
      };
    `);
    expectDiagnostics(diagnostics, {
      code: "decorator-wrong-target",
      message: "Cannot apply @bicepNamespaceFunctionType decorator to Hello.MyFunc since it is not assignable to Bicep.Extensibility.BicepNamespaceFunction",
      severity: "error",
    });
  });

  it("rejects bicep namespace function parameter flag decorator with an invalid value", async () => {
    const diagnostics = await Tester.diagnose(`
      namespace Hello;

      model MyFunction {
        @bicepNamespaceFunctionParameterFlags("notAFlag")
        findKey: string;
      };
    `);
    expectDiagnostics(
      diagnostics,
      {
        code: "invalid-argument",
        message: "Argument of type '\"notAFlag\"' is not assignable to parameter of type 'valueof Bicep.Extensibility.BicepNamespaceFunctionParameterFlag'",
        severity: "error",
      },
      {
        strict: true,
      },
    );
  });
});

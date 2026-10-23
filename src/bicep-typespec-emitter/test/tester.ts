import { resolvePath } from "@typespec/compiler";
import { createTester } from "@typespec/compiler/testing";
import { BicepEmitterOptions } from "../src/lib.js";

export const BaseTester = createTester(resolvePath(import.meta.dirname, ".."), {
  libraries: ["@azure/bicep-typespec-emitter", "@typespec/versioning"], // Add other libraries you depend on in your tests
});

// Tester that automatically imports our library and the versioning library and add usings.
export const Tester = BaseTester.import("@azure/bicep-typespec-emitter", "@typespec/versioning").using("Bicep.Extensibility", "TypeSpec.Versioning");

export const EmitTester = Tester.emit("@azure/bicep-typespec-emitter", {
  "bicep-extension-name": "test",
  "bicep-extension-version": "1.0.0",
});

export function createCustomEmitTester(options: BicepEmitterOptions) {
  // TODO: Is there a way to avoid the double cast?
  return Tester.emit("@azure/bicep-typespec-emitter", options as unknown as Record<string, unknown>);
}
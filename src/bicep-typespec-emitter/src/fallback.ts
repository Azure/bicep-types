import { Program } from "@typespec/compiler";
import { TypeFactory, TypeReference } from "@azure/bicep-types";
import { getSingletonDecoratedModel, isBicepFallbackType } from "./decorators.js";
import { createFallbackTypeMustNotBeVersionedDiagnostic, createFallbackTypeSingletonDiagnostic } from "./diagnostics.js";
import { generateType } from "./transform.js";

export function generateFallbackType(program: Program, typeFactory: TypeFactory): TypeReference | undefined {
  const fallbackTypeModel = getSingletonDecoratedModel(program, isBicepFallbackType, createFallbackTypeMustNotBeVersionedDiagnostic, createFallbackTypeSingletonDiagnostic);
  if (fallbackTypeModel === undefined) {
    return undefined;
  }

  const fallbackBodyType = generateType(program, typeFactory, fallbackTypeModel, fallbackTypeModel);
  if (fallbackBodyType === undefined) {
    return undefined;
  }

  return typeFactory.addUnscopedResourceType("FallbackResourceType", fallbackBodyType, false, true);
}

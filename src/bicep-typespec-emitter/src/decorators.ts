import { navigateProgram, type DecoratorContext, type EnumValue, type Model, type ModelProperty, type Namespace, type Program } from "@typespec/compiler";

import { StateKeys } from "./lib.js";
import { createModelMustBeInNamespaceDiagnostic } from "./diagnostics.js";
import { findVersionedNamespace } from "@typespec/versioning";

export function $bicepResourceNamespace(context: DecoratorContext, target: Namespace, extensionNamespace: string) {
  context.program.stateMap(StateKeys.bicepResourceNamespace).set(target, extensionNamespace);
}

export function getBicepResourceNamespace(p: Program, n: Namespace): string | undefined {
  return p.stateMap(StateKeys.bicepResourceNamespace).get(n);
}

export type BicepResourceCapability = "Readable" | "Writeable";

export function $bicepResourceType(context: DecoratorContext, target: Model, extensionType: string) {
  context.program.stateMap(StateKeys.bicepResourceType).set(target, extensionType);
}

export function getBicepResourceType(p: Program, m: Model): string | undefined {
  return p.stateMap(StateKeys.bicepResourceType).get(m);
}

export interface BicepResourceTypeOptions {
  capabilities: BicepResourceCapability[];
}

export function $bicepResourceTypeOptions(context: DecoratorContext, target: Model, ...capabilities: EnumValue[]) {
  const convertedCapabilities: BicepResourceCapability[] = [];
  for (const capability of capabilities) {
    // TypeSpec validation should ensure that value matches the constraint
    convertedCapabilities.push(capability.value.name as BicepResourceCapability);
  }

  // by default, Bicep resources are both readable and writeable
  if (convertedCapabilities.length === 0) {
    convertedCapabilities.push("Readable");
    convertedCapabilities.push("Writeable");
  }

  const options: BicepResourceTypeOptions = {
    capabilities: convertedCapabilities,
  };

  context.program.stateMap(StateKeys.bicepResourceTypeOptions).set(target, options);
}

export function getBicepResourceTypeOptions(p: Program, m: Model): BicepResourceTypeOptions | undefined {
  return p.stateMap(StateKeys.bicepResourceTypeOptions).get(m);
}

export function $bicepFallbackType(context: DecoratorContext, target: Model) {
  context.program.stateMap(StateKeys.bicepFallbackType).set(target, true);
}

export function isBicepFallbackType(p: Program, m: Model) {
  return p.stateMap(StateKeys.bicepFallbackType).get(m) !== undefined;
}

export function $bicepConfigurationType(context: DecoratorContext, target: Model) {
  context.program.stateMap(StateKeys.bicepConfigurationType).set(target, true);
}

export function isBicepConfigurationType(p: Program, m: Model) {
  return p.stateMap(StateKeys.bicepConfigurationType).get(m) !== undefined;
}

export function $bicepIdentifierProperty(context: DecoratorContext, target: ModelProperty) {
  context.program.stateMap(StateKeys.bicepIdentifierProperty).set(target, true);
}

export function isBicepIdentifierProperty(p: Program, prop: ModelProperty) {
  return p.stateMap(StateKeys.bicepIdentifierProperty).get(prop) !== undefined;
}

export function $bicepNamespaceFunctionType(context: DecoratorContext, target: Model) {
  context.program.stateMap(StateKeys.bicepNamespaceFunctionType).set(target, true);
}

export function isBicepNamespaceFunctionType(p: Program, m: Model) {
  return p.stateMap(StateKeys.bicepNamespaceFunctionType).get(m) !== undefined;
}

export type BicepNamespaceFunctionParameterFlag = "CompileTimeConstant" | "DeployTimeConstant";

export interface BicepNamespaceFunctionParameterOptions {
  flags: BicepNamespaceFunctionParameterFlag[];
}

export function $bicepNamespaceFunctionParameterFlags(context: DecoratorContext, target: ModelProperty, ...flags: EnumValue[]) {
  const convertedFlags: BicepNamespaceFunctionParameterFlag[] = [];
  for (const flag of flags) {
    convertedFlags.push(flag.value.name as BicepNamespaceFunctionParameterFlag);
  }

  context.program.stateMap(StateKeys.bicepNamespaceFunctionParameterFlags).set(target, convertedFlags);
}

export function getBicepNamespaceFunctionParameterFlags(p: Program, prop: ModelProperty): BicepNamespaceFunctionParameterFlag[] | undefined {
  return p.stateMap(StateKeys.bicepNamespaceFunctionParameterFlags).get(prop);
}

export function getSingletonDecoratedModel(
  program: Program,
  predicateFunc: (p: Program, m: Model) => boolean,
  nonVersionedDiagnosticFunc: (p: Program, m: Model) => void,
  nonSingletonDiagnosticFunc: (p: Program, m: Model) => void,
): Model | undefined {
  const models: Model[] = [];
  navigateProgram(
    program,
    {
      model: (m) => {
        if (predicateFunc(program, m)) {
          models.push(m);

          if (m.namespace === undefined) {
            createModelMustBeInNamespaceDiagnostic(program, m);
            return;
          }

          if (findVersionedNamespace(program, m.namespace) !== undefined) {
            nonVersionedDiagnosticFunc(program, m);
            return;
          }
        }
      },
    },
    {
      includeTemplateDeclaration: false,
      visitDerivedTypes: false,
    },
  );

  if (models.length === 0) {
    // fallback types are optional
    return undefined;
  }

  if (models.length > 1) {
    // the user declared more than one fallback type in the extension
    // report diagnostic on every single one
    for (const fallbackModel of models) {
      nonSingletonDiagnosticFunc(program, fallbackModel);
    }

    return undefined;
  }

  return models[0];
}


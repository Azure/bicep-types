import "../dist/src/index.js";

namespace Bicep.Extensibility;

using TypeSpec.Reflection;

@minLength(1)
scalar BicepResourceNamespaceString extends string;

@minLength(1)
scalar BicepResourceTypeString extends string;

extern dec bicepResourceNamespace(
  target: Namespace,
  extensionNamespace: valueof BicepResourceNamespaceString
);

extern dec bicepResourceType(
  target: Model,
  extensionType: valueof BicepResourceTypeString
);

extern dec bicepResourceTypeOptions(
  target: Model,
  ...capabilities: valueof BicepResourceCapability[]
);

extern dec bicepFallbackType(target: Model);

extern dec bicepConfigurationType(target: Model);

extern dec bicepIdentifierProperty(target: ModelProperty);

/**
 * The minimum shape required for a model decorated with @bicepNamespaceFunctionType.
 */
model BicepNamespaceFunction {
  name: string;
  parameters: {};
  outputType: unknown;
}

extern dec bicepNamespaceFunctionType(target: BicepNamespaceFunction);

extern dec bicepNamespaceFunctionParameterFlags(
  target: ModelProperty,
  ...flags: valueof BicepNamespaceFunctionParameterFlag[]
);

enum BicepResourceCapability {
  Readable,
  Writeable,
}

enum BicepNamespaceFunctionParameterFlag {
  CompileTimeConstant,
  DeployTimeConstant,
}
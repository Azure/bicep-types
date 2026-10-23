import { $bicepConfigurationType, $bicepFallbackType, $bicepIdentifierProperty, $bicepResourceNamespace, $bicepResourceType, $bicepResourceTypeOptions, $bicepNamespaceFunctionType, $bicepNamespaceFunctionParameterFlags } from "./decorators.js";

export { $onEmit } from "./emitter.js";
export { $onValidate } from "./validate.js";
export { $lib } from "./lib.js";

export const $decorators = {
  "Bicep.Extensibility": {
    bicepResourceNamespace: $bicepResourceNamespace,
    bicepResourceType: $bicepResourceType,
    bicepResourceTypeOptions: $bicepResourceTypeOptions,
    bicepFallbackType: $bicepFallbackType,
    bicepConfigurationType: $bicepConfigurationType,
    bicepIdentifierProperty: $bicepIdentifierProperty,
    bicepNamespaceFunctionType: $bicepNamespaceFunctionType,
    bicepNamespaceFunctionParameterFlags: $bicepNamespaceFunctionParameterFlags,
  },
};

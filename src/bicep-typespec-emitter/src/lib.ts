import { createTypeSpecLibrary, JSONSchemaType, paramMessage } from "@typespec/compiler";

export interface BicepEmitterOptions {
  /**
   * The Bicep extension name that will be set in the index.json file.
   */
  "bicep-extension-name": string;

  /**
   * The Bicep extension version that will be set in the index.json file.
   */
  "bicep-extension-version": string;

  /**
   * Flag indicating whether the Bicep extension is a singleton extension. This will be set in the index.json file.
   */
  "bicep-extension-is-singleton"?: boolean;

  /**
   * Flag indicating whether the Bicep extension is in preview. This will be set in the index.json file.
   */
  "bicep-extension-is-preview"?: boolean;

  /**
   * Flag indicating whether the Bicep extension is deprecated. This will be set in the index.json file.
   */
  "bicep-extension-is-deprecated"?: boolean;
}

const BicepEmitterOptionsSchema: JSONSchemaType<BicepEmitterOptions> = {
  type: "object",
  additionalProperties: false,
  properties: {
    "bicep-extension-name": { type: "string" },
    "bicep-extension-version": {
      type: "string",
    },
    "bicep-extension-is-singleton": {
      type: "boolean",
      nullable: true,
      default: false,
    },
    "bicep-extension-is-preview": {
      type: "boolean",
      nullable: true,
      default: false,
    },
    "bicep-extension-is-deprecated": {
      type: "boolean",
      nullable: true,
      default: false,
    },
  },
  required: ["bicep-extension-name", "bicep-extension-version"],
};

export const $lib = createTypeSpecLibrary({
  name: "@azure/bicep-typespec-emitter",
  diagnostics: {
    "type-not-supported": {
      severity: "error",
      messages: {
        default: paramMessage`Type ${"value"} is not supported by Bicep.`,
        kind: paramMessage`Type kind ${"value"} is not supported by Bicep.`,
      },
    },
    "fallback-type-must-be-singleton": {
      severity: "error",
      messages: {
        default: "Only one model may be decorated with @bicepFallbackType.",
      },
    },
    "fallback-type-must-not-be-versioned": {
      severity: "error",
      messages: {
        default: "A model decorated with @bicepFallbackType must not be versioned.",
      },
    },
    "configuration-type-must-be-singleton": {
      severity: "error",
      messages: {
        default: "Only one model may be decorated with @bicepConfigurationType.",
      },
    },
    "configuration-type-must-not-be-versioned": {
      severity: "error",
      messages: {
        default: "A model decorated with @bicepConfigurationType must not be versioned.",
      },
    },
    "only-string-indexer-allowed": {
      severity: "error",
      messages: {
        default: "Only unconstrained strings can be model indexers in Bicep Types.",
      },
    },
    "model-must-be-in-namespace": {
      severity: "error",
      messages: {
        default: paramMessage`Model ${"model"} must be in a namespace to be emitted as a Bicep Type.`,
      },
    },
    "namespace-must-be-decorated": {
      severity: "error",
      messages: {
        default: paramMessage`Namespace ${"namespace"} must be decorated with @bicepResourceNamespace.`,
      },
    },
    "namespace-must-be-versioned": {
      severity: "error",
      messages: {
        default: paramMessage`Namespace ${"namespace"} must be versioned with @versioned or must reside inside a namespace with @versioned.`,
      },
    },
    "scalar-range-was-widened": {
      severity: "warning",
      messages: {
        default: paramMessage`Bicep does not have an exact equivalent to scalar type ${"intrinsicScalar"}. The closest matching type of ${"closestMatch"} will be used instead. The allowed range of values will be widened as a result.`,
      },
    },
    "scalar-range-was-narrowed": {
      severity: "warning",
      messages: {
        default: paramMessage`Bicep does not have an exact equivalent to scalar type ${"intrinsicScalar"}. The closest matching type of ${"closestMatch"} will be used instead. The allowed range of values will be narrowed as a result.`,
      },
    },
    "discriminated-union-variant-must-be-an-object": {
      severity: "error",
      messages: {
        default: paramMessage`The discriminated union variant ${"variantKey"} of discriminated union ${"discriminatedUnionTypeName"} must be an object.`,
        anonymous: paramMessage`The discriminated union variant ${"variantTypeName"} of the inline/anonymous discriminated union must be an object.`,
      },
    },
    "discriminated-union-must-have-at-least-one-variant": {
      severity: "error",
      messages: {
        default: paramMessage`The discriminated union ${"discriminatedUnionTypeName"} must have at least one variant.`,
        anonymous: paramMessage`The inline/anonymous discriminated union must have at least one variant.`,
        model: paramMessage`The polymorphic model ${"modelTypeName"} must have at least one variant.`,
      },
    },
    "discriminated-union-variant-must-have-discriminator-property-defined": {
      severity: "error",
      messages: {
        default: paramMessage`The discriminated union variant ${"variantKey"} of discriminated union ${"discriminatedUnionTypeName"} without an envelope must have the discriminator property ${"discriminatorPropertyName"} defined as a string literal type.`,
        anonymous: paramMessage`The discriminated union variant ${"variantTypeName"} of an inline/anonymous discriminated union without an envelope must have the discriminator property ${"discriminatorPropertyName"} defined as a string literal type.`,
      },
    },
  },
  emitter: {
    options: BicepEmitterOptionsSchema,
  },
  state: {
    bicepResourceNamespace: {
      description: "State for the @bicepResourceNamespace decorator.",
    },
    bicepResourceType: {
      description: "State for the @bicepResourceType decorator.",
    },
    bicepResourceTypeOptions: {
      description: "State for the @bicepResourceTypeOptions decorator.",
    },
    bicepFallbackType: {
      description: "State for the @bicepFallbackType decorator.",
    },
    bicepConfigurationType: {
      description: "State for the @bicepConfigurationType decorator.",
    },
    bicepIdentifierProperty: {
      description: "State for the @bicepIdentifierProperty decorator.",
    },
    bicepNamespaceFunctionType: {
      description: "State for the @bicepNamespaceFunctionType decorator.",
    },
    bicepNamespaceFunctionParameterFlags: {
      description: "State for the @bicepNamespaceFunctionParameterFlags decorator.",
    }
  },
});

export const { reportDiagnostic, createDiagnostic, createStateSymbol } = $lib;
export const StateKeys = $lib.stateKeys;

import { DiagnosticTarget, DiscriminatedUnion, Model, Namespace, NoTarget, Program, Scalar, TypeKind } from "@typespec/compiler";
import { reportDiagnostic } from "./lib.js";

export function createTypeNotSupportedDiagnostic(program: Program, target: DiagnosticTarget | typeof NoTarget, typeName: string) {
  reportDiagnostic(program, {
    code: "type-not-supported",
    format: {
      value: typeName,
    },
    target: target,
  });
}

export function createFallbackTypeSingletonDiagnostic(program: Program, model: Model) {
  reportDiagnostic(program, {
    code: "fallback-type-must-be-singleton",
    messageId: "default",
    target: model,
  });
}

export function createFallbackTypeMustNotBeVersionedDiagnostic(program: Program, model: Model) {
  reportDiagnostic(program, {
    code: "fallback-type-must-not-be-versioned",
    messageId: "default",
    target: model,
  });
}

export function createConfigurationTypeSingletonDiagnostic(program: Program, model: Model) {
  reportDiagnostic(program, {
    code: "configuration-type-must-be-singleton",
    messageId: "default",
    target: model,
  });
}

export function createConfigurationTypeMustNotBeVersionedDiagnostic(program: Program, model: Model) {
  reportDiagnostic(program, {
    code: "configuration-type-must-not-be-versioned",
    messageId: "default",
    target: model,
  });
}

export function createTypeKindNotSupportedDiagnostic(program: Program, target: DiagnosticTarget | typeof NoTarget, typeKind: TypeKind) {
  reportDiagnostic(program, {
    code: "type-not-supported",
    messageId: "kind",
    format: {
      value: typeKind,
    },
    target: target,
  });
}

export function createOnlyStringIndexerAllowedDiagnostic(program: Program, target: DiagnosticTarget | typeof NoTarget) {
  reportDiagnostic(program, {
    code: "only-string-indexer-allowed",
    target: target,
  });
}

export function createModelMustBeInNamespaceDiagnostic(program: Program, model: Model) {
  reportDiagnostic(program, {
    code: "model-must-be-in-namespace",
    format: {
      model: model.name,
    },
    target: model,
  });
}

export function createNamespaceMustBeDecoratedDiagnostic(program: Program, ns: Namespace) {
  reportDiagnostic(program, {
    code: "namespace-must-be-decorated",
    format: {
      namespace: ns.name,
    },
    target: ns,
  });
}

export function createNamespaceMustBeVersionedDiagnostic(program: Program, ns: Namespace) {
  reportDiagnostic(program, {
    code: "namespace-must-be-versioned",
    format: {
      namespace: ns.name,
    },
    target: ns,
  });
}

export function createScalarRangeWasWidenedDiagnostic(program: Program, target: DiagnosticTarget | typeof NoTarget, intrinsicScalar: Scalar, closestMatch: string) {
  reportDiagnostic(program, {
    code: "scalar-range-was-widened",
    format: {
      intrinsicScalar: intrinsicScalar.name,
      closestMatch: closestMatch,
    },
    target: target,
  });
}

export function createScalarRangeWasNarrowedDiagnostic(program: Program, target: DiagnosticTarget | typeof NoTarget, intrinsicScalar: Scalar, closestMatch: string) {
  reportDiagnostic(program, {
    code: "scalar-range-was-narrowed",
    format: {
      intrinsicScalar: intrinsicScalar.name,
      closestMatch: closestMatch,
    },
    target: target,
  });
}

export function createDiscriminatedUnionVariantMustBeAnObjectDiagnostic(program: Program, discriminatedUnion: DiscriminatedUnion, variantKey: string) {
  if (discriminatedUnion.type.name) {
    reportDiagnostic(program, {
      code: "discriminated-union-variant-must-be-an-object",
      messageId: "default",
      format: {
        discriminatedUnionTypeName: discriminatedUnion.type.name,
        variantKey: variantKey,
      },
      target: discriminatedUnion.type,
    });
  } else {
    reportDiagnostic(program, {
      code: "discriminated-union-variant-must-be-an-object",
      messageId: "anonymous",
      format: {
        variantTypeName: variantKey,
      },
      target: discriminatedUnion.type,
    });
  }
}

export function createDiscriminatedUnionMustHaveAtLeastOneVariantDiagnostic(program: Program, discriminatedUnion: DiscriminatedUnion | Model) {
  if ("options" in discriminatedUnion) {
    if (discriminatedUnion.type.name) {
      reportDiagnostic(program, {
        code: "discriminated-union-must-have-at-least-one-variant",
        messageId: "default",
        format: {
          discriminatedUnionTypeName: discriminatedUnion.type.name,
        },
        target: discriminatedUnion.type,
      });
    } else {
      reportDiagnostic(program, {
        code: "discriminated-union-must-have-at-least-one-variant",
        messageId: "anonymous",
        format: {},
        target: discriminatedUnion.type,
      });
    }
  } else {
    reportDiagnostic(program, {
      code: "discriminated-union-must-have-at-least-one-variant",
      messageId: "model",
      format: {
        modelTypeName: discriminatedUnion.name,
      },
      target: discriminatedUnion,
    });
  }
}

export function createDiscriminatedUnionVariantMustHaveDiscriminatorPropertyDefinedDiagnostic(program: Program, discriminatedUnion: DiscriminatedUnion, variantKey: string, discriminatorPropertyName: string) {
  if (discriminatedUnion.type.name) {
    reportDiagnostic(program, {
      code: "discriminated-union-variant-must-have-discriminator-property-defined",
      messageId: "default",
      format: {
        discriminatedUnionTypeName: discriminatedUnion.type.name,
        variantKey: variantKey,
        discriminatorPropertyName: discriminatorPropertyName,
      },
      target: discriminatedUnion.type,
    });
  } else {
    reportDiagnostic(program, {
      code: "discriminated-union-variant-must-have-discriminator-property-defined",
      messageId: "anonymous",
      format: {
        variantTypeName: variantKey,
        discriminatorPropertyName: discriminatorPropertyName,
      },
      target: discriminatedUnion.type,
    });
  }
}

import {
  ArrayModelType,
  DiagnosticTarget,
  DiscriminatedUnion,
  Discriminator,
  getDiscriminatedUnion,
  getDiscriminatedUnionFromInheritance,
  getDiscriminator,
  getDoc,
  getTypeName,
  IntrinsicScalarName,
  IntrinsicType,
  isArrayModelType,
  Model,
  ModelProperty,
  NoTarget,
  Program,
  Scalar,
  Type,
  Union,
  walkPropertiesInherited,
} from "@typespec/compiler";
import { BicepSourceFileKind, NamespaceFunctionParameter, NamespaceFunctionParameterFlags, ObjectTypeProperty, ObjectTypePropertyFlags, TypeFactory, TypeReference } from "@azure/bicep-types";
import { BicepNamespaceFunctionParameterFlag, getBicepNamespaceFunctionParameterFlags, isBicepIdentifierProperty } from "./decorators.js";
import {
  createDiscriminatedUnionMustHaveAtLeastOneVariantDiagnostic,
  createDiscriminatedUnionVariantMustBeAnObjectDiagnostic,
  createDiscriminatedUnionVariantMustHaveDiscriminatorPropertyDefinedDiagnostic,
  createOnlyStringIndexerAllowedDiagnostic,
  createScalarRangeWasNarrowedDiagnostic,
  createScalarRangeWasWidenedDiagnostic,
  createTypeKindNotSupportedDiagnostic,
  createTypeNotSupportedDiagnostic,
} from "./diagnostics.js";

export function generateType(program: Program, typeFactory: TypeFactory, diagnosticTarget: DiagnosticTarget | typeof NoTarget, type: Type): TypeReference | undefined {
  switch (type.kind) {
    case "Intrinsic": {
      return generateTypeForIntrinsic(program, typeFactory, diagnosticTarget, type);
    }

    case "Boolean": {
      createTypeNotSupportedDiagnostic(program, diagnosticTarget, `${type.value}`);
      return undefined;
    }

    case "EnumMember": {
      if (typeof type.value === "number") {
        createTypeNotSupportedDiagnostic(program, diagnosticTarget, `${type.value}`);
        return undefined;
      }

      // use value if specified
      // otherwise use name as the value
      return typeFactory.addStringLiteralType(type.value ?? type.name);
    }

    case "Enum": {
      const bicepEnumMembers: TypeReference[] = [];
      for (const [, member] of type.members) {
        // put any diagnostics on the member
        const memberType = generateType(program, typeFactory, member, member);
        if (memberType === undefined) {
          // diagnostic should have already been reported
          return undefined;
        }

        bicepEnumMembers.push(memberType);
      }

      return typeFactory.addUnionType(bicepEnumMembers);
    }

    case "Model": {
      if (isArrayModelType(program, type)) {
        // put diagnostics on the array itself
        return generateTypeForArray(program, typeFactory, type, type);
      }

      const discriminator = getDiscriminator(program, type);
      if (discriminator === undefined) {
        return generateTypeForModel(program, typeFactory, type, type);
      }

      return generateTypeForDiscriminatedModel(program, typeFactory, type, type, discriminator);
    }

    case "Number": {
      // TODO: Can we get min and max values from the model?
      return typeFactory.addIntegerType();
    }

    case "String": {
      return typeFactory.addStringLiteralType(type.value);
    }

    case "Scalar": {
      return generateTypeForScalar(program, typeFactory, diagnosticTarget, type);
    }

    case "Union": {
      const [discriminatedUnion, diagnostics] = getDiscriminatedUnion(program, type);

      if (diagnostics.length > 0) {
        for (const diagnostic of diagnostics) {
          program.reportDiagnostic(diagnostic);
        }
        return undefined;
      }

      if (discriminatedUnion === undefined) {
        return generateTypeForUnion(program, typeFactory, type);
      }

      return generateTypeForDiscriminatedUnion(program, typeFactory, type, discriminatedUnion);
    }

    case "UnionVariant": {
      return generateType(program, typeFactory, type, type.type);
    }

    default: {
      createTypeKindNotSupportedDiagnostic(program, type, type.kind);
      return undefined;
    }
  }
}

function generateTypeForArray(program: Program, typeFactory: TypeFactory, diagnosticTarget: DiagnosticTarget | typeof NoTarget, array: ArrayModelType): TypeReference | undefined {
  const itemType = generateType(program, typeFactory, array.indexer.value, array.indexer.value);
  if (itemType === undefined) {
    // diagnostic should have already been reported
    return undefined;
  }

  return typeFactory.addArrayType(itemType);
}

function generateTypeForDiscriminatedUnion(program: Program, typeFactory: TypeFactory, union: Union, discriminatedUnion: DiscriminatedUnion) {
  const elements: Record<string, TypeReference> = {};

  if (discriminatedUnion.variants.size === 0) {
    createDiscriminatedUnionMustHaveAtLeastOneVariantDiagnostic(program, discriminatedUnion);
    return undefined;
  }

  for (const [key, variant] of discriminatedUnion.variants) {
    if (variant.kind !== "Model" || isArrayModelType(program, variant)) {
      createDiscriminatedUnionVariantMustBeAnObjectDiagnostic(program, discriminatedUnion, key);
      return undefined;
    }

    if (discriminatedUnion.options.envelope === "none") {
      // if this is a discriminated union serialized without an envelope,
      // we currently require the discriminator property to be defined
      // in every variant model with the correct string literal type
      // (we will inject it for object envelope scenarios automatically)
      const variantDiscriminatorProperty = variant.properties.get(discriminatedUnion.options.discriminatorPropertyName);

      if (variantDiscriminatorProperty === undefined || (variantDiscriminatorProperty.type.kind !== "String" && variantDiscriminatorProperty.type.kind !== "EnumMember")) {
        // TODO: We should be able to inject the missing discriminator property into the model
        // but it's not critical to support this scenario right now, so we will block it
        createDiscriminatedUnionVariantMustHaveDiscriminatorPropertyDefinedDiagnostic(program, discriminatedUnion, key, discriminatedUnion.options.discriminatorPropertyName);
        return undefined;
      }
    }

    const variantType = generateType(program, typeFactory, variant, variant);
    if (variantType === undefined) {
      // diagnostic should have already been reported
      return undefined;
    }

    switch (discriminatedUnion.options.envelope) {
      case "none": {
        // since there's no discriminator envelope, we don't need to wrap the variant type
        elements[key] = variantType;
        break;
      }

      case "object": {
        const envelopeType = typeFactory.addObjectType("envelope", {
          [discriminatedUnion.options.discriminatorPropertyName]: {
            flags: ObjectTypePropertyFlags.Required,
            type: typeFactory.addStringLiteralType(key),
          },
          [discriminatedUnion.options.envelopePropertyName]: {
            flags: ObjectTypePropertyFlags.Required,
            type: variantType,
          },
        });
        elements[key] = envelopeType;
        break;
      }

      default: {
        createTypeNotSupportedDiagnostic(program, union, union.name ?? "anonymous union");
        return undefined;
      }
    }
  }

  const discriminatorTypes: TypeReference[] = [];
  for (const variantKey of discriminatedUnion.variants.keys()) {
    discriminatorTypes.push(typeFactory.addStringLiteralType(variantKey));
  }

  if (discriminatorTypes.length === 1) {
    // we can simplify a discriminator with a single variant
    return Object.values(elements)[0];
  }

  // let's start with just the discriminator property in the base and leave everything else to the variants
  const baseProperties: Record<string, ObjectTypeProperty> = {};
  baseProperties[discriminatedUnion.options.discriminatorPropertyName] = {
    flags: ObjectTypePropertyFlags.Required,
    type: typeFactory.addUnionType(discriminatorTypes),
  };

  return typeFactory.addDiscriminatedObjectType(union.name ?? "anonymous union", discriminatedUnion.options.discriminatorPropertyName, baseProperties, elements);
}

function generateTypeForUnion(program: Program, typeFactory: TypeFactory, union: Union) {
  const bicepUnionMembers: TypeReference[] = [];
  for (const variant of union.variants.values()) {
    const variantType = generateType(program, typeFactory, variant, variant);
    if (variantType === undefined) {
      // diagnostic should have already been reported
      return undefined;
    }

    bicepUnionMembers.push(variantType);
  }

  return typeFactory.addUnionType(bicepUnionMembers);
}

function generateTypeForDiscriminatedModel(program: Program, typeFactory: TypeFactory, diagnosticTarget: DiagnosticTarget | typeof NoTarget, model: Model, discriminator: Discriminator) {
  const [discriminatedUnionFromInheritance, diagnostics] = getDiscriminatedUnionFromInheritance(model, discriminator);
  if (discriminatedUnionFromInheritance === undefined) {
    for (const diagnostic of diagnostics) {
      program.reportDiagnostic(diagnostic);
    }
    return undefined;
  }

  if (discriminatedUnionFromInheritance.variants.size === 0) {
    createDiscriminatedUnionMustHaveAtLeastOneVariantDiagnostic(program, model);
    return undefined;
  }

  const elements: Record<string, TypeReference> = {};

  // TODO: Simplify discriminated union generation when there's only one variant
  // this will produce a better experience in Bicep

  for (const [key, variant] of discriminatedUnionFromInheritance.variants) {
    const variantType = generateType(program, typeFactory, variant, variant);
    if (variantType === undefined) {
      // diagnostic should have already been reported
      return undefined;
    }

    elements[key] = variantType;
  }

  const baseProperties = generateModelProperties(program, typeFactory, model);
  if (baseProperties === undefined) {
    // diagnostic should have already been reported
    return undefined;
  }

  return typeFactory.addDiscriminatedObjectType(getBicepTypeName(model), discriminatedUnionFromInheritance.propertyName, baseProperties, elements);
}

function generateTypeForModel(program: Program, typeFactory: TypeFactory, diagnosticTarget: DiagnosticTarget | typeof NoTarget, model: Model) {
  const bicepProperties = generateModelProperties(program, typeFactory, model);
  if (bicepProperties === undefined) {
    // diagnostic should have already been reported
    return undefined;
  }

  if (model.indexer !== undefined) {
    // TODO: Handle namespaces
    if (model.indexer.key.name !== "string") {
      createOnlyStringIndexerAllowedDiagnostic(program, model);
      return undefined;
    }

    const additionalPropertiesType = generateType(program, typeFactory, model.indexer.value, model.indexer.value);

    if (additionalPropertiesType === undefined) {
      // diagnostic should have already been reported
      return undefined;
    }

    return typeFactory.addObjectType(getBicepTypeName(model), bicepProperties, additionalPropertiesType);
  }

  return typeFactory.addObjectType(getBicepTypeName(model), bicepProperties);
}

function generateModelProperties(program: Program, typeFactory: TypeFactory, model: Model): Record<string, ObjectTypeProperty> | undefined {
  const bicepProperties: Record<string, ObjectTypeProperty> = {};
  for (const property of walkPropertiesInherited(model)) {
    // put diagnostics on the property
    const bicepPropertyType = generateType(program, typeFactory, property, property.type);
    if (bicepPropertyType === undefined) {
      // diagnostic should have already been reported
      return undefined;
    }

    bicepProperties[property.name] = {
      type: bicepPropertyType,
      flags: getObjectPropertyFlags(program, property),
      // get the description from either the doc comment or the @doc decorator
      description: getDoc(program, property),
    };
  }
  return bicepProperties;
}

function getObjectPropertyFlags(program: Program, property: ModelProperty) {
  let flags = ObjectTypePropertyFlags.None;

  if (!property.optional) {
    flags |= ObjectTypePropertyFlags.Required;
  }

  if (isBicepIdentifierProperty(program, property)) {
    flags |= ObjectTypePropertyFlags.Identifier;
  }

  return flags;
}

function generateTypeForIntrinsic(program: Program, typeFactory: TypeFactory, diagnosticTarget: DiagnosticTarget | typeof NoTarget, type: IntrinsicType) {
  switch (type.name) {
    case "null":
      return typeFactory.addNullType();

    case "unknown":
      return typeFactory.addAnyType();

    default:
      createTypeNotSupportedDiagnostic(program, diagnosticTarget, type.name);
      return undefined;
  }
}

type IntrinsicScalar = Scalar & { name: IntrinsicScalarName };

const intrinsicScalarNames: string[] = [
  "bytes",
  "numeric",
  "integer",
  "float",
  "int64",
  "int32",
  "int16",
  "int8",
  "uint64",
  "uint32",
  "uint16",
  "uint8",
  "safeint",
  "float32",
  "float64",
  "decimal",
  "decimal128",
  "string",
  "plainDate",
  "plainTime",
  "utcDateTime",
  "offsetDateTime",
  "duration",
  "boolean",
  "url",
];

function isIntrinsicScalar(scalar: Scalar): scalar is IntrinsicScalar {
  return typeof scalar.name === "string" && intrinsicScalarNames.includes(scalar.name);
}

function generateTypeForScalar(program: Program, typeFactory: TypeFactory, diagnosticTarget: DiagnosticTarget | typeof NoTarget, scalar: Scalar): TypeReference | undefined {
  // TODO: Handle namespaces
  if (isIntrinsicScalar(scalar)) {
    return generateTypeForIntrinsicScalar(program, typeFactory, scalar);
  }

  if (scalar.baseScalar) {
    // this is a base scalar type, we can generate it directly
    return generateTypeForScalar(program, typeFactory, diagnosticTarget, scalar.baseScalar);
  }

  createTypeNotSupportedDiagnostic(program, scalar, scalar.name);
  return undefined;
}

function generateTypeForIntrinsicScalar(program: Program, typeFactory: TypeFactory, scalar: IntrinsicScalar): TypeReference | undefined {
  switch (scalar.name) {
    case "boolean":
      return typeFactory.addBooleanType();

    case "string":
      return typeFactory.addStringType();

    case "url":
    case "plainDate":
    case "plainTime":
    case "utcDateTime":
    case "offsetDateTime":
    case "duration":
      createScalarRangeWasWidenedDiagnostic(program, scalar, scalar, "string");
      return typeFactory.addStringType();

    case "int64":
      // despite the method signature,
      // Bicep types only support signed 64-bit integers at this point (c# long type)
      // as a result, we don't have to specify min and max constraints
      return typeFactory.addIntegerType();

    case "int32":
      return typeFactory.addIntegerType(-2_147_483_648, 2_147_483_647);

    case "int16":
      return typeFactory.addIntegerType(-32_768, 32_767);

    case "int8":
      return typeFactory.addIntegerType(-128, 127);

    case "safeint":
      return typeFactory.addIntegerType(-9_007_199_254_740_991, 9_007_199_254_740_991);

    case "numeric":
    case "integer":
      createScalarRangeWasNarrowedDiagnostic(program, scalar, scalar, "int64");
      return typeFactory.addIntegerType();

    case "uint64":
      createScalarRangeWasNarrowedDiagnostic(program, scalar, scalar, "int64");
      return typeFactory.addIntegerType(0, undefined);

    case "uint32":
      return typeFactory.addIntegerType(0, 4_294_967_295);

    case "uint16":
      return typeFactory.addIntegerType(0, 65_535);

    case "uint8":
      return typeFactory.addIntegerType(0, 255);

    case "float":
    case "float32":
    case "float64":
    case "decimal":
    case "decimal128":
    case "bytes":
    default:
      createTypeNotSupportedDiagnostic(program, scalar, scalar.name);
      return undefined;
  }
}

function getBicepTypeName(type: Type): string {
  return getTypeName(type, { nameOnly: true });
}

const namespaceFunctionParameterFlagMap: Record<BicepNamespaceFunctionParameterFlag, NamespaceFunctionParameterFlags> = {
  CompileTimeConstant: NamespaceFunctionParameterFlags.CompileTimeConstant,
  DeployTimeConstant: NamespaceFunctionParameterFlags.DeployTimeConstant,
};

export function generateNamespaceFunctionType(program: Program, typeFactory: TypeFactory, model: Model): TypeReference | undefined {
  const name = model.name;
  const description = getDoc(program, model);

  // Extract parameters from the "parameters" property's model type
  const parametersProperty = model.properties.get("parameters");
  const namespaceFunctionParams: NamespaceFunctionParameter[] = [];

  if (parametersProperty && parametersProperty.type.kind === "Model") {
    const parametersModel = parametersProperty.type;
    for (const [propName, prop] of parametersModel.properties) {
      const paramType = generateType(program, typeFactory, prop, prop.type);
      if (paramType === undefined) {
        return undefined;
      }

      let flags: NamespaceFunctionParameterFlags = NamespaceFunctionParameterFlags.None;
      if (!prop.optional) {
        flags |= NamespaceFunctionParameterFlags.Required;
      }

      const decoratorFlags = getBicepNamespaceFunctionParameterFlags(program, prop);
      if (decoratorFlags) {
        for (const f of decoratorFlags) {
          flags |= namespaceFunctionParameterFlagMap[f];
        }
      }

      namespaceFunctionParams.push({
        name: propName,
        type: paramType,
        description: getDoc(program, prop),
        flags,
      });
    }
  }

  // Extract output type from the "outputType" property's type
  const outputTypeProperty = model.properties.get("outputType");
  if (outputTypeProperty === undefined) {
    return undefined;
  }

  const outputType = generateType(program, typeFactory, outputTypeProperty, outputTypeProperty.type);
  if (outputType === undefined) {
    return undefined;
  }

  const evalType = model.properties.get("evaluatedLanguageExpression")?.type;
  const evaluatedLanguageExpression = evalType?.kind === "String" ? evalType.value : undefined;

  const fileKindType = model.properties.get("visibleInFileKind")?.type;
  const visibleInFileKind = fileKindType?.kind === "EnumMember" ? BicepSourceFileKind[fileKindType.name as keyof typeof BicepSourceFileKind] : undefined;

  return typeFactory.addNamespaceFunctionType(
    name,
    namespaceFunctionParams,
    outputType,
    description,
    evaluatedLanguageExpression,
    visibleInFileKind,
  );
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export enum BuiltInTypeKind {
  Any = 1,
  Null = 2,
  Bool = 3,
  Int = 4,
  String = 5,
  Object = 6,
  Array = 7,
  ResourceRef = 8,
}

const BuiltInTypeKindLabel = new Map<BuiltInTypeKind, string>([
  [BuiltInTypeKind.Any, 'Any'],
  [BuiltInTypeKind.Null, 'Null'],
  [BuiltInTypeKind.Bool, 'Bool'],
  [BuiltInTypeKind.Int, 'Int'],
  [BuiltInTypeKind.String, 'String'],
  [BuiltInTypeKind.Object, 'Object'],
  [BuiltInTypeKind.Array, 'Array'],
  [BuiltInTypeKind.ResourceRef, 'ResourceRef'],
]);

export function getBuiltInTypeKindLabel(input: BuiltInTypeKind) {
  return BuiltInTypeKindLabel.get(input) ?? '';
}

export enum ScopeType {
  None = 0,
  Tenant = 1 << 0,
  ManagementGroup = 1 << 1,
  Subscription = 1 << 2,
  ResourceGroup = 1 << 3,
  Extension = 1 << 4,
  DesiredStateConfiguration = 1 << 5,
}

export const AllExceptExtension = ScopeType.Tenant | ScopeType.ManagementGroup | ScopeType.Subscription | ScopeType.ResourceGroup;
export const All = AllExceptExtension | ScopeType.Extension;

const ScopeTypeLabel = new Map<ScopeType, string>([
  [ScopeType.Tenant, 'Tenant'],
  [ScopeType.ManagementGroup, 'ManagementGroup'],
  [ScopeType.Subscription, 'Subscription'],
  [ScopeType.ResourceGroup, 'ResourceGroup'],
  [ScopeType.Extension, 'Extension'],
  [ScopeType.DesiredStateConfiguration, 'DesiredStateConfiguration'],
]);

export function getScopeTypeLabels(input: ScopeType, ...scopeLabels: [ScopeType | undefined, string][]) {
  const types = [];
  for (const [key, value] of ScopeTypeLabel) {
    if ((key & input) === key) {
      const labels = [];
      for (const [labeledScopes, label] of scopeLabels) {
        if (labeledScopes !== undefined && (key & labeledScopes) === key) {
          labels.push(label);
        }
      }
      types.push(`${value}${labels.length > 0 ? ` (${labels.join(', ')})` : ''}`);
    }
  }

  return types
}

export enum ObjectTypePropertyFlags {
  None = 0,
  Required = 1 << 0,
  ReadOnly = 1 << 1,
  WriteOnly = 1 << 2,
  DeployTimeConstant = 1 << 3,
  Identifier = 1 << 4
}

const ObjectTypePropertyFlagsLabel = new Map<ObjectTypePropertyFlags, string>([
  [ObjectTypePropertyFlags.Required, 'Required'],
  [ObjectTypePropertyFlags.ReadOnly, 'ReadOnly'],
  [ObjectTypePropertyFlags.WriteOnly, 'WriteOnly'],
  [ObjectTypePropertyFlags.DeployTimeConstant, 'DeployTimeConstant'],
  [ObjectTypePropertyFlags.Identifier, 'Identifier'],
]);

export function getObjectTypePropertyFlagsLabels(input: ObjectTypePropertyFlags) {
  const types = [];
  for (const [key, value] of ObjectTypePropertyFlagsLabel) {
    if ((key & input) === key) {
      types.push(value);
    }
  }

  return types;
}

export enum NamespaceFunctionParameterFlags {
  None = 0,
  Required = 1 << 0,
  CompileTimeConstant = 1 << 1,
  DeployTimeConstant = 1 << 2,
}

const NamespaceFunctionParameterFlagsLabel = new Map<NamespaceFunctionParameterFlags, string>([
  [NamespaceFunctionParameterFlags.Required, 'Required'],
  [NamespaceFunctionParameterFlags.CompileTimeConstant, 'CompileTimeConstant'],
  [NamespaceFunctionParameterFlags.DeployTimeConstant, 'DeployTimeConstant'],
]);

export function getNamespaceFunctionParameterFlagsLabels(input: NamespaceFunctionParameterFlags) {
  const types = [];
  for (const [key, value] of NamespaceFunctionParameterFlagsLabel) {
    if ((key & input) === key) {
      types.push(value);
    }
  }

  return types;
}

export enum BicepSourceFileKind {
  BicepFile = 1,
  ParamsFile = 2,
}

export enum TypeBaseKind {
  BuiltInType = 'BuiltInType',
  ObjectType = 'ObjectType',
  ArrayType = 'ArrayType',
  ResourceType = 'ResourceType',
  UnionType = 'UnionType',
  StringLiteralType = 'StringLiteralType',
  DiscriminatedObjectType = 'DiscriminatedObjectType',
  ResourceFunctionType = 'ResourceFunctionType',
  AnyType = 'AnyType',
  NullType = 'NullType',
  BooleanType = 'BooleanType',
  IntegerType = 'IntegerType',
  StringType = 'StringType',
  FunctionType = 'FunctionType',
  NamespaceFunctionType = 'NamespaceFunctionType',
}

export function getTypeBaseKindLabel(input: TypeBaseKind): string {
  return input;
}

export class TypeReference {
  constructor(public readonly index: number) {}
}

export class CrossFileTypeReference {
  constructor(
    public readonly relativePath: string,
    public readonly index: number) {}
}

type TypeBase<T extends TypeBaseKind, U extends object = Record<string, unknown>> = { type: T } & U

export type BuiltInType = TypeBase<TypeBaseKind.BuiltInType, {
  kind: BuiltInTypeKind;
}>

export type UnionType = TypeBase<TypeBaseKind.UnionType, {
  elements: TypeReference[];
}>

export type StringLiteralType = TypeBase<TypeBaseKind.StringLiteralType, {
  value: string;
}>

export type ResourceType = TypeBase<TypeBaseKind.ResourceType, {
  name: string;
  body: TypeReference;
  functions?: Record<string, ResourceTypeFunction>;
  readableScopes: ScopeType;
  writableScopes: ScopeType;
}>

export type ResourceFunctionType = TypeBase<TypeBaseKind.ResourceFunctionType, {
  name: string;
  resourceType: string;
  apiVersion: string;
  output: TypeReference;
  input?: TypeReference;
}>

export type ObjectType = TypeBase<TypeBaseKind.ObjectType, {
  name: string;
  properties: Record<string, ObjectTypeProperty>;
  additionalProperties?: TypeReference;
  sensitive?: boolean;
}>

export type FunctionParameter = {
  name: string;
  type: TypeReference;
  description?: string;
}

export type FunctionType = TypeBase<TypeBaseKind.FunctionType, {
  parameters: FunctionParameter[];
  output: TypeReference;
}>

export type NamespaceFunctionParameter = {
  name: string;
  type: TypeReference;
  description?: string;
  flags: NamespaceFunctionParameterFlags;
}

export type NamespaceFunctionType = TypeBase<TypeBaseKind.NamespaceFunctionType, {
  name: string;
  description?: string;
  evaluatedLanguageExpression?: string;
  parameters: NamespaceFunctionParameter[];
  outputType: TypeReference;
  visibleInFileKind?: BicepSourceFileKind;
}>

export type DiscriminatedObjectType = TypeBase<TypeBaseKind.DiscriminatedObjectType, {
  name: string;
  discriminator: string;
  baseProperties: Record<string, ObjectTypeProperty>;
  elements: Record<string, TypeReference>;
}>

export type ArrayType = TypeBase<TypeBaseKind.ArrayType, {
  itemType: TypeReference;
  minLength?: number;
  maxLength?: number;
}>

export type AnyType = TypeBase<TypeBaseKind.AnyType>

export type NullType = TypeBase<TypeBaseKind.NullType>

export type BooleanType = TypeBase<TypeBaseKind.BooleanType>

export type IntegerType = TypeBase<TypeBaseKind.IntegerType, {
  minValue?: number;
  maxValue?: number;
}>

export type StringType = TypeBase<TypeBaseKind.StringType, {
  sensitive?: boolean;
  minLength?: number;
  maxLength?: number;
  pattern?: string;
}>

export type BicepType = BuiltInType |
  UnionType |
  StringType |
  StringLiteralType |
  IntegerType |
  BooleanType |
  NullType |
  AnyType |
  ResourceType |
  ResourceFunctionType |
  ObjectType |
  DiscriminatedObjectType |
  ArrayType |
  FunctionType |
  NamespaceFunctionType;

export type ObjectTypeProperty = {
  type: TypeReference;
  flags: ObjectTypePropertyFlags;
  description?: string;
}

export type ResourceTypeFunction = {
  type: TypeReference;
  description?: string;
}

export class TypeFactory {
  types: BicepType[];
  private readonly typeToTypeReference: Map<BicepType, TypeReference> = new Map();
  private readonly stringTypeCache: Map<string, TypeReference> = new Map();
  private readonly integerTypeCache: Map<string, TypeReference> = new Map();
  private readonly anyType: AnyType = {type: TypeBaseKind.AnyType};
  private readonly nullType: NullType = {type: TypeBaseKind.NullType};
  private readonly booleanType: BooleanType = {type: TypeBaseKind.BooleanType};

  constructor() {
    this.types = [];
  }

  public addType(type: BicepType): TypeReference {
    const preexisting = this.typeToTypeReference.get(type);
    if (preexisting !== undefined)
    {
      return preexisting;
    }

    const index = this.types.length;
    const reference = new TypeReference(index);
    this.types[index] = type;
    this.typeToTypeReference.set(type, reference);

    return reference;
  }

  public lookupType(reference: TypeReference): BicepType {
    return this.types[reference.index];
  }

  public addUnionType(elements: TypeReference[]) {
    return this.addType({
      type: TypeBaseKind.UnionType,
      elements: elements,
    });
  }

  public addStringLiteralType(value: string) {
    return this.addType({
      type: TypeBaseKind.StringLiteralType,
      value: value,
    });
  }

  public addStringType(sensitive?: true, minLength?: number, maxLength?: number, pattern?: string): TypeReference {
    const cacheKey = `secure:${sensitive}|minLength:${minLength}|maxLength:${maxLength}|pattern:${pattern}`;
    const preexisting = this.stringTypeCache.get(cacheKey);
    if (preexisting !== undefined) {
      return preexisting;
    }

    const added = this.addType({
      type: TypeBaseKind.StringType,
      sensitive: sensitive,
      minLength: minLength,
      maxLength: maxLength,
      pattern: pattern,
    });
    this.stringTypeCache.set(cacheKey, added);
    return added;
  }

  public addIntegerType(minValue?: number, maxValue?: number): TypeReference {
    const cacheKey = `minValue:${minValue}|maxValue:${maxValue}`;
    const preexisting = this.integerTypeCache.get(cacheKey);
    if (preexisting !== undefined)
    {
      return preexisting;
    }

    const added = this.addType({
      type: TypeBaseKind.IntegerType,
      minValue: minValue,
      maxValue: maxValue,
    });
    this.integerTypeCache.set(cacheKey, added);
    return added;
  }

  public addAnyType(): TypeReference {
    return this.addType(this.anyType);
  }

  public addNullType(): TypeReference {
    return this.addType(this.nullType);
  }

  public addBooleanType(): TypeReference {
    return this.addType(this.booleanType);
  }

  public addResourceType(
    name: string,
    body: TypeReference,
    readableScopes: ScopeType,
    writableScopes: ScopeType,
    functions?: Record<string, ResourceTypeFunction>,
  ): TypeReference {
    const resource: ResourceType = {
      type: TypeBaseKind.ResourceType,
      name,
      body,
      readableScopes,
      writableScopes,
      functions,
    };

    return this.addType(resource);
  }

  public addUnscopedResourceType(
    name: string,
    body: TypeReference,
    readable: boolean = true,
    writable: boolean = true,
    functions?: Record<string, ResourceTypeFunction>,
  ): TypeReference {
  const readableScopes = readable ? All : ScopeType.None;
  const writableScopes = writable ? All : ScopeType.None;

    const resource: ResourceType = {
      type: TypeBaseKind.ResourceType,
      name,
      body,
      readableScopes,
      writableScopes,
      functions,
    };

    return this.addType(resource);
  }

  public addResourceFunctionType(name: string, resourceType: string, apiVersion: string, output: TypeReference, input?: TypeReference) {
    return this.addType({
      type: TypeBaseKind.ResourceFunctionType,
      name: name,
      resourceType: resourceType,
      apiVersion: apiVersion,
      output: output,
      input: input,
    });
  }

  public addObjectType(name: string, properties: Record<string, ObjectTypeProperty>, additionalProperties?: TypeReference, sensitive?: boolean) {
    return this.addType({
      type: TypeBaseKind.ObjectType,
      name: name,
      properties: properties,
      additionalProperties: additionalProperties,
      sensitive: sensitive,
    });
  }

  public addDiscriminatedObjectType(name: string, discriminator: string, baseProperties: Record<string, ObjectTypeProperty>, elements: Record<string, TypeReference>) {
    return this.addType({
      type: TypeBaseKind.DiscriminatedObjectType,
      name: name,
      discriminator: discriminator,
      baseProperties: baseProperties,
      elements: elements,
    });
  }

  public addArrayType(itemType: TypeReference, minLength?: number, maxLength?: number) {
    return this.addType({
      type: TypeBaseKind.ArrayType,
      itemType: itemType,
      minLength: minLength,
      maxLength: maxLength,
    });
  }

  public addFunctionType(parameters: FunctionParameter[], output: TypeReference) {
    return this.addType({
      type: TypeBaseKind.FunctionType,
      parameters,
      output,
    });
  }

  public addNamespaceFunctionType(
    name: string,
    parameters: NamespaceFunctionParameter[],
    outputType: TypeReference,
    description?: string,
    evaluatedLanguageExpression?: string,
    visibleInFileKind?: BicepSourceFileKind,
  ) {
    return this.addType({
      type: TypeBaseKind.NamespaceFunctionType,
      name,
      description,
      evaluatedLanguageExpression,
      parameters,
      outputType,
      visibleInFileKind,
    });
  }
}

export interface TypeIndex {
  resources: Record<string, CrossFileTypeReference>;
  resourceFunctions: Record<string, Record<string, CrossFileTypeReference[]>>;
  namespaceFunctions: CrossFileTypeReference[];
  settings?: TypeSettings;
  fallbackResourceType?: CrossFileTypeReference;
}

export interface TypeFile {
  relativePath: string;
  types: BicepType[];
}

export interface TypeSettings {
  name: string;
  version: string;
  isSingleton: boolean;
  isPreview?: boolean;
  isDeprecated?: boolean;
  configurationType?: CrossFileTypeReference;
}

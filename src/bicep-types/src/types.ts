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
  Unknown = 0,
  Tenant = 1 << 0,
  ManagementGroup = 1 << 1,
  Subscription = 1 << 2,
  ResourceGroup = 1 << 3,
  Extension = 1 << 4,
}

const ScopeTypeLabel = new Map<ScopeType, string>([
  [ScopeType.Tenant, 'Tenant'],
  [ScopeType.ManagementGroup, 'ManagementGroup'],
  [ScopeType.Subscription, 'Subscription'],
  [ScopeType.ResourceGroup, 'ResourceGroup'],
  [ScopeType.Extension, 'Extension'],
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
}

export function getTypeBaseKindLabel(input: TypeBaseKind): string {
  return input;
}

export enum ResourceFlags {
  None = 0,
  ReadOnly = 1 << 0,
}

const ResourceFlagsLabels = new Map<ResourceFlags, string>([
  [ResourceFlags.ReadOnly, 'ReadOnly'],
]);

export function getResourceFlagsLabels(input: ResourceFlags) {
  const flags = [];
  for (const [bitmask, label] of ResourceFlagsLabels) {
    if ((bitmask & input) === bitmask) {
      flags.push(label);
    }
  }

  return flags;
}

export type TypeReference = number

type TypeBase<T extends TypeBaseKind, U extends object = Record<string, unknown>> = { Type: T } & U

export type BuiltInType = TypeBase<TypeBaseKind.BuiltInType, {
  Kind: BuiltInTypeKind;
}>

export type UnionType = TypeBase<TypeBaseKind.UnionType, {
  Elements: TypeReference[];
}>

export type StringLiteralType = TypeBase<TypeBaseKind.StringLiteralType, {
  Value: string;
}>

export type ResourceType = TypeBase<TypeBaseKind.ResourceType, {
  Name: string;
  ScopeType: ScopeType;
  ReadOnlyScopes?: ScopeType;
  Body: TypeReference;
  Flags: ResourceFlags;
}>

export type ResourceFunctionType = TypeBase<TypeBaseKind.ResourceFunctionType, {
  Name: string;
  ResourceType: string;
  ApiVersion: string;
  Output: TypeReference;
  Input?: TypeReference;
}>

export type ObjectType = TypeBase<TypeBaseKind.ObjectType, {
  Name: string;
  Properties: Record<string, ObjectTypeProperty>;
  AdditionalProperties?: TypeReference;
  Sensitive?: boolean;
}>

export type DiscriminatedObjectType = TypeBase<TypeBaseKind.DiscriminatedObjectType, {
  Name: string;
  Discriminator: string;
  BaseProperties: Record<string, ObjectTypeProperty>;
  Elements: Record<string, TypeReference>;
}>

export type ArrayType = TypeBase<TypeBaseKind.ArrayType, {
  ItemType: TypeReference;
  MinLength?: number;
  MaxLength?: number;
}>

export type AnyType = TypeBase<TypeBaseKind.AnyType>

export type NullType = TypeBase<TypeBaseKind.NullType>

export type BooleanType = TypeBase<TypeBaseKind.BooleanType>

export type IntegerType = TypeBase<TypeBaseKind.IntegerType, {
  MinValue?: number;
  MaxValue?: number;
}>

export type StringType = TypeBase<TypeBaseKind.StringType, {
  Sensitive?: boolean;
  MinLength?: number;
  MaxLength?: number;
  Pattern?: string;
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
  ArrayType

export type ObjectTypeProperty = {
  Type: TypeReference;
  Flags: ObjectTypePropertyFlags;
  Description?: string;
}

export class TypeFactory {
  types: BicepType[];
  private readonly typeToTypeReference: Map<BicepType, TypeReference> = new Map();
  private readonly stringTypeCache: Map<string, TypeReference> = new Map();
  private readonly integerTypeCache: Map<string, TypeReference> = new Map();
  private readonly anyType: AnyType = {Type: TypeBaseKind.AnyType};
  private readonly nullType: NullType = {Type: TypeBaseKind.NullType};
  private readonly booleanType: BooleanType = {Type: TypeBaseKind.BooleanType};

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
    this.types[index] = type;
    this.typeToTypeReference.set(type, index);

    return index;
  }

  public lookupType(reference: TypeReference): BicepType {
    return this.types[reference];
  }

  public addUnionType(elements: TypeReference[]) {
    return this.addType({
      Type: TypeBaseKind.UnionType,
      Elements: elements,
    });
  }

  public addStringLiteralType(value: string) {
    return this.addType({
      Type: TypeBaseKind.StringLiteralType,
      Value: value,
    });
  }

  public addStringType(sensitive?: true, minLength?: number, maxLength?: number, pattern?: string): TypeReference {
    const cacheKey = `secure:${sensitive}|minLength:${minLength}|maxLength:${maxLength}|pattern:${pattern}`;
    const preexisting = this.stringTypeCache.get(cacheKey);
    if (preexisting !== undefined) {
      return preexisting;
    }

    const added = this.addType({
      Type: TypeBaseKind.StringType,
      Sensitive: sensitive,
      MinLength: minLength,
      MaxLength: maxLength,
      Pattern: pattern,
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
      Type: TypeBaseKind.IntegerType,
      MinValue: minValue,
      MaxValue: maxValue,
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

  public addResourceType(name: string, scopeType: ScopeType, readOnlyScopes: ScopeType | undefined, body: TypeReference, flags: ResourceFlags) {
    return this.addType({
      Type: TypeBaseKind.ResourceType,
      Name: name,
      ScopeType: scopeType,
      ReadOnlyScopes: readOnlyScopes,
      Body: body,
      Flags: flags,
    });
  }

  public addResourceFunctionType(name: string, resourceType: string, apiVersion: string, output: TypeReference, input?: TypeReference) {
    return this.addType({
      Type: TypeBaseKind.ResourceFunctionType,
      Name: name,
      ResourceType: resourceType,
      ApiVersion: apiVersion,
      Output: output,
      Input: input,
    });
  }

  public addObjectType(name: string, properties: Record<string, ObjectTypeProperty>, additionalProperties?: TypeReference, sensitive?: boolean) {
    return this.addType({
      Type: TypeBaseKind.ObjectType,
      Name: name,
      Properties: properties,
      AdditionalProperties: additionalProperties,
      Sensitive: sensitive,
    });
  }

  public addDiscriminatedObjectType(name: string, discriminator: string, baseProperties: Record<string, ObjectTypeProperty>, elements: Record<string, TypeReference>) {
    return this.addType({
      Type: TypeBaseKind.DiscriminatedObjectType,
      Name: name,
      Discriminator: discriminator,
      BaseProperties: baseProperties,
      Elements: elements,
    });
  }

  public addArrayType(itemType: TypeReference, minLength?: number, maxLength?: number) {
    return this.addType({
      Type: TypeBaseKind.ArrayType,
      ItemType: itemType,
      MinLength: minLength,
      MaxLength: maxLength,
    });
  }
}

export interface TypeIndex {
  Resources: Record<string, TypeLocation>;
  Functions: Record<string, Record<string, TypeLocation[]>>;
  Settings?: TypeSettings;
  FallbackResourceType?: TypeLocation;
}

export interface TypeLocation {
  RelativePath: string;
  Index: number;
}

export interface TypeFile {
  relativePath: string;
  types: BicepType[];
}

export interface TypeSettings {
  Name: string;
  Version: string;
  IsSingleton: boolean;
  ConfigurationType?: TypeLocation;
}

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
  BuiltInType = 1,
  ObjectType = 2,
  ArrayType = 3,
  ResourceType = 4,
  UnionType = 5,
  StringLiteralType = 6,
  DiscriminatedObjectType = 7,
  ResourceFunctionType = 8,
}

const TypeBaseKindLabel = new Map<TypeBaseKind, string>([
  [TypeBaseKind.BuiltInType, 'BuiltInType'],
  [TypeBaseKind.ObjectType, 'ObjectType'],
  [TypeBaseKind.ArrayType, 'ArrayType'],
  [TypeBaseKind.ResourceType, 'ResourceType'],
  [TypeBaseKind.UnionType, 'UnionType'],
  [TypeBaseKind.StringLiteralType, 'StringLiteralType'],
  [TypeBaseKind.DiscriminatedObjectType, 'DiscriminatedObjectType'],
]);

export function getTypeBaseKindLabel(input: TypeBaseKind) {
  return TypeBaseKindLabel.get(input) ?? '';
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

type TypeBase<T extends TypeBaseKind, U extends object> = { Type: T } & U

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
  Output?: TypeReference;
  Input?: TypeReference;
}>

export type ObjectType = TypeBase<TypeBaseKind.ObjectType, {
  Name: string;
  Properties: Record<string, ObjectTypeProperty>;
  AdditionalProperties?: TypeReference;
}>

export type DiscriminatedObjectType = TypeBase<TypeBaseKind.DiscriminatedObjectType, {
  Name: string;
  Discriminator: string;
  BaseProperties: Record<string, ObjectTypeProperty>;
  Elements: Record<string, TypeReference>;
}>

export type ArrayType = TypeBase<TypeBaseKind.ArrayType, {
  ItemType: TypeReference;
}>

export type BicepType = BuiltInType | UnionType | StringLiteralType | ResourceType | ResourceFunctionType | ObjectType | DiscriminatedObjectType | ArrayType

export type ObjectTypeProperty = {
  Type: TypeReference;
  Flags: ObjectTypePropertyFlags;
  Description?: string;
}

export class TypeFactory {
  types: BicepType[];
  builtInTypes: Record<BuiltInTypeKind, TypeReference>;

  constructor() {
    this.types = [];
    this.builtInTypes = {
      [BuiltInTypeKind.Any]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.Any }),
      [BuiltInTypeKind.Null]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.Null }),
      [BuiltInTypeKind.Bool]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.Bool }),
      [BuiltInTypeKind.Int]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.Int }),
      [BuiltInTypeKind.String]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.String }),
      [BuiltInTypeKind.Object]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.Object }),
      [BuiltInTypeKind.Array]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.Array }),
      [BuiltInTypeKind.ResourceRef]: this.addType({ Type: TypeBaseKind.BuiltInType, Kind: BuiltInTypeKind.ResourceRef }),
    };
  }

  public addType(type: BicepType): TypeReference {
    const index = this.types.length;
    this.types[index] = type;

    return index;
  }

  public lookupType(reference: TypeReference): BicepType {
    return this.types[reference];
  }

  public lookupBuiltInType(kind: BuiltInTypeKind): TypeReference {
    return this.builtInTypes[kind];
  }

  public addBuiltInType(kind: BuiltInTypeKind) {
    return this.addType({
      Type: TypeBaseKind.BuiltInType,
      Kind: kind,
    });
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

  public addResourceFunctionType(name: string, resourceType: string, apiVersion: string, output?: TypeReference, input?: TypeReference) {
    return this.addType({
      Type: TypeBaseKind.ResourceFunctionType,
      Name: name,
      ResourceType: resourceType,
      ApiVersion: apiVersion,
      Output: output,
      Input: input,
    });
  }

  public addObjectType(name: string, properties: Record<string, ObjectTypeProperty>, additionalProperties?: TypeReference) {
    return this.addType({
      Type: TypeBaseKind.ObjectType,
      Name: name,
      Properties: properties,
      AdditionalProperties: additionalProperties,
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

  public addArrayType(itemType: TypeReference) {
    return this.addType({
      Type: TypeBaseKind.ArrayType,
      ItemType: itemType,
    });
  }
}

export interface TypeIndex {
  Resources: Record<string, TypeIndexEntry>;
  Functions: Record<string, Record<string, TypeIndexEntry[]>>;
}

export interface TypeIndexEntry {
  RelativePath: string;
  Index: number;
}

export interface TypeFile {
  relativePath: string;
  types: BicepType[];
}
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

export enum ObjectPropertyFlags {
  None = 0,
  Required = 1 << 0,
  ReadOnly = 1 << 1,
  WriteOnly = 1 << 2,
  DeployTimeConstant = 1 << 3,
}

const ObjectPropertyFlagsLabel = new Map<ObjectPropertyFlags, string>([
  [ObjectPropertyFlags.Required, 'Required'],
  [ObjectPropertyFlags.ReadOnly, 'ReadOnly'],
  [ObjectPropertyFlags.WriteOnly, 'WriteOnly'],
  [ObjectPropertyFlags.DeployTimeConstant, 'DeployTimeConstant'],
]);

export function getObjectPropertyFlagsLabels(input: ObjectPropertyFlags) {
  const types = [];
  for (const [key, value] of ObjectPropertyFlagsLabel) {
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

export interface TypeBase {
  readonly Type: TypeBaseKind;
}

export type TypeReference = number;

export interface BuiltInType extends TypeBase {
  readonly Type: TypeBaseKind.BuiltInType;
  readonly Kind: BuiltInTypeKind;
}

export interface UnionType extends TypeBase {
  readonly Type: TypeBaseKind.UnionType;
  readonly Elements: TypeReference[];
}

export interface StringLiteralType extends TypeBase {
  readonly Type: TypeBaseKind.StringLiteralType;
  readonly Value: string;
}

export interface ResourceType extends TypeBase {
  readonly Type: TypeBaseKind.ResourceType;
  readonly Name: string;
  readonly ScopeType: ScopeType;
  readonly ReadOnlyScopes?: ScopeType;
  readonly Body: TypeReference;
  readonly Flags: ResourceFlags;
}

export interface ResourceFunctionType extends TypeBase {
  readonly Type: TypeBaseKind.ResourceFunctionType;
  readonly Name: string;
  readonly ResourceType: string;
  readonly ApiVersion: string;
  readonly Output: TypeReference;
  readonly Input?: TypeReference;
}

export interface ObjectType extends TypeBase {
  readonly Type: TypeBaseKind.ObjectType;
  readonly Name: string;
  readonly Properties: Record<string, ObjectProperty>;
  readonly AdditionalProperties?: TypeReference;
}

export interface DiscriminatedObjectType extends TypeBase {
  readonly Type: TypeBaseKind.DiscriminatedObjectType;
  readonly Name: string;
  readonly Discriminator: string;
  readonly BaseProperties: Record<string, ObjectProperty>;
  readonly Elements: Record<string, TypeReference>;
}

export interface ArrayType extends TypeBase {
  readonly Type: TypeBaseKind.ArrayType;
  readonly ItemType: TypeReference;
}

export type Type = BuiltInType | UnionType | StringLiteralType | ResourceType | ResourceFunctionType | ObjectType | DiscriminatedObjectType | ArrayType;

export interface ObjectProperty {
  readonly Type: TypeReference;
  readonly Flags: ObjectPropertyFlags;
  readonly Description?: string;
}

export class TypeFactory {
  readonly types: Type[];
  readonly builtInTypes: Record<BuiltInTypeKind, TypeReference>;

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

  public addType(type: Type): TypeReference {
    const index = this.types.length;
    this.types[index] = type;

    return index;
  }

  public lookupType(reference: TypeReference): Type {
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

  public addObjectType(name: string, properties: Record<string, ObjectProperty>, additionalProperties?: TypeReference) {
    return this.addType({
      Type: TypeBaseKind.ObjectType,
      Name: name,
      Properties: properties,
      AdditionalProperties: additionalProperties,
    });
  }

  public addDiscriminatedObjectType(name: string, discriminator: string, baseProperties: Record<string, ObjectProperty>, elements: Record<string, TypeReference>) {
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
  types: TypeBase[];
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { ArrayType, BuiltInType, DiscriminatedObjectType, getBuiltInTypeKindLabel, getObjectTypePropertyFlagsLabels, getResourceFlagsLabels, getScopeTypeLabels, ObjectTypeProperty, ObjectType, ResourceFunctionType, ResourceType, StringLiteralType, StringType, BicepType, TypeBaseKind, TypeIndex, TypeReference, UnionType, IntegerType } from '../types';
import { groupBy, orderBy } from '../utils';

class MarkdownFile {
  private output = '';

  generateAnchorLink(name: string) {
    return `[${name}](#${name.replace(/[^a-zA-Z0-9-]/g, '').toLowerCase()})`;
  }

  writeHeading(nesting: number, message: string) {
    this.output += `${'#'.repeat(nesting)} ${message}`;
    this.writeNewLine();
  }

  writeBullet(key: string, value: string) {
    this.output += `* **${key}**: ${value}`;
    this.writeNewLine();
  }

  writeNotaBene(content: string) {
    this.output += `*${content}*`;
    this.writeNewLine();
  }

  writeNewLine() {
    this.output += '\n';
  }

  toString() {
    return this.output;
  }
}

export function writeMarkdown(types: BicepType[], fileHeading?: string) {
  const md = new MarkdownFile();

  function getTypeName(types: BicepType[], typeReference: TypeReference): string {
    const type = types[typeReference.index];
    switch (type.type) {
      case TypeBaseKind.BuiltInType:
        return getBuiltInTypeKindLabel((type as BuiltInType).kind).toLowerCase();
      case TypeBaseKind.ObjectType:
        return md.generateAnchorLink((type as ObjectType).name);
      case TypeBaseKind.ArrayType:
        return getArrayTypeName(types, (type as ArrayType));
      case TypeBaseKind.ResourceType:
        return (type as ResourceType).name;
      case TypeBaseKind.ResourceFunctionType: {
        const functionType = type as ResourceFunctionType;
        return `${functionType.name} (${functionType.resourceType}@${functionType.apiVersion})`;
      }
      case TypeBaseKind.UnionType: {
        const elements = (type as UnionType).elements.map(x => getTypeName(types, x));
        return elements.sort().join(' | ');
      }
      case TypeBaseKind.StringLiteralType:
        return `'${(type as StringLiteralType).value}'`;
      case TypeBaseKind.DiscriminatedObjectType:
        return md.generateAnchorLink((type as DiscriminatedObjectType).name);
      case TypeBaseKind.AnyType:
        return 'any';
      case TypeBaseKind.NullType:
        return 'null';
      case TypeBaseKind.BooleanType:
        return 'bool';
      case TypeBaseKind.IntegerType:
        return `int${getIntegerModifiers(type as IntegerType)}`;
      case TypeBaseKind.StringType:
        return `string${getStringModifiers(type as StringType)}`;
      default:
        throw `Unrecognized type`;
    }
  }

  function getArrayTypeName(types: BicepType[], type: ArrayType): string
  {
    let itemTypeName = getTypeName(types, type.itemType);
    if (itemTypeName.indexOf(' ') != -1)
    {
      itemTypeName = `(${itemTypeName})`;
    }

    return `${itemTypeName}[]${formatModifiers(type.minLength !== undefined ? `minLength: ${type.minLength}` : undefined, type.maxLength !== undefined ? `maxLength: ${type.maxLength}` : undefined)}`;
  }

  function getIntegerModifiers(type: IntegerType): string
  {
    return formatModifiers(type.minValue !== undefined ? `minValue: ${type.minValue}` : undefined,
      type.maxValue !== undefined ? `maxValue: ${type.maxValue}` : undefined);
  }

  function getStringModifiers(type: StringType): string
  {
    return formatModifiers(type.sensitive ? 'sensitive' : undefined,
      type.minLength !== undefined ? `minLength: ${type.minLength}` : undefined,
      type.maxLength !== undefined ? `maxLength: ${type.maxLength}` : undefined,
      type.pattern !== undefined ? `pattern: "${type.pattern.replace('"', '\\"')}"` : undefined);
  }

  function formatModifiers(...modifiers: Array<string | undefined>): string
  {
    const modifierString = modifiers.filter(modifier => !!modifier).join(', ');
    return modifierString.length > 0 ? ` {${modifierString}}` : modifierString;
  }

  function writeTypeProperty(types: BicepType[], name: string, property: ObjectTypeProperty) {
    const flagsString = property.flags ? ` (${getObjectTypePropertyFlagsLabels(property.flags).join(', ')})` : '';
    const descriptionString = property.description ? `: ${property.description}` : '';
    md.writeBullet(name, `${getTypeName(types, property.type)}${flagsString}${descriptionString}`);
  }

  function findTypesToWrite(types: BicepType[], typesToWrite: BicepType[], typeReference: TypeReference) {
    function processTypeLinks(typeReference: TypeReference, skipParent: boolean) {
      // this is needed to avoid circular type references causing stack overflows
      if (typesToWrite.indexOf(types[typeReference.index]) === -1) {
        if (!skipParent) {
          typesToWrite.push(types[typeReference.index]);
        }

        findTypesToWrite(types, typesToWrite, typeReference);
      }
    }

    const type = types[typeReference.index];
    switch (type.type) {
      case TypeBaseKind.ArrayType: {
        const arrayType = type as ArrayType;
        processTypeLinks(arrayType.itemType, false);

        return;
      }
      case TypeBaseKind.ObjectType: {
        const objectType = type as ObjectType;

        for (const key of sortedKeys(objectType.properties)) {
          processTypeLinks(objectType.properties[key].type, false);
        }

        if (objectType.additionalProperties !== undefined) {
          processTypeLinks(objectType.additionalProperties, false);
        }

        return;
      }
      case TypeBaseKind.DiscriminatedObjectType: {
        const discriminatedObjectType = type as DiscriminatedObjectType;

        for (const key of sortedKeys(discriminatedObjectType.baseProperties)) {
          processTypeLinks(discriminatedObjectType.baseProperties[key].type, false);
        }

        for (const key of sortedKeys(discriminatedObjectType.elements)) {
          const element = discriminatedObjectType.elements[key];
          // Don't display discriminated object elements as individual types
          processTypeLinks(element, true);
        }

        return;
      }
    }
  }

  function sortedKeys<T>(dictionary: Record<string, T>) {
    return orderBy(Object.keys(dictionary), x => x.toLowerCase());
  }

  function writeComplexType(types: BicepType[], type: BicepType, nesting: number, includeHeader: boolean) {
    switch (type.type) {
      case TypeBaseKind.ResourceType: {
        const resourceType = type as ResourceType;
        const flagsString = resourceType.flags ? ` (${getResourceFlagsLabels(resourceType.flags).join(', ')})` : '';
        md.writeHeading(nesting, `Resource ${resourceType.name}${flagsString}`);
        md.writeBullet("Valid Scope(s)", `${getScopeTypeLabels(resourceType.scopeType, [resourceType.readOnlyScopes, 'ReadOnly']).join(', ') || 'Unknown'}`);
        writeComplexType(types, types[resourceType.body.index], nesting, false);

        return;
      }
      case TypeBaseKind.ResourceFunctionType: {
        const resourceFunctionType = type as ResourceFunctionType;
        md.writeHeading(nesting, `Function ${resourceFunctionType.name} (${resourceFunctionType.resourceType}@${resourceFunctionType.apiVersion})`);
        md.writeBullet("Resource", resourceFunctionType.resourceType);
        md.writeBullet("ApiVersion", resourceFunctionType.apiVersion);
        if (resourceFunctionType.input !== undefined) {
          md.writeBullet("Input", getTypeName(types, resourceFunctionType.input));
        }
        md.writeBullet("Output", getTypeName(types, resourceFunctionType.output));

        md.writeNewLine();
        return;
      }
      case TypeBaseKind.ObjectType: {
        const objectType = type as ObjectType;
        if (includeHeader) {
          md.writeHeading(nesting, objectType.name);
        }

        if (objectType.sensitive) {
          md.writeNotaBene("Sensitive")
        }

        md.writeHeading(nesting + 1, "Properties");
        for (const key of sortedKeys(objectType.properties)) {
          writeTypeProperty(types, key, objectType.properties[key]);
        }

        if (objectType.additionalProperties !== undefined) {
          md.writeHeading(nesting + 1, "Additional Properties");
          md.writeBullet("Additional Properties Type", getTypeName(types, objectType.additionalProperties));
        }

        md.writeNewLine();
        return;
      }
      case TypeBaseKind.DiscriminatedObjectType: {
        const discriminatedObjectType = type as DiscriminatedObjectType;
        if (includeHeader) {
          md.writeHeading(nesting, discriminatedObjectType.name);
        }

        md.writeBullet("Discriminator", discriminatedObjectType.discriminator);
        md.writeNewLine();

        md.writeHeading(nesting + 1, "Base Properties");
        for (const propertyName of sortedKeys(discriminatedObjectType.baseProperties)) {
          writeTypeProperty(types, propertyName, discriminatedObjectType.baseProperties[propertyName]);
        }

        md.writeNewLine();

        for (const key of sortedKeys(discriminatedObjectType.elements)) {
          const element = discriminatedObjectType.elements[key];
          writeComplexType(types, types[element.index], nesting + 1, true);
        }

        md.writeNewLine();
        return;
      }
    }
  }

  function generateMarkdown(types: BicepType[]) {
    md.writeHeading(1, fileHeading ?? 'Bicep Types');
    md.writeNewLine();

    const resourceTypes = orderBy(types.filter(t => t.type == TypeBaseKind.ResourceType) as ResourceType[], x => x.name.split('@')[0].toLowerCase());
    const resourceFunctionTypes = orderBy(types.filter(t => t.type == TypeBaseKind.ResourceFunctionType) as ResourceFunctionType[], x => x.name.split('@')[0].toLowerCase());
    const typesToWrite: BicepType[] = []

    for (const resourceType of resourceTypes) {
      findTypesToWrite(types, typesToWrite, resourceType.body);
    }

    for (const resourceFunctionType of resourceFunctionTypes) {
      if (resourceFunctionType.input !== undefined)
      {
        typesToWrite.push(types[resourceFunctionType.input.index]);
        findTypesToWrite(types, typesToWrite, resourceFunctionType.input);
      }
      typesToWrite.push(types[resourceFunctionType.output.index]);
      findTypesToWrite(types, typesToWrite, resourceFunctionType.output);
    }

    typesToWrite.sort((a, b) => {
      const aName = (a as ObjectType).name?.toLowerCase();
      const bName = (b as ObjectType).name?.toLowerCase();

      if (aName === undefined) {
        return bName === undefined ? 0 : 1;
      }
      if (bName === undefined || aName < bName) return -1;
      if (bName > aName) return 1;
      return 0;
    });

    for (const type of (resourceTypes as BicepType[]).concat(resourceFunctionTypes).concat(typesToWrite)) {
      writeComplexType(types, type, 2, true);
    }

    return md.toString();
  }

  return generateMarkdown(types);
}

export function writeIndexMarkdown(index: TypeIndex) {
  const md = new MarkdownFile();
  md.writeHeading(1, 'Bicep Types');

  const byProvider = groupBy(Object.keys(index.resources), x => x.split('/')[0].toLowerCase());
  for (const namespace of orderBy(Object.keys(byProvider), x => x.toLowerCase())) {
    md.writeHeading(2, namespace);

    const byResourceType = groupBy(byProvider[namespace], x => x.split('@')[0].toLowerCase());
    for (const resourceType of orderBy(Object.keys(byResourceType), x => x.toLowerCase())) {
      md.writeHeading(3, resourceType);

      for (const typeString of orderBy(byResourceType[resourceType], x => x.toLowerCase())) {
        const version = typeString.split('@')[1];
        const jsonPath = index.resources[typeString].relativePath;
        const anchor = `resource-${typeString.replace(/[^a-zA-Z0-9-]/g, '').toLowerCase()}`;

        const mdPath = jsonPath.substring(0, jsonPath.toLowerCase().lastIndexOf('.json')) + '.md';

        md.writeBullet('Link', `[${version}](${mdPath}#${anchor})`);
      }

      md.writeNewLine();
    }
  }

  return md.toString();
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { ArrayType, BuiltInType, DiscriminatedObjectType, getBuiltInTypeKindLabel, getObjectTypePropertyFlagsLabels, getResourceFlagsLabels, getScopeTypeLabels, ObjectTypeProperty, ObjectType, ResourceFunctionType, ResourceType, StringLiteralType, BicepType, TypeBaseKind, TypeIndex, TypeReference, UnionType } from '../types';
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
    const type = types[typeReference];
    switch (type.Type) {
      case TypeBaseKind.BuiltInType:
        return getBuiltInTypeKindLabel((type as BuiltInType).Kind).toLowerCase();
      case TypeBaseKind.ObjectType:
        return md.generateAnchorLink((type as ObjectType).Name);
      case TypeBaseKind.ArrayType:
        return `${getTypeName(types, (type as ArrayType).ItemType)}[]`;
      case TypeBaseKind.ResourceType:
        return (type as ResourceType).Name;
      case TypeBaseKind.ResourceFunctionType: {
        const functionType = type as ResourceFunctionType;
        return `${functionType.Name} (${functionType.ResourceType}@${functionType.ApiVersion})`;
      }
      case TypeBaseKind.UnionType: {
        const elements = (type as UnionType).Elements.map(x => getTypeName(types, x));
        return elements.sort().join(' | ');
      }
      case TypeBaseKind.StringLiteralType:
        return `'${(type as StringLiteralType).Value}'`;
      case TypeBaseKind.DiscriminatedObjectType:
        return md.generateAnchorLink((type as DiscriminatedObjectType).Name);
      default:
        throw `Unrecognized type`;
    }
  }

  function writeTypeProperty(types: BicepType[], name: string, property: ObjectTypeProperty) {
    const flagsString = property.Flags ? ` (${getObjectTypePropertyFlagsLabels(property.Flags).join(', ')})` : '';
    const descriptionString = property.Description ? `: ${property.Description}` : '';
    md.writeBullet(name, `${getTypeName(types, property.Type)}${flagsString}${descriptionString}`);
  }

  function findTypesToWrite(types: BicepType[], typesToWrite: BicepType[], typeReference: TypeReference) {
    function processTypeLinks(typeReference: TypeReference, skipParent: boolean) {
      // this is needed to avoid circular type references causing stack overflows
      if (typesToWrite.indexOf(types[typeReference]) === -1) {
        if (!skipParent) {
          typesToWrite.push(types[typeReference]);
        }

        findTypesToWrite(types, typesToWrite, typeReference);
      }
    }

    const type = types[typeReference];
    switch (type.Type) {
      case TypeBaseKind.ArrayType: {
        const arrayType = type as ArrayType;
        processTypeLinks(arrayType.ItemType, false);

        return;
      }
      case TypeBaseKind.ObjectType: {
        const objectType = type as ObjectType;

        for (const key of sortedKeys(objectType.Properties)) {
          processTypeLinks(objectType.Properties[key].Type, false);
        }

        if (objectType.AdditionalProperties !== undefined) {
          processTypeLinks(objectType.AdditionalProperties, false);
        }

        return;
      }
      case TypeBaseKind.DiscriminatedObjectType: {
        const discriminatedObjectType = type as DiscriminatedObjectType;

        for (const key of sortedKeys(discriminatedObjectType.BaseProperties)) {
          processTypeLinks(discriminatedObjectType.BaseProperties[key].Type, false);
        }

        for (const key of sortedKeys(discriminatedObjectType.Elements)) {
          const element = discriminatedObjectType.Elements[key];
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
    switch (type.Type) {
      case TypeBaseKind.ResourceType: {
        const resourceType = type as ResourceType;
        const flagsString = resourceType.Flags ? ` (${getResourceFlagsLabels(resourceType.Flags).join(', ')})` : '';
        md.writeHeading(nesting, `Resource ${resourceType.Name}${flagsString}`);
        md.writeBullet("Valid Scope(s)", `${getScopeTypeLabels(resourceType.ScopeType, [resourceType.ReadOnlyScopes, 'ReadOnly']).join(', ') || 'Unknown'}`);
        writeComplexType(types, types[resourceType.Body], nesting, false);

        return;
      }
      case TypeBaseKind.ResourceFunctionType: {
        const resourceFunctionType = type as ResourceFunctionType;
        md.writeHeading(nesting, `Function ${resourceFunctionType.Name} (${resourceFunctionType.ResourceType}@${resourceFunctionType.ApiVersion})`);
        md.writeBullet("Resource", resourceFunctionType.ResourceType);
        md.writeBullet("ApiVersion", resourceFunctionType.ApiVersion);
        if (resourceFunctionType.Input !== undefined) {
          md.writeBullet("Input", getTypeName(types, resourceFunctionType.Input));
        }
        if (resourceFunctionType.Output !== undefined) {
          md.writeBullet("Output", getTypeName(types, resourceFunctionType.Output));
        }

        md.writeNewLine();
        return;
      }
      case TypeBaseKind.ObjectType: {
        const objectType = type as ObjectType;
        if (includeHeader) {
          md.writeHeading(nesting, objectType.Name);
        }

        md.writeHeading(nesting + 1, "Properties");
        for (const key of sortedKeys(objectType.Properties)) {
          writeTypeProperty(types, key, objectType.Properties[key]);
        }

        if (objectType.AdditionalProperties !== undefined) {
          md.writeHeading(nesting + 1, "Additional Properties");
          md.writeBullet("Additional Properties Type", getTypeName(types, objectType.AdditionalProperties));
        }

        md.writeNewLine();
        return;
      }
      case TypeBaseKind.DiscriminatedObjectType: {
        const discriminatedObjectType = type as DiscriminatedObjectType;
        if (includeHeader) {
          md.writeHeading(nesting, discriminatedObjectType.Name);
        }

        md.writeBullet("Discriminator", discriminatedObjectType.Discriminator);
        md.writeNewLine();

        md.writeHeading(nesting + 1, "Base Properties");
        for (const propertyName of sortedKeys(discriminatedObjectType.BaseProperties)) {
          writeTypeProperty(types, propertyName, discriminatedObjectType.BaseProperties[propertyName]);
        }

        md.writeNewLine();

        for (const key of sortedKeys(discriminatedObjectType.Elements)) {
          const element = discriminatedObjectType.Elements[key];
          writeComplexType(types, types[element], nesting + 1, true);
        }

        md.writeNewLine();
        return;
      }
    }
  }

  function generateMarkdown(types: BicepType[]) {
    md.writeHeading(1, fileHeading ?? 'Bicep Types');
    md.writeNewLine();

    const resourceTypes = orderBy(types.filter(t => t.Type == TypeBaseKind.ResourceType) as ResourceType[], x => x.Name.split('@')[0].toLowerCase());
    const resourceFunctionTypes = orderBy(types.filter(t => t.Type == TypeBaseKind.ResourceFunctionType) as ResourceFunctionType[], x => x.Name.split('@')[0].toLowerCase());
    const typesToWrite: BicepType[] = []

    for (const resourceType of resourceTypes) {
      findTypesToWrite(types, typesToWrite, resourceType.Body);
    }

    for (const resourceFunctionType of resourceFunctionTypes) {
      if (resourceFunctionType.Input !== undefined)
      {
        typesToWrite.push(types[resourceFunctionType.Input]);
        findTypesToWrite(types, typesToWrite, resourceFunctionType.Input);
      }
      if (resourceFunctionType.Output !== undefined) {
        typesToWrite.push(types[resourceFunctionType.Output]);
        findTypesToWrite(types, typesToWrite, resourceFunctionType.Output);
      }
    }

    typesToWrite.sort((a, b) => {
      const aName = (a as ObjectType).Name?.toLowerCase();
      const bName = (b as ObjectType).Name?.toLowerCase();

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

  const byProvider = groupBy(Object.keys(index.Resources), x => x.split('/')[0].toLowerCase());
  for (const namespace of orderBy(Object.keys(byProvider), x => x.toLowerCase())) {
    md.writeHeading(2, namespace);

    const byResourceType = groupBy(byProvider[namespace], x => x.split('@')[0].toLowerCase());
    for (const resourceType of orderBy(Object.keys(byResourceType), x => x.toLowerCase())) {
      md.writeHeading(3, resourceType);

      for (const typeString of orderBy(byResourceType[resourceType], x => x.toLowerCase())) {
        const version = typeString.split('@')[1];
        const jsonPath = index.Resources[typeString].RelativePath;
        const anchor = `resource-${typeString.replace(/[^a-zA-Z0-9-]/g, '').toLowerCase()}`;

        const mdPath = jsonPath.substring(0, jsonPath.toLowerCase().lastIndexOf('.json')) + '.md';

        md.writeBullet('Link', `[${version}](${mdPath}#${anchor})`);
      }

      md.writeNewLine();
    }
  }

  return md.toString();
}
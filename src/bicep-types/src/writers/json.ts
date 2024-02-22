// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { BicepType, CrossFileTypeReference, TypeIndex, TypeReference } from '../types';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function writeTypesJsonReplacer(key: any, value: any) {
  if (value instanceof TypeReference) {
    return {
      "$ref": `#/${value.index}`,
    };
  }

  return value;
}

function writeTypesJsonMapper(types: BicepType[]) {
  return types.map(t => {
    const { type, ...rest } = t;
    return {
      // System.Text.Json uses this as the polymorphic discriminator
      // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism
      '$type': type,
      ...rest,
    };
  });
}

export function writeTypesJson(types: BicepType[]) {
  const output = writeTypesJsonMapper(types);

  return JSON.stringify(output, writeTypesJsonReplacer, 2);
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function readTypesJsonReviver(key: any, value: any) {
  if (typeof value === 'object' && 
    typeof value['$ref'] === 'string' &&
    value['$ref'].startsWith('#/')) {
    const index = parseInt(value['$ref'].substring(2));

    return new TypeReference(index);
  }

  return value;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function readTypesJsonMapper(input: any[]) {
  return input.map(t => {
    const { '$type': type, ...rest } = t;
    return {
      type: type,
      ...rest,
    } as BicepType;
  });
}

export function readTypesJson(content: string) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const input = JSON.parse(content, readTypesJsonReviver) as any[];

  return readTypesJsonMapper(input);
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function writeIndexJsonReplacer(key: any, value: any) {
  if (value instanceof CrossFileTypeReference) {
    return {
      "$ref": `${value.relativePath}#/${value.index}`,
    };
  }

  return value;
}

export function writeIndexJson(index: TypeIndex) {
  return JSON.stringify(index, writeIndexJsonReplacer, 2);
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function readIndexJsonReviver(key: any, value: any) {
  if (typeof value === 'object' && 
    typeof value['$ref'] === 'string' &&
    value['$ref'].indexOf('#/') > -1) {

    const split = value['$ref'].split('#/');

    return new CrossFileTypeReference(split[0], parseInt(split[1]));
  }

  return value;
}

export function readIndexJson(content: string) {
  return JSON.parse(content, readIndexJsonReviver) as TypeIndex;
}
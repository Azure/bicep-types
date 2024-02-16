// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { BicepType, TypeIndex } from '../types';

export function writeJson(types: BicepType[]) {
  const output = types.map(t => {
    const { Type, ...rest } = t;
    return {
      // System.Text.Json uses this as the polymorphic discriminator
      // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism
      '$type': Type,
      ...rest,
    };
  });

  return JSON.stringify(output, null, 2);
}

export function readJson(content: string) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const input = JSON.parse(content) as any[];

  return input.map(t => {
    const { '$type': Type, ...rest } = t;
    return {
      Type: Type,
      ...rest,
    } as BicepType;
  });
}

export function writeIndexJson(index: TypeIndex) {
  return JSON.stringify(index, null, 2);
}

export function readIndexJson(content: string) {
  return JSON.parse(content) as TypeIndex;
}
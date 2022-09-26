// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { TypeBase, TypeIndex, TypeReference } from '../types';

export function writeJson(types: TypeBase[]) {
  return JSON.stringify(types, replacer, 0);
}

export function writeIndexJson(index: TypeIndex) {
  return JSON.stringify(index, null, 0);
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function replacer(this: any, key: string, value: any) {
  if (value instanceof TypeReference) {
    return value.Index;
  }

  if (value instanceof TypeBase) {
    const { Type, ...rest } = value;

    return {
      [Type]: rest,
    };
  }

  return value;
}
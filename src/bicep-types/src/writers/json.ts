// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { TypeBase, TypeBaseKind, TypeIndex } from '../types';

export function writeJson(types: TypeBase[]) {
  const output = types.map(t => {
    const { Type, ...rest } = t;
    return {
      [`${Type}`]: rest,
    };
  });

  return JSON.stringify(output, null, 0);
}

export function readJson(content: string) {
  const input = JSON.parse(content) as any[];

  return input.flatMap(t => 
    Object.keys(t).map(k => ({
      Type: parseInt(k) as TypeBaseKind,
      ...t[k],
    }))
  ) as TypeBase[];
}

export function writeIndexJson(index: TypeIndex) {
  return JSON.stringify(index, null, 0);
}
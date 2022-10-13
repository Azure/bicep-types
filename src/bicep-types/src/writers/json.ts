// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { BicepType, TypeBaseKind, TypeIndex } from '../types';

export function writeJson(types: BicepType[]) {
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
  ) as BicepType[];
}

export function writeIndexJson(index: TypeIndex) {
  return JSON.stringify(index, null, 0);
}
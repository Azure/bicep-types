// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
export function orderBy<T, Y>(input: T[], predicate: (item: T) => Y) {
  return input.slice().sort((a, b) => predicate(a) < predicate(b) ? -1 : 1);
}

export function groupBy<T>(input: T[], predicate: (item: T) => string) {
  return input.reduce<Record<string, T[]>>((prev, cur) => {
    prev[predicate(cur)] = prev[predicate(cur)] || [];
    prev[predicate(cur)].push(cur);

    return prev;
  }, {});
}
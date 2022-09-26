// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { ResourceFunctionType, ResourceType, TypeBaseKind, TypeFile, TypeIndex, TypeIndexEntry } from "./types";
import { orderBy } from "./utils";

export function buildIndex(typeFiles: TypeFile[], logFunc: (val: string) => void): TypeIndex {
  const resourceTypes = new Set<string>();
  const resourceFunctions = new Set<string>();
  const resDictionary: Record<string, TypeIndexEntry> = {};
  const funcDictionary: Record<string, Record<string, TypeIndexEntry[]>> = {};

  // Use a consistent sort order so that file system differences don't generate changes
  for (const typeFile of orderBy(typeFiles, f => f.relativePath.toLowerCase())) {
    const types = typeFile.types;
    for (const type of types) {
      if (type.Type == TypeBaseKind.ResourceType) {
        const resourceType = type as ResourceType;
        if (resourceTypes.has(resourceType.Name.toLowerCase())) {
          logFunc(`WARNING: Found duplicate type "${resourceType.Name}"`);
          continue;
        }
        resourceTypes.add(resourceType.Name.toLowerCase());

        resDictionary[resourceType.Name] = {
          RelativePath: typeFile.relativePath,
          Index: types.indexOf(type),
        };

        continue;
      }

      if (type.Type == TypeBaseKind.ResourceFunctionType) {
        const resourceFunction = type as ResourceFunctionType;
        const funcKey = `${resourceFunction.ResourceType}@${resourceFunction.ApiVersion}:${resourceFunction.Name}`.toLowerCase();

        const resourceTypeLower = resourceFunction.ResourceType.toLowerCase();
        const apiVersionLower = resourceFunction.ApiVersion.toLowerCase();
        if (resourceFunctions.has(funcKey)) {
          logFunc(`WARNING: Found duplicate function "${resourceFunction.Name}" for resource type "${resourceFunction.ResourceType}@${resourceFunction.ApiVersion}"`);
          continue;
        }
        resourceFunctions.add(funcKey);

        funcDictionary[resourceTypeLower] = funcDictionary[resourceTypeLower] || {};
        funcDictionary[resourceTypeLower][apiVersionLower] = funcDictionary[resourceTypeLower][apiVersionLower] || [];

        funcDictionary[resourceTypeLower][apiVersionLower].push({
          RelativePath: typeFile.relativePath,
          Index: types.indexOf(type),
        });

        continue;
      }
    }
  }

  return {
    Resources: resDictionary,
    Functions: funcDictionary,
  }
}
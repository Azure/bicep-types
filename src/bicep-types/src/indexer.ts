// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { ResourceFunctionType, ResourceType, TypeBaseKind, TypeFile, TypeIndex, CrossFileTypeReference, TypeSettings } from "./types";
import { orderBy } from "./utils";

export function buildIndex(typeFiles: TypeFile[], logFunc: (val: string) => void, settings?: TypeSettings, fallbackResourceType?: CrossFileTypeReference): TypeIndex {
  const resourceTypes = new Set<string>();
  const resourceFunctions = new Set<string>();
  const resDictionary: Record<string, CrossFileTypeReference> = {};
  const funcDictionary: Record<string, Record<string, CrossFileTypeReference[]>> = {};

  // Use a consistent sort order so that file system differences don't generate changes
  for (const typeFile of orderBy(typeFiles, f => f.relativePath.toLowerCase())) {
    const types = typeFile.types;
    for (const type of types) {
      if (type.type == TypeBaseKind.ResourceType) {
        const resourceType = type as ResourceType;
        if (resourceTypes.has(resourceType.name.toLowerCase())) {
          logFunc(`WARNING: Found duplicate type "${resourceType.name}"`);
          continue;
        }
        resourceTypes.add(resourceType.name.toLowerCase());

        resDictionary[resourceType.name] = new CrossFileTypeReference(typeFile.relativePath, types.indexOf(type));

        continue;
      }

      if (type.type == TypeBaseKind.ResourceFunctionType) {
        const resourceFunction = type as ResourceFunctionType;
        const funcKey = `${resourceFunction.resourceType}@${resourceFunction.apiVersion}:${resourceFunction.name}`.toLowerCase();

        const resourceTypeLower = resourceFunction.resourceType.toLowerCase();
        const apiVersionLower = resourceFunction.apiVersion.toLowerCase();
        if (resourceFunctions.has(funcKey)) {
          logFunc(`WARNING: Found duplicate function "${resourceFunction.name}" for resource type "${resourceFunction.resourceType}@${resourceFunction.apiVersion}"`);
          continue;
        }
        resourceFunctions.add(funcKey);

        funcDictionary[resourceTypeLower] = funcDictionary[resourceTypeLower] || {};
        funcDictionary[resourceTypeLower][apiVersionLower] = funcDictionary[resourceTypeLower][apiVersionLower] || [];

        funcDictionary[resourceTypeLower][apiVersionLower].push(
          new CrossFileTypeReference(typeFile.relativePath, types.indexOf(type)));

        continue;
      }
    }
  }

  return {
    resources: resDictionary,
    functions: funcDictionary,
    settings: settings,
    fallbackResourceType: fallbackResourceType,
  }
}
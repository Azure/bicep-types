import { EmitContext, emitFile, Model, Namespace, navigateProgram, navigateTypesInNamespace, Program, resolvePath } from "@typespec/compiler";
import { unsafe_mutateSubgraphWithNamespace } from "@typespec/compiler/experimental";
import { findVersionedNamespace, getVersioningMutators } from "@typespec/versioning";
import { buildIndex, CrossFileTypeReference, TypeFactory, TypeFile, TypeReference, TypeSettings, writeIndexJson, writeIndexMarkdown, writeMarkdown, writeTypesJson } from "@azure/bicep-types";
import { getBicepResourceNamespace, getBicepResourceType, getBicepResourceTypeOptions, getSingletonDecoratedModel, isBicepConfigurationType, isBicepNamespaceFunctionType } from "./decorators.js";
import {
  createConfigurationTypeMustNotBeVersionedDiagnostic,
  createConfigurationTypeSingletonDiagnostic,
  createModelMustBeInNamespaceDiagnostic,
  createNamespaceMustBeDecoratedDiagnostic,
  createNamespaceMustBeVersionedDiagnostic,
} from "./diagnostics.js";
import { generateFallbackType } from "./fallback.js";
import { BicepEmitterOptions } from "./lib.js";
import { generateNamespaceFunctionType, generateType } from "./transform.js";

export async function $onEmit(context: EmitContext<BicepEmitterOptions>) {
  const types = generateRoot(context.program, context.options);

  for (const { index, subDir, typeFiles } of types) {
    await addEmittedFile(context, `${subDir}/index.json`, writeIndexJson(index));
    await addEmittedFile(context, `${subDir}/index.md`, writeIndexMarkdown(index, typeFiles));
  }

  for (const { subDir, typeFiles } of types) {
    for (const { types, relativePath } of typeFiles) {
      await addEmittedFile(context, `${subDir}/${relativePath}`, writeTypesJson(types));
      await addEmittedFile(context, `${subDir}/${relativePath.substring(0, relativePath.lastIndexOf("."))}.md`, writeMarkdown(types));
    }
  }
}

async function addEmittedFile(context: EmitContext<BicepEmitterOptions>, path: string, content: string) {
  // paths with leading slashes are not working in the emitter, so we remove them
  const pathWithoutLeadingSlash = path.startsWith("/") ? path.substring(1) : path;
  await emitFile(context.program, {
    path: resolvePath(context.emitterOutputDir, pathWithoutLeadingSlash),
    content: content,
  });
}

export function generateRoot(program: Program, options?: BicepEmitterOptions) {
  options ??= {
    "bicep-extension-name": "$onValidate",
    "bicep-extension-version": "1.0.0",
  };

  const typeFactory = new TypeFactory();
  const fallbackType = generateFallbackType(program, typeFactory);
  const configurationType = generateConfigurationType(program, typeFactory);
  const namespaceFunctionTypes = generateNamespaceFunctions(program, typeFactory);

  // only create the types.json if type factory has types in it
  const typeFile =
    typeFactory.types.length > 0
      ? {
          relativePath: "types.json",
          types: typeFactory.types,
        }
      : undefined;

  const fallbackTypeReference = fallbackType !== undefined ? new CrossFileTypeReference("types.json", fallbackType.index) : undefined;
  const configurationTypeReference = configurationType !== undefined ? new CrossFileTypeReference("types.json", configurationType.index) : undefined;

  //generateFallbackTypeReference(program, typeFactory);
  const typeFiles: TypeFile[] = generateTypesForAllVersions(program);

  const typeSettings: TypeSettings = {
    name: options["bicep-extension-name"],
    version: options["bicep-extension-version"],
    isSingleton: options["bicep-extension-is-singleton"] ?? false,
    isPreview: options["bicep-extension-is-preview"] ?? false,
    isDeprecated: options["bicep-extension-is-deprecated"] ?? false,
    configurationType: configurationTypeReference,
  };

  // TODO: report a diagnostic instead of calling console.log
  const index = buildIndex(typeFiles, console.log, typeSettings, fallbackTypeReference);

  if (typeFile) {
    for (const ref of namespaceFunctionTypes) {
      index.namespaceFunctions.push(new CrossFileTypeReference(typeFile.relativePath, ref.index));
    }
  }

  return [
    {
      index,
      subDir: "",
      typeFiles: [...typeFiles, ...(typeFile ? [typeFile] : [])],
    },
  ];
}

function generateTypesForAllVersions(program: Program): TypeFile[] {
  const namespacesWithResources: Namespace[] = [];
  navigateProgram(
    program,
    {
      namespace: (ns) => {
        if (getBicepResourceNamespace(program, ns) !== undefined) {
          namespacesWithResources.push(ns);
        }
      },
    },
    {
      includeTemplateDeclaration: false,
      visitDerivedTypes: false,
    },
  );

  const typeFiles: TypeFile[] = [];
  for (const namespace of namespacesWithResources) {
    const typeFilesFromNamespace = generateTypesForNamespace(program, namespace);
    if (typeFilesFromNamespace === undefined) {
      continue;
    }

    typeFiles.push(...typeFilesFromNamespace);
  }

  return typeFiles;
}

function generateTypesForNamespace(program: Program, namespace: Namespace): TypeFile[] | undefined {
  const typeFiles: TypeFile[] = [];

  // the versioned namespace may be above the namespace decorated with @bicepResourceNamespace
  const versionedNamespace = findVersionedNamespace(program, namespace);
  if (versionedNamespace === undefined) {
    createNamespaceMustBeVersionedDiagnostic(program, namespace);
    return undefined;
  }

  const versioningMutators = getVersioningMutators(program, versionedNamespace);
  if (versioningMutators?.kind !== "versioned") {
    createNamespaceMustBeVersionedDiagnostic(program, namespace);
    return undefined;
  }

  for (const versionedSnapshot of versioningMutators.snapshots) {
    const mutatedSubgraph = unsafe_mutateSubgraphWithNamespace(program, [versionedSnapshot.mutator], versionedNamespace);
    if (!mutatedSubgraph.realm || mutatedSubgraph.type.kind !== "Namespace") {
      // TODO: This shouldn't really happen, so how do we handle this case?
      continue;
    }

    // it's important to use the mutated subgraph objects rather than the original ones
    // so that we correctly handle the versioning decorators on the models within the namespace
    const typeFile = generateTypesForMutatedNamespace(mutatedSubgraph.realm.program, mutatedSubgraph.type, versionedSnapshot.version.value);

    if (typeFile === undefined) {
      // diagnostics should have been reported during the type generation
      return undefined;
    }

    typeFiles.push(typeFile);
  }

  return typeFiles;
}

function generateTypesForMutatedNamespace(program: Program, namespace: Namespace, apiVersion: string): TypeFile | undefined {
  const typeFactory = new TypeFactory();
  let failed = false;
  navigateTypesInNamespace(
    namespace,
    {
      model: (model) => {
        if (!generateTypeForVersionedModel(apiVersion, program, typeFactory, model)) {
          failed = true;
        }
      },
    },
    {
      includeTemplateDeclaration: false,
      skipSubNamespaces: false,
      visitDerivedTypes: true,
    },
  );

  if (failed) {
    // diagnostics should have been reported during the type generation
    return undefined;
  }

  return {
    // This is the namespace that is versioned which may or may not be the same as the resource type namespace
    relativePath: `./${namespace.name}/${apiVersion}/types.json`,
    types: typeFactory.types,
  };
}

function generateTypeForVersionedModel(apiVersion: string, program: Program, typeFactory: TypeFactory, model: Model): boolean {
  const resourceType = getBicepResourceType(program, model);
  if (resourceType === undefined) {
    // models do not have to be decorated with @bicepResourceType
    // no type was generated but we also didn't fail
    return true;
  }

  const options = getBicepResourceTypeOptions(program, model) ?? {
    capabilities: ["Readable", "Writeable"],
  };

  if (model.namespace === undefined) {
    createModelMustBeInNamespaceDiagnostic(program, model);
    return false;
  }

  const resourceNamespace = getBicepResourceNamespace(program, model.namespace);
  if (resourceNamespace === undefined) {
    // TODO: This was checked earlier when we collected the namespaces, so this should never happen
    createNamespaceMustBeDecoratedDiagnostic(program, model.namespace);
    return false;
  }

  const resourceBodyType = generateType(program, typeFactory, model, model);
  if (resourceBodyType === undefined) {
    return false;
  }

  typeFactory.addUnscopedResourceType(`${resourceNamespace}/${resourceType}@${apiVersion}`, resourceBodyType, options.capabilities.includes("Readable"), options.capabilities.includes("Writeable"));

  return true;
}

function generateConfigurationType(program: Program, typeFactory: TypeFactory): TypeReference | undefined {
  const configurationTypeModel = getSingletonDecoratedModel(program, isBicepConfigurationType, createConfigurationTypeMustNotBeVersionedDiagnostic, createConfigurationTypeSingletonDiagnostic);
  if (configurationTypeModel === undefined) {
    return undefined;
  }

  return generateType(program, typeFactory, configurationTypeModel, configurationTypeModel);
}

function generateNamespaceFunctions(program: Program, typeFactory: TypeFactory): TypeReference[] {
  const results: TypeReference[] = [];
  navigateProgram(
    program,
    {
      model: (model) => {
        if (isBicepNamespaceFunctionType(program, model)) {
          const result = generateNamespaceFunctionType(program, typeFactory, model);
          if (result) {
            results.push(result);
          }
        }
      },
    },
    {
      includeTemplateDeclaration: false,
      visitDerivedTypes: false,
    },
  );
  return results;
}
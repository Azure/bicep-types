// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import path from 'path';
import { existsSync } from 'fs';
import { mkdir, writeFile, readFile } from 'fs/promises';
import { CrossFileTypeReference, FunctionParameter, ObjectTypePropertyFlags, ResourceFlags, ScopeType, TypeFactory, TypeFile, TypeIndex, TypeSettings } from '../../src/types';
import { readTypesJson, writeIndexJson, writeTypesJson } from '../../src/writers/json';
import { writeIndexMarkdown, writeMarkdown } from '../../src/writers/markdown';
import { buildIndex } from '../../src/indexer';

// set to true to overwrite baselines
const record = (process.env['BASELINE_RECORD']?.toLowerCase() === 'true');
const baselinesDir = path.resolve(`${__dirname}/baselines`);

describe('types tests', () => {
  it('should generated expected json & markdown', async () => {
    const factory = new TypeFactory();

    const props = factory.addObjectType('foo', {
      abc: {
        type: factory.addStringType(),
        flags: ObjectTypePropertyFlags.None,
        description: 'Abc prop'
      },
      def: {
        type: factory.addObjectType("def", {}),
        flags: ObjectTypePropertyFlags.ReadOnly,
        description: 'Def prop'
      },
      ghi: {
        type: factory.addBooleanType(),
        flags: ObjectTypePropertyFlags.WriteOnly,
        description: 'Ghi prop'
      },
      jkl: {
        type: factory.addObjectType("jkl", {}),
        flags: ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required,
        description: 'Jkl prop'
      },
      dictType: {
        type: factory.addObjectType('dictType', {},
          factory.addAnyType(),
          true),
        flags: ObjectTypePropertyFlags.None,
        description: 'Dictionary of any'
      },
      arrayType: {
        type: factory.addArrayType(factory.addAnyType(), 1, 10),
        flags: ObjectTypePropertyFlags.None,
        description: 'Array of any'
      },
    });

    const funcArg: FunctionParameter = { name: 'arg', type: factory.addStringType() };
    const funcArg2: FunctionParameter = { name: 'arg2', type: factory.addStringType() };
    const func = factory.addFunctionType([funcArg, funcArg2], factory.addBooleanType());

    const res = factory.addResourceType('foo@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None, { doSomething: { type: func } });

    const configFactory = new TypeFactory();
    const configLocation = configFactory.addObjectType('config', {
      configProp: {
        type: factory.addStringType(),
        flags: ObjectTypePropertyFlags.Required,
        description: 'Config property',
      },
    });
    const fallbackRef = configFactory.addResourceType('fallback', ScopeType.Unknown, undefined, configFactory.addObjectType('fallback body', {
      bodyProp: {
        type: factory.addStringType(),
        flags: ObjectTypePropertyFlags.Required,
        description: 'Body property',
      },
    }), ResourceFlags.None);

    const settings: TypeSettings = {
      name: 'Foo',
      isSingleton: true,
      isPreview: true,
      isDeprecated: false,
      version: '0.1.2',
      configurationType: new CrossFileTypeReference('types.json', configLocation.index),
    };

    const fallbackResourceType = new CrossFileTypeReference('types.json', fallbackRef.index);

    await verifyBaselines(factory, 'foo', 'foo', configFactory, settings, fallbackResourceType);
  });

  it('should generated http types', async () => {
    const factory = new TypeFactory();

    const formatType = factory.addUnionType([
      factory.addStringLiteralType("raw"),
      factory.addStringLiteralType("json"),
    ]);

    const props = factory.addObjectType('request@v1', {
      uri: { type: factory.addStringType(), flags: ObjectTypePropertyFlags.Required, description: 'The HTTP request URI to submit a GET request to.' },
      format: { type: formatType, flags: ObjectTypePropertyFlags.None, description: 'How to deserialize the response body.' },
      method: { type: factory.addStringType(undefined, 3), flags: ObjectTypePropertyFlags.None, description: 'The HTTP method to submit request to the given URI.' },
      statusCode: { type: factory.addIntegerType(100, 599), flags: ObjectTypePropertyFlags.ReadOnly, description: 'The status code of the HTTP request.' },
      body: { type: factory.addAnyType(), flags: ObjectTypePropertyFlags.ReadOnly, description: 'The parsed request body.' },
    });
    factory.addResourceType('request@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None);

    await verifyBaselines(factory, 'http/v1', 'http');
  });

  it('should not succeed if record is set to true', () => {
    // This test just ensures the suite doesn't pass in 'record' mode
    expect(record).toBeFalsy();
  });
});

async function verifyBaselines(factory: TypeFactory, typesPath: string, testName: string, configFactory?: TypeFactory, settings?: TypeSettings, fallbackResourceType?: CrossFileTypeReference) {
  const deserializedTypes = readTypesJson(writeTypesJson(factory.types));
  expect(deserializedTypes).toEqual(factory.types);

  const typeFiles = [{
    relativePath: `${typesPath}/types.json`,
    types: factory.types,
  }];

  const index = buildIndex(typeFiles, console.log, settings, fallbackResourceType);

  if (configFactory) {
    const deserializedTypes = readTypesJson(writeTypesJson(configFactory.types));
    expect(deserializedTypes).toEqual(configFactory.types);

    typeFiles.push({
      relativePath: `types.json`,
      types: configFactory.types,
    });
  }

  expectFiles(testName, typeFiles, index);
}

async function expectFiles(testName: string, typeFiles: TypeFile[], index: TypeIndex) {
  const baseDir = `${baselinesDir}/${testName}`;
  await expectFileContents(`${baseDir}/index.json`, writeIndexJson(index));
  await expectFileContents(`${baseDir}/index.md`, writeIndexMarkdown(index));
  for (const { types, relativePath } of typeFiles) {
    await expectFileContents(`${baseDir}/${relativePath}`, writeTypesJson(types));
    await expectFileContents(`${baseDir}/${relativePath.substring(0, relativePath.lastIndexOf('.'))}.md`, writeMarkdown(types));
  }
}

async function expectFileContents(filePath: string, contents: string) {
  if (record) {
    await mkdir(path.dirname(filePath), { recursive: true });
    await writeFile(filePath, contents, 'utf-8');
  } else {
    // If these assertions fail, use 'npm run test:fix' to replace the baseline files
    expect(existsSync(filePath)).toBeTruthy();

    const readContents = await readFile(filePath, { encoding: 'utf8' });
    expect(contents).toBe(readContents);
  }
}

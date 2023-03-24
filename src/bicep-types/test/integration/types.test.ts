// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import path from 'path';
import { existsSync } from 'fs';
import { mkdir, writeFile, readFile } from 'fs/promises';
import { ObjectTypePropertyFlags, ResourceFlags, ScopeType, TypeFactory, TypeFile, TypeIndex } from '../../src/types';
import { readJson, writeIndexJson, writeJson } from '../../src/writers/json';
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
        Type: factory.addStringType(),
        Flags: ObjectTypePropertyFlags.None,
        Description: 'Abc prop'
      },
      def: {
        Type: factory.addObjectType("def", {}),
        Flags: ObjectTypePropertyFlags.ReadOnly,
        Description: 'Def prop'
      },
      ghi: {
        Type: factory.addBooleanType(),
        Flags: ObjectTypePropertyFlags.WriteOnly,
        Description: 'Ghi prop'
      },
      jkl: {
        Type: factory.addObjectType("jkl", {}),
        Flags: ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required,
        Description: 'Jkl prop'
      },
      dictType: {
        Type: factory.addObjectType('dictType', {},
          factory.addAnyType()),
        Flags: ObjectTypePropertyFlags.None,
        Description: 'Dictionary of any'
      },
      arrayType: {
        Type: factory.addArrayType(factory.addAnyType()),
        Flags: ObjectTypePropertyFlags.None,
        Description: 'Array of any'
      },
    });
    const res = factory.addResourceType('foo@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None);

    await verifyBaselines(factory, 'foo', 'foo');
  });

  it('should generated http types', async () => {
    const factory = new TypeFactory();

    const formatType = factory.addUnionType([
      factory.addStringLiteralType("raw"),
      factory.addStringLiteralType("json"),
    ]);

    const props = factory.addObjectType('request@v1', {
      uri: { Type: factory.addStringType(), Flags: ObjectTypePropertyFlags.Required, Description: 'The HTTP request URI to submit a GET request to.' },
      format: { Type: formatType, Flags: ObjectTypePropertyFlags.None, Description: 'How to deserialize the response body.' },
      method: { Type: factory.addStringType(undefined, 3), Flags: ObjectTypePropertyFlags.None, Description: 'The HTTP method to submit request to the given URI.' },
      statusCode: { Type: factory.addIntegerType(100, 599), Flags: ObjectTypePropertyFlags.ReadOnly, Description: 'The status code of the HTTP request.' },
      body: { Type: factory.addAnyType(), Flags: ObjectTypePropertyFlags.ReadOnly, Description: 'The parsed request body.' },
    });
    factory.addResourceType('request@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None);

    await verifyBaselines(factory, 'http/v1', 'http');
  });

  it('should not succeed if record is set to true', () => {
    // This test just ensures the suite doesn't pass in 'record' mode
    expect(record).toBeFalsy();
  });
});

async function verifyBaselines(factory: TypeFactory, typesPath: string, testName: string) {
  const deserializedTypes = readJson(writeJson(factory.types));
  expect(deserializedTypes).toEqual(factory.types);

  const typeFiles = [{
    relativePath: `${typesPath}/types.json`,
    types: factory.types,
  }];
  const index = buildIndex(typeFiles, console.log);

  expectFiles(testName, typeFiles, index);
}

async function expectFiles(testName: string, typeFiles: TypeFile[], index: TypeIndex) {
  const baseDir = `${baselinesDir}/${testName}`;
  await expectFileContents(`${baseDir}/index.json`, writeIndexJson(index));
  await expectFileContents(`${baseDir}/index.md`, writeIndexMarkdown(index));
  for (const { types, relativePath } of typeFiles) {
    await expectFileContents(`${baseDir}/${relativePath}`, writeJson(types));
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

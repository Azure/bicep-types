// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { BuiltInTypeKind, ObjectProperty, ObjectPropertyFlags, ObjectType, ResourceFlags, ResourceType, ScopeType, TypeFactory } from '../../src/types';
import { readJson, writeIndexJson, writeJson } from '../../src/writers/json';
import { writeIndexMarkdown, writeMarkdown } from '../../src/writers/markdown';
import { buildIndex } from '../../src/indexer';

describe('types tests', () => {
  it('should generated expected json & markdown', async () => {
    const factory = new TypeFactory();

    const props = factory.addObjectType('foo', {
      abc: { Type: factory.builtInTypes[BuiltInTypeKind.String], Flags: ObjectPropertyFlags.None, Description: 'Abc prop' },
      def: { Type: factory.builtInTypes[BuiltInTypeKind.Object], Flags: ObjectPropertyFlags.ReadOnly, Description: 'Def prop' },
      ghi: { Type: factory.builtInTypes[BuiltInTypeKind.Bool], Flags: ObjectPropertyFlags.WriteOnly, Description: 'Ghi prop' },
      dictType: { Type: factory.addObjectType('dictType', {}, factory.builtInTypes[BuiltInTypeKind.Any]), Flags: ObjectPropertyFlags.None, Description: 'Dictionary of any' },
      arrayType: { Type: factory.addArrayType(factory.builtInTypes[BuiltInTypeKind.Any]), Flags: ObjectPropertyFlags.None, Description: 'Array of any' },
    });
    const res = factory.addResourceType('foo@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None);

    const json = writeJson(factory.types);
    expect(json).toBe("[{\"1\":{\"Kind\":1}},{\"1\":{\"Kind\":2}},{\"1\":{\"Kind\":3}},{\"1\":{\"Kind\":4}},{\"1\":{\"Kind\":5}},{\"1\":{\"Kind\":6}},{\"1\":{\"Kind\":7}},{\"1\":{\"Kind\":8}},{\"2\":{\"Name\":\"dictType\",\"Properties\":{},\"AdditionalProperties\":0}},{\"3\":{\"ItemType\":0}},{\"2\":{\"Name\":\"foo\",\"Properties\":{\"abc\":{\"Type\":4,\"Flags\":0,\"Description\":\"Abc prop\"},\"def\":{\"Type\":5,\"Flags\":2,\"Description\":\"Def prop\"},\"ghi\":{\"Type\":2,\"Flags\":4,\"Description\":\"Ghi prop\"},\"dictType\":{\"Type\":8,\"Flags\":0,\"Description\":\"Dictionary of any\"},\"arrayType\":{\"Type\":9,\"Flags\":0,\"Description\":\"Array of any\"}}}},{\"4\":{\"Name\":\"foo@v1\",\"ScopeType\":0,\"Body\":10,\"Flags\":0}}]");

    const deserializedTypes = readJson(json);
    expect(deserializedTypes).toEqual(factory.types);

    const markdown = writeMarkdown(factory.types);
    expect(markdown).toBe(`# Bicep Types

## Resource foo@v1
* **Valid Scope(s)**: Unknown
### Properties
* **abc**: string: Abc prop
* **arrayType**: any[]: Array of any
* **def**: object (ReadOnly): Def prop
* **dictType**: [dictType](#dicttype): Dictionary of any
* **ghi**: bool (WriteOnly): Ghi prop

## dictType
### Properties
### Additional Properties
* **Additional Properties Type**: any

`);

const index = buildIndex([{
  relativePath: 'foo/types.json',
  types: factory.types,
}], _ => {});

const jsonIndex = writeIndexJson(index);
expect(jsonIndex).toBe("{\"Resources\":{\"foo@v1\":{\"RelativePath\":\"foo/types.json\",\"Index\":11}},\"Functions\":{}}");

const markdownIndex = writeIndexMarkdown(index);
expect(markdownIndex).toBe(`# Bicep Types
## foo@v1
### foo
* **Link**: [v1](foo/types.md#resource-foov1)

`);
  });

  it('should generated http types', async () => {
    const factory = new TypeFactory();

    const props = factory.addObjectType('request@v1', {
      requestUri: { Type: factory.builtInTypes[BuiltInTypeKind.String], Flags: ObjectPropertyFlags.None, Description: 'The HTTP request URI to submit a GET request to.' },
    });
    factory.addResourceType('request@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None);

    const json = writeJson(factory.types);
    expect(json).toBe('[{\"1\":{\"Kind\":1}},{\"1\":{\"Kind\":2}},{\"1\":{\"Kind\":3}},{\"1\":{\"Kind\":4}},{\"1\":{\"Kind\":5}},{\"1\":{\"Kind\":6}},{\"1\":{\"Kind\":7}},{\"1\":{\"Kind\":8}},{\"2\":{\"Name\":\"request@v1\",\"Properties\":{\"requestUri\":{\"Type\":4,\"Flags\":0,\"Description\":\"The HTTP request URI to submit a GET request to.\"}}}},{\"4\":{\"Name\":\"request@v1\",\"ScopeType\":0,\"Body\":8,\"Flags\":0}}]');

    const deserializedTypes = readJson(json);
    expect(deserializedTypes).toEqual(factory.types);

    const markdown = writeMarkdown(factory.types);
    expect(markdown).toBe(`# Bicep Types

## Resource request@v1
* **Valid Scope(s)**: Unknown
### Properties
* **requestUri**: string: The HTTP request URI to submit a GET request to.

`);

    const index = buildIndex([{
      relativePath: 'http/v1/types.json',
      types: factory.types,
    }], _ => {});

    const jsonIndex = writeIndexJson(index);
    expect(jsonIndex).toBe("{\"Resources\":{\"request@v1\":{\"RelativePath\":\"http/v1/types.json\",\"Index\":9}},\"Functions\":{}}");

    const markdownIndex = writeIndexMarkdown(index);
    expect(markdownIndex).toBe(`# Bicep Types
## request@v1
### request
* **Link**: [v1](http/v1/types.md#resource-requestv1)

`);
  });
});
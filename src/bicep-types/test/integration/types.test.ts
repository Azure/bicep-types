// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { BuiltInTypeKind, ObjectProperty, ObjectPropertyFlags, ObjectType, ResourceFlags, ResourceType, ScopeType, TypeFactory } from '../../src/types';
import { writeIndexJson, writeJson } from '../../src/writers/json';
import { writeIndexMarkdown, writeMarkdown } from '../../src/writers/markdown';
import { buildIndex } from '../../src/indexer';

describe('types tests', () => {
  it('should generated expected json & markdown', async () => {
    const factory = new TypeFactory();

    const props = factory.addType(new ObjectType('foo', {
      abc: new ObjectProperty(factory.builtInTypes[BuiltInTypeKind.String], ObjectPropertyFlags.None, 'Abc prop'),
      def: new ObjectProperty(factory.builtInTypes[BuiltInTypeKind.Object], ObjectPropertyFlags.ReadOnly, 'Def prop'),
      ghi: new ObjectProperty(factory.builtInTypes[BuiltInTypeKind.Bool], ObjectPropertyFlags.WriteOnly, 'Ghi prop'),
    }));
    const res = factory.addType(new ResourceType('foo@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None));

    const json = writeJson(factory.types);
    expect(json).toBe('[{\"1\":{\"Kind\":1}},{\"1\":{\"Kind\":2}},{\"1\":{\"Kind\":3}},{\"1\":{\"Kind\":4}},{\"1\":{\"Kind\":5}},{\"1\":{\"Kind\":6}},{\"1\":{\"Kind\":7}},{\"1\":{\"Kind\":8}},{\"2\":{\"Name\":\"foo\",\"Properties\":{\"abc\":{\"Type\":4,\"Flags\":0,\"Description\":\"Abc prop\"},\"def\":{\"Type\":5,\"Flags\":2,\"Description\":\"Def prop\"},\"ghi\":{\"Type\":2,\"Flags\":4,\"Description\":\"Ghi prop\"}}}},{\"4\":{\"Name\":\"foo@v1\",\"ScopeType\":0,\"Body\":8,\"Flags\":0}}]');

    const markdown = writeMarkdown(factory.types);
    expect(markdown).toBe(`# Bicep Types

## Resource foo@v1
* **Valid Scope(s)**: Unknown
### Properties
* **abc**: string: Abc prop
* **def**: object (ReadOnly): Def prop
* **ghi**: bool (WriteOnly): Ghi prop

`);

const index = buildIndex([{
  relativePath: 'foo/types.json',
  types: factory.types,
}], _ => {});

const jsonIndex = writeIndexJson(index);
expect(jsonIndex).toBe("{\"Resources\":{\"foo@v1\":{\"RelativePath\":\"foo/types.json\",\"Index\":9}},\"Functions\":{}}");

const markdownIndex = writeIndexMarkdown(index);
expect(markdownIndex).toBe(`# Bicep Types
## foo@v1
### foo
* **Link**: [v1](foo/types.md#resource-foov1)

`);
  });

  it('should generated http types', async () => {
    const factory = new TypeFactory();

    const props = factory.addType(new ObjectType('request@v1', {
      requestUri: new ObjectProperty(factory.builtInTypes[BuiltInTypeKind.String], ObjectPropertyFlags.None, 'The HTTP request URI to submit a GET request to.'),
    }));
    factory.addType(new ResourceType('request@v1', ScopeType.Unknown, undefined, props, ResourceFlags.None));

    const json = writeJson(factory.types);
    expect(json).toBe('[{\"1\":{\"Kind\":1}},{\"1\":{\"Kind\":2}},{\"1\":{\"Kind\":3}},{\"1\":{\"Kind\":4}},{\"1\":{\"Kind\":5}},{\"1\":{\"Kind\":6}},{\"1\":{\"Kind\":7}},{\"1\":{\"Kind\":8}},{\"2\":{\"Name\":\"request@v1\",\"Properties\":{\"requestUri\":{\"Type\":4,\"Flags\":0,\"Description\":\"The HTTP request URI to submit a GET request to.\"}}}},{\"4\":{\"Name\":\"request@v1\",\"ScopeType\":0,\"Body\":8,\"Flags\":0}}]');

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
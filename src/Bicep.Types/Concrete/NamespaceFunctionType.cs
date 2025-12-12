// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class NamespaceFunctionType : TypeBase
{
    [JsonConstructor]
    public NamespaceFunctionType(string name, ITypeReference type, string? description)
        => (Name, Type, Description) = (name, type, description);

    public string Name { get; }
    public ITypeReference Type { get; }
    public string? Description { get; }
}

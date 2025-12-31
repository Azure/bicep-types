// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class NamespaceFunctionType : TypeBase
{
    [JsonConstructor]
    public NamespaceFunctionType(
        string name,
        string? description,
        string? evaluatesTo,
        IReadOnlyList<FunctionParameter> parameters,
        ITypeReference output,
        NamespaceFunctionTypeFlags flags,
        NamespaceFunctionTypeFileVisibilityFlags fileVisibilityFlags)
        => (Name, Description, EvaluatesTo, Parameters, Output, Flags, FileVisibilityFlags) = (name, description, evaluatesTo, parameters, output, flags, fileVisibilityFlags);

    public string Name { get; }
    public string? Description { get; }
    public string? EvaluatesTo { get; }
    public IReadOnlyList<FunctionParameter> Parameters { get; }
    public ITypeReference Output { get; }
    public NamespaceFunctionTypeFlags Flags { get; }
    public NamespaceFunctionTypeFileVisibilityFlags FileVisibilityFlags { get; }
}

public enum NamespaceFunctionTypeFileVisibilityFlags
{
    None = 0,
    Bicep = 1 << 0,
    Bicepparam = 1 << 1,
}

public enum NamespaceFunctionTypeFlags
{
    Default = 0,
    ExternalInput = 1 << 0
}

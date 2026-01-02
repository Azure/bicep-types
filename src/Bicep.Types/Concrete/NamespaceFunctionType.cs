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
        NamespaceFunctionTypeFileVisibility fileVisibilityFlags)
        => (Name, Description, EvaluatesTo, Parameters, Output, Flags, FileVisibilityFlags) = (name, description, evaluatesTo, parameters, output, flags, fileVisibilityFlags);

    public string Name { get; }
    public string? Description { get; }
    public string? EvaluatesTo { get; }
    public IReadOnlyList<FunctionParameter> Parameters { get; }
    public ITypeReference Output { get; }
    public NamespaceFunctionTypeFlags Flags { get; }
    public NamespaceFunctionTypeFileVisibility FileVisibilityFlags { get; }
}

public enum NamespaceFunctionTypeFileVisibility
{
    Bicep,
    Bicepparam,
}

public enum NamespaceFunctionTypeFlags
{
    Default = 0,
    ExternalInput = 1 << 0
}

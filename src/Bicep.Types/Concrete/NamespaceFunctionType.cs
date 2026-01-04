// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
        IReadOnlyList<NamespaceFunctionParameter> parameters,
        ITypeReference output,
        NamespaceFunctionFlags flags,
        NamespaceFunctionFileVisibilityRestriction? fileVisibilityRestriction)
        => (Name, Description, EvaluatesTo, Parameters, Output, Flags, FileVisibilityRestriction) = (name, description, evaluatesTo, parameters, output, flags, fileVisibilityRestriction);

    public string Name { get; }
    public string? Description { get; }
    public string? EvaluatesTo { get; }
    public IReadOnlyList<NamespaceFunctionParameter> Parameters { get; }
    public ITypeReference Output { get; }
    public NamespaceFunctionFlags Flags { get; }
    public NamespaceFunctionFileVisibilityRestriction? FileVisibilityRestriction { get; }
}

public class NamespaceFunctionParameter
{
    [JsonConstructor]
    public NamespaceFunctionParameter(string name, ITypeReference type, string? description, NamespaceFunctionParameterFlags flags)
        => (Name, Type, Description, Flags) = (name, type, description, flags);

    public string Name { get; }

    public ITypeReference Type { get; }

    public string? Description { get; }

    public NamespaceFunctionParameterFlags Flags { get; }
}

[Flags]
public enum NamespaceFunctionFlags
{
    Default = 0,
    ExternalInput = 1 << 0
}

[Flags]
public enum NamespaceFunctionParameterFlags
{
    None = 0,
    Required = 1 << 0,
    Constant = 1 << 1
}

public enum NamespaceFunctionFileVisibilityRestriction
{
    Bicep,
    Bicepparam,
}

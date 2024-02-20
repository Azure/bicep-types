// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class FunctionType : TypeBase
{
    [JsonConstructor]
    public FunctionType(IReadOnlyList<FunctionParameter> parameters, ITypeReference output)
        => (Parameters, Output) = (parameters, output);

    public IReadOnlyList<FunctionParameter> Parameters { get; }

    public ITypeReference Output { get; }
}

public class FunctionParameter
{
    [JsonConstructor]
    public FunctionParameter(string name, ITypeReference type, string? description)
        => (Name, Type, Description) = (name, type, description);

    public string Name { get; }

    public ITypeReference Type { get; }

    public string? Description { get; }
}
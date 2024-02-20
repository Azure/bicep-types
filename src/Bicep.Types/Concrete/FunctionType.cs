// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class FunctionType : TypeBase
{
    [JsonConstructor]
    public FunctionType(IReadOnlyList<FunctionTypeParameter> parameters, ITypeReference output)
        => (Parameters, Output) = (parameters, output);

    public IReadOnlyList<FunctionTypeParameter> Parameters { get; }

    public ITypeReference Output { get; }
}

public class FunctionTypeParameter
{
    [JsonConstructor]
    public FunctionTypeParameter(string name, ITypeReference type, string? description)
        => (Name, Type, Description) = (name, type, description);

    public string Name { get; }

    public ITypeReference Type { get; }

    public string? Description { get; }
}
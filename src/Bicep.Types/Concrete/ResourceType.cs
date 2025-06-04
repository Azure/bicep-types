// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace Azure.Bicep.Types.Concrete
{
    [Flags]
    public enum ResourceFlags
    {
        None = 0,
        ReadOnly = 1 << 0,
        WriteOnly = 1 << 0,
    }

    public class ResourceType : TypeBase
    {
        [JsonConstructor]
        public ResourceType(string name, ScopeType scopeType, ScopeType? readOnlyScopes, ITypeReference body, ResourceFlags flags, IReadOnlyDictionary<string, ResourceTypeFunction>? functions)
            => (Name, ScopeType, ReadOnlyScopes, Body, Flags, Functions) = (name, scopeType, readOnlyScopes, body, flags, functions);

        public string Name { get; }

        public ScopeType ScopeType { get; }

        public ScopeType? ReadOnlyScopes { get; }

        public ITypeReference Body { get; }

        public ResourceFlags Flags { get; }

        public IReadOnlyDictionary<string, ResourceTypeFunction>? Functions { get; }
    }
}
public class ResourceTypeFunction
{
    [JsonConstructor]
    public ResourceTypeFunction(ITypeReference type, string? description)
        => (Type, Description) = (type, description);

    public ITypeReference Type { get; }

    public string? Description { get; }
}
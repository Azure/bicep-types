// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    [Flags]
    public enum ResourceFlags
    {
        None = 0,
        ReadOnly = 1 << 0,
    }

    public class ResourceType : TypeBase
    {
        [JsonConstructor]
        public ResourceType(string name, ScopeType scopeType, ScopeType? readOnlyScopes, ITypeReference body, ResourceFlags flags, IReadOnlyList<ITypeReference>? functions)
            => (Name, ScopeType, ReadOnlyScopes, Body, Flags, Functions) = (name, scopeType, readOnlyScopes, body, flags, functions);

        public string Name { get; }

        public ScopeType ScopeType { get; }

        public ScopeType? ReadOnlyScopes { get; }

        public ITypeReference Body { get; }

        public ResourceFlags Flags { get; }

        public IReadOnlyList<ITypeReference>? Functions { get; }
    }
}

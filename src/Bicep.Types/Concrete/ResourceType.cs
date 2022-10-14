// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
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
        public ResourceType(string name, ScopeType scopeType, ScopeType? readOnlyScopes, ITypeReference body, ResourceFlags flags)
            => (Name, ScopeType, ReadOnlyScopes, Body, Flags) = (name, scopeType, readOnlyScopes, body, flags);

        public string Name { get; }

        public ScopeType ScopeType { get; }

        public ScopeType? ReadOnlyScopes { get; }

        public ITypeReference Body { get; }

        public ResourceFlags Flags { get; }
    }
}

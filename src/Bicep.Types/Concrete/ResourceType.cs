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
        ReadOnly = 1 << 0
    }

    public class ResourceType : TypeBase
    {
        [JsonConstructor]
        public ResourceType(
            string name, 
            ScopeType scopeType, 
            ScopeType? readOnlyScopes, 
            ITypeReference body, 
            ResourceFlags flags, 
            IReadOnlyDictionary<string, ResourceTypeFunction>? functions, 
            ScopeType? writableScopes = null, 
            ScopeType? readableScopes = null)
        {
            // Check for illegal mixing of legacy and new scope fields
            bool hasLegacy = scopeType != ScopeType.Unknown ||
                 (readOnlyScopes.HasValue && readOnlyScopes.Value != ScopeType.Unknown) ||
                 flags != ResourceFlags.None;

            bool hasModern =
                (writableScopes.HasValue && writableScopes.Value != ScopeType.Unknown) ||
                (readableScopes.HasValue && readableScopes.Value != ScopeType.Unknown);

            if (hasLegacy && hasModern)
            {
                throw new ArgumentException("Cannot supply both legacy scope fields (scopeType, readOnlyScopes, flags) and modern fields (writableScopes, readableScopes).");
            }

            Name = name;
            Body = body;
            Functions = functions;

            this.ScopeType = hasLegacy ? scopeType : ScopeType.Unknown;
            this.ReadOnlyScopes = hasLegacy ? readOnlyScopes : null;
            this.Flags = hasLegacy ? flags : ResourceFlags.None;

            this.WritableScopes = hasModern && writableScopes.HasValue && writableScopes.Value != ScopeType.Unknown
                ? writableScopes.Value
                : null;
            this.ReadableScopes = hasModern && readableScopes.HasValue && readableScopes.Value != ScopeType.Unknown
                ? readableScopes.Value
                : null;
        }

        public string Name { get; }

        public ScopeType ScopeType { get; }

        public ScopeType? ReadOnlyScopes { get; }

        public ITypeReference Body { get; }

        public ResourceFlags Flags { get; }

        public IReadOnlyDictionary<string, ResourceTypeFunction>? Functions { get; }

        public ScopeType? ReadableScopes { get; }

        public ScopeType? WritableScopes { get; }
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
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
            ScopeType? scopeType, 
            ScopeType? readOnlyScopes, 
            ITypeReference body, 
            ResourceFlags? flags, 
            IReadOnlyDictionary<string, ResourceTypeFunction>? functions, 
            ScopeType? writableScopes = null, 
            ScopeType? readableScopes = null)
        {
            // Check for illegal mixing of legacy and new scope fields
            bool hasLegacy = scopeType.HasValue || readOnlyScopes.HasValue || (flags.HasValue && flags.Value != ResourceFlags.None);
            bool hasModern = writableScopes.HasValue || readableScopes.HasValue;

            if (hasLegacy && hasModern)
            {
                throw new ArgumentException("Cannot mix both legacy scope fields (scopeType, readOnlyScopes, flags) and modern fields (writableScopes, readableScopes).");
            }

            Name = name;
            Body = body;
            Functions = functions;

            // Legacy properties (kept for backward compatibility)
#pragma warning disable CS0618 
            this.ScopeType = scopeType;
            this.ReadOnlyScopes = readOnlyScopes;
            this.Flags = flags;
#pragma warning restore CS0618

            this.WritableScopes = writableScopes;
            this.ReadableScopes = readableScopes;

            if (!this.WritableScopes.HasValue && !scopeType.HasValue) {
                throw new ArgumentException("Must supply either 'writableScopes' or 'scopeType'.");
            }

            if (!this.ReadableScopes.HasValue && !scopeType.HasValue) {
                throw new ArgumentException("Must supply either 'readableScopes' or 'scopeType'.");
            }
        }

        public string Name { get; }

        public ITypeReference Body { get; }

        public IReadOnlyDictionary<string, ResourceTypeFunction>? Functions { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ScopeType? ReadableScopes { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ScopeType? WritableScopes { get; }

        // Legacy properties (kept for backward compatibility)
        [Obsolete("Use WritableScopes and ReadableScopes instead")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ScopeType? ScopeType { get; }

        [Obsolete("Use ReadableScopes instead")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ScopeType? ReadOnlyScopes { get; }

        [Obsolete("Use WritableScopes instead")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResourceFlags? Flags { get; }
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
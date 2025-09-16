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
            ITypeReference body, 
            IReadOnlyDictionary<string, ResourceTypeFunction>? functions,
            ScopeType? writableScopes_in = null, 
            ScopeType? readableScopes_in = null,
            ScopeType? scopeType = null, 
            ScopeType? readOnlyScopes = null, 
            ResourceFlags? flags = null)
        {
            Name = name;
            Body = body;
            Functions = functions;

            // Check for illegal mixing of legacy and new scope fields
            bool hasLegacy = scopeType.HasValue || readOnlyScopes.HasValue || (flags.HasValue && flags.Value != ResourceFlags.None);
            bool hasModern = writableScopes_in.HasValue || readableScopes_in.HasValue;

            if (hasLegacy && hasModern)
            {
                throw new ArgumentException("Cannot mix both legacy scope fields (scopeType, readOnlyScopes, flags) and modern fields (writableScopes, readableScopes).");
            }

            if (hasModern)
            {
                // Use modern input directly
                if (!writableScopes_in.HasValue || !readableScopes_in.HasValue)
                {
                    throw new ArgumentException("Must set both WritableScopes and ReadableScopes when using modern configuration");
                }

                WritableScopes = writableScopes_in.Value;
                ReadableScopes = readableScopes_in.Value;
            }
            else
            {
                // Derive modern properties from legacy input (format normalization)
                var effectiveScopeType = scopeType ?? Azure.Bicep.Types.Concrete.ScopeType.None;

                // Legacy: 'Unknown' value (0) meant "all standard scopes"
                // Now the value 0 maps to ScopeType.None which is restrictive
                // To preserve legacy intent, if an explicit legacy scopeType of 0 is provided then interpret as AllExceptExtension
                if (scopeType.HasValue && scopeType.Value == Azure.Bicep.Types.Concrete.ScopeType.None)
                {
                    effectiveScopeType = Azure.Bicep.Types.Concrete.ScopeType.AllExceptExtension;
                }

                ReadableScopes = effectiveScopeType;
                if (readOnlyScopes.HasValue)
                {
                    ReadableScopes = ReadableScopes | readOnlyScopes.Value;
                }

                if (flags.HasValue && flags.Value.HasFlag(ResourceFlags.ReadOnly))
                {
                    WritableScopes = Azure.Bicep.Types.Concrete.ScopeType.None;
                }
                else
                {
                    WritableScopes = effectiveScopeType;
                }

            }
        }

        public string Name { get; }

        public ITypeReference Body { get; }

        public IReadOnlyDictionary<string, ResourceTypeFunction>? Functions { get; }

        [JsonIgnore]
        public ScopeType ReadableScopes { get; }

        [JsonIgnore]
        public ScopeType WritableScopes { get; }

        // Proxy properties for JSON serialization (format normalization)
        [JsonPropertyName("readableScopes")]
        public ScopeType? ReadableScopes_in => ReadableScopes;

        [JsonPropertyName("writableScopes")]
        public ScopeType? WritableScopes_in => WritableScopes;

        // Legacy properties which are never serialized, only used for deserialization
        [Obsolete("Use WritableScopes and ReadableScopes instead.")]
        [JsonPropertyName("scopeType")]
        public ScopeType? ScopeType => null; // Always null to prevent serialization

        [Obsolete("Use ReadableScopes instead.")]
        [JsonPropertyName("readOnlyScopes")]
        public ScopeType? ReadOnlyScopes => null;

        [Obsolete("Use WritableScopes instead.")]
        [JsonPropertyName("flags")]
        public ResourceFlags? Flags => null;
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
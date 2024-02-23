// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;

namespace Azure.Bicep.Types.Index
{
    public class TypeIndex
    {
        public TypeIndex(
            IReadOnlyDictionary<string, CrossFileTypeReference> resources,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>> resourceFunctions,
            TypeSettings? settings,
            CrossFileTypeReference? fallbackResourceType)
        {
            Resources = resources;
            ResourceFunctions = resourceFunctions;
            Settings = settings;
            FallbackResourceType = fallbackResourceType;
        }

        /// <summary>
        /// Available resource types, indexed by resource type name.
        /// </summary>
        public IReadOnlyDictionary<string, CrossFileTypeReference> Resources { get; }

        /// <summary>
        /// Available resource function types, indexed by resource type -> api version.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>> ResourceFunctions { get; }

        /// <summary>
        /// Provider settings such as name, version, isSingleton and configurationType.
        /// </summary>
        public TypeSettings? Settings { get; }

        /// <summary>
        /// If inputted type not recognized for provider, will default to fallbackType.
        /// </summary>
        public CrossFileTypeReference? FallbackResourceType { get; }
    }
}

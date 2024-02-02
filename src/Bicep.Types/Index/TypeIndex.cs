// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;

namespace Azure.Bicep.Types.Index
{
    public class TypeIndex
    {
        public TypeIndex(
            IReadOnlyDictionary<string, TypeLocation> resources,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<TypeLocation>>> functions,
            TypeSettings settings,
            TypeLocation fallbackResourceType)
        {
            Resources = resources;
            Functions = functions;
            Settings = settings;
            FallbackResourceType = fallbackResourceType;
        }

        /// <summary>
        /// Available resource types, indexed by resource type name.
        /// </summary>
        public IReadOnlyDictionary<string, TypeLocation> Resources { get; }

        /// <summary>
        /// Available resource function types, indexed by resource type -> api version.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<TypeLocation>>> Functions { get; }

        /// <summary>
        /// Provider settings such as name, version, isSingleton and configuration.
        /// </summary>
        public TypeSettings? Settings { get; }

        /// <summary>
        /// If inputted type not recognized for provider, will default to fallbackType.
        /// </summary>
        public TypeLocation? FallbackResourceType { get; }
    }
}

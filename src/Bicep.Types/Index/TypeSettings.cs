// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Index
{
    public class TypeSettings
    {
        [JsonConstructor]
        public TypeSettings(string name, string version, bool isSingleton, bool? isPreview = false, bool? isDeprecated = false, CrossFileTypeReference? configurationType = null)
        {
            this.Name = name;
            this.Version = version;
            this.IsSingleton = isSingleton;
            this.IsPreview = isPreview;
            this.IsDeprecated = isDeprecated;
            this.ConfigurationType = configurationType;
        }

        public string Name { get; }

        public string Version { get; }

        public bool IsSingleton { get; }

        public bool? IsPreview { get; }

        public bool? IsDeprecated { get; }

        public CrossFileTypeReference? ConfigurationType { get; }
    }
}

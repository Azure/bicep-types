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
        public TypeSettings(string name, string version, bool isSingleton, TypeLocation configurationType)
            => (Name, Version, IsSingleton, Configuration) = (name, version, isSingleton, configurationType);

        public string Name { get; }

        public string Version { get; }

        public bool IsSingleton { get; }

        public TypeLocation Configuration { get; }
    }
}

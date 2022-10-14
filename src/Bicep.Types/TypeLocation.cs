// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Bicep.Types
{
    public class TypeLocation
    {
        [JsonConstructor]
        public TypeLocation(string relativePath, int index)
            => (RelativePath, Index) = (relativePath, index);

        public string RelativePath { get; }

        public int Index { get; }
    }
}
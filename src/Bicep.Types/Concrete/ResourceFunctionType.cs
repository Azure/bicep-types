// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public class ResourceFunctionType : TypeBase
    {
        [JsonConstructor]
        public ResourceFunctionType(string name, string resourceType, string apiVersion, ITypeReference output, ITypeReference? input)
            => (Name, ResourceType, ApiVersion, Output, Input) = (name, resourceType, apiVersion, output, input);

        public string Name { get; }
        
        public string ResourceType { get; }
        
        public string ApiVersion { get; }
        
        public ITypeReference Output { get; }

        public ITypeReference? Input { get; }
    }
}
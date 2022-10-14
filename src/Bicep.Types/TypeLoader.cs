// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.IO;
using System.IO.Compression;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Concrete;
using System.Reflection;
using Azure.Bicep.Types.Serialization;

namespace Azure.Bicep.Types
{
    public abstract class TypeLoader : ITypeLoader
    {
        private const string TypeContainerName = "types.json";
        private const string TypeIndexResourceName = "index.json";

        public ResourceType LoadResourceType(TypeLocation typeLocation)
        {
            if (LoadType(typeLocation) is not ResourceType resourceType)
            {
                throw new ArgumentException($"Unable to locate resource type at index {typeLocation.Index} in \"{typeLocation.RelativePath}\" resource");
            }

            return resourceType;
        }

        public ResourceFunctionType LoadResourceFunctionType(TypeLocation typeLocation)
        {
            if (LoadType(typeLocation) is not ResourceFunctionType resourceFunctionType)
            {
                throw new ArgumentException($"Unable to locate resource function type at index {typeLocation.Index} in \"{typeLocation.RelativePath}\" resource");
            }

            return resourceFunctionType;
        }

        public TypeIndex LoadTypeIndex()
        {
            using var contentStream = GetContentStreamAtPath(TypeIndexResourceName);

            return TypeSerializer.DeserializeIndex(contentStream);
        }

        private TypeBase LoadType(TypeLocation typeLocation)
        {
            using var contentStream = GetContentStreamAtPath(typeLocation.RelativePath);
            var types = TypeSerializer.Deserialize(contentStream);

            return types[typeLocation.Index];
        }

        protected abstract Stream GetContentStreamAtPath(string path);
    }
}
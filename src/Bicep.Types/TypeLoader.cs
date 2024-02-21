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
        private const string TypeIndexResourceName = "index.json";

        public ResourceType LoadResourceType(CrossFileTypeReference reference)
        {
            if (LoadType(reference) is not ResourceType resourceType)
            {
                throw new ArgumentException($"Unable to locate resource type at index {reference.Index} in \"{reference.RelativePath}\" resource");
            }

            return resourceType;
        }

        public ResourceFunctionType LoadResourceFunctionType(CrossFileTypeReference reference)
        {
            if (LoadType(reference) is not ResourceFunctionType resourceFunctionType)
            {
                throw new ArgumentException($"Unable to locate resource function type at index {reference.Index} in \"{reference.RelativePath}\" resource");
            }

            return resourceFunctionType;
        }

        public ObjectType LoadObjectType(CrossFileTypeReference reference)
        {
            if (LoadType(reference) is not ObjectType objectType)
            {
                throw new ArgumentException($"Unable to locate object type at index {reference.Index} in \"{reference.RelativePath}\" resource");
            }

            return objectType;
        }

        public TypeIndex LoadTypeIndex()
        {
            using var contentStream = GetContentStreamAtPath(TypeIndexResourceName);

            return TypeSerializer.DeserializeIndex(contentStream);
        }

        private TypeBase LoadType(CrossFileTypeReference reference)
        {
            using var contentStream = GetContentStreamAtPath(reference.RelativePath);
            var types = TypeSerializer.Deserialize(contentStream);

            return types[reference.Index];
        }

        protected abstract Stream GetContentStreamAtPath(string path);
    }
}
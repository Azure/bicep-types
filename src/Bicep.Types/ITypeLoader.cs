// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Concrete;

namespace Azure.Bicep.Types
{
    public interface ITypeLoader
    {
        ResourceType LoadResourceType(TypeLocation location);

        ResourceFunctionType LoadResourceFunctionType(TypeLocation location);

        TypeIndex LoadTypeIndex();
    }
}
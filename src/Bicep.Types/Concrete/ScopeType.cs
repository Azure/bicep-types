// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;

namespace Azure.Bicep.Types.Concrete
{
    [Flags]
    public enum ScopeType
    {
        None = 0,

        Tenant = 1 << 0,

        ManagementGroup = 1 << 1,

        Subscription = 1 << 2,

        ResourceGroup = 1 << 3,

        Extension = 1 << 4,

        AllExceptExtension = Tenant | ManagementGroup | Subscription | ResourceGroup,
    }
}
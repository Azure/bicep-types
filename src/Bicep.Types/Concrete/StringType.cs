// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class StringType : TypeBase
{
    [JsonConstructor]
    public StringType(bool? secure = null, long? minLength = null, long? maxLength = null, string? pattern = null)
    {
        Secure = secure;
        MinLength = minLength;
        MaxLength = maxLength;
        Pattern = pattern;
    }

    public bool? Secure { get; }

    public long? MinLength { get; }

    public long? MaxLength { get; }

    public string? Pattern { get; }
}

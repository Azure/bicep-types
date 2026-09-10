// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Reflection;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Diagnostics;

[TestClass]
public class TypeValidationDiagnosticCodesTests
{
    [TestMethod]
    public void Active_codes_are_unique_and_contiguous()
    {
        var codes = typeof(TypeValidationDiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .OrderBy(code => code)
            .ToArray();

        var expectedCodes = Enumerable.Range(1, 35)
            .Select(number => $"BCPVT{number:D3}");

        codes.Should().Equal(expectedCodes);
    }
}
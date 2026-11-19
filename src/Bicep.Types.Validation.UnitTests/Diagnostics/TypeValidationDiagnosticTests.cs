// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Diagnostics;

[TestClass]
public class TypeValidationDiagnosticTests
{
    [TestMethod]
    public void Diagnostics_sort_deterministically_by_path_line_column_and_code()
    {
        var typesLater = Diag("BCPVT200", path: "types.json", line: 5, column: 2);
        var typesLaterHigherCode = Diag("BCPVT201", path: "types.json", line: 5, column: 2);
        var indexFirst = Diag("BCPVT100", path: "index.json", line: 1, column: 1);

        var list = new List<TypeValidationDiagnostic> { typesLater, typesLaterHigherCode, indexFirst };
        list.Sort(TypeValidationDiagnosticComparer.Instance);

        list.Should().ContainInOrder(indexFirst, typesLater, typesLaterHigherCode);
    }

    [TestMethod]
    public void Input_level_diagnostics_sort_before_file_level_diagnostics()
    {
        var inputLevel = Diag("BCPVT001", path: null);
        var fileLevel = Diag("BCPVT100", path: "index.json", line: 1, column: 1);

        var list = new List<TypeValidationDiagnostic> { fileLevel, inputLevel };
        list.Sort(TypeValidationDiagnosticComparer.Instance);

        list[0].Should().BeSameAs(inputLevel);
        list[1].Should().BeSameAs(fileLevel);
    }

    [TestMethod]
    public void Related_locations_are_preserved()
    {
        var related = new TypeValidationDiagnosticRelatedLocation(
            message: "declared here",
            path: "types.json",
            jsonPointer: "/0",
            line: 3,
            column: 1);

        var diagnostic = new TypeValidationDiagnostic(
            "BCPVT400",
            TypeValidationDiagnosticSeverity.Error,
            "wrong target kind",
            path: "index.json",
            relatedLocations: new[] { related });

        diagnostic.RelatedLocations.Should().ContainSingle()
            .Which.Message.Should().Be("declared here");
    }

    [TestMethod]
    public void Diagnostic_defaults_to_no_related_locations()
    {
        var diagnostic = new TypeValidationDiagnostic(
            "BCPVT100",
            TypeValidationDiagnosticSeverity.Error,
            "message");

        diagnostic.RelatedLocations.Should().BeEmpty();
    }

    private static TypeValidationDiagnostic Diag(string code, string? path, int? line = null, int? column = null)
        => new(code, TypeValidationDiagnosticSeverity.Error, $"message for {code}", path: path, line: line, column: column);
}

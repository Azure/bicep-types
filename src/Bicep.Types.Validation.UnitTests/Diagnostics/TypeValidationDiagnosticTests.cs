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
    public void Missing_path_sorts_before_empty_path_within_input_level_scope()
    {
        var missingPath = Diag("BCPVT100", path: null);
        var emptyPath = Diag("BCPVT100", path: string.Empty);

        TypeValidationDiagnosticComparer.Instance.Compare(missingPath, emptyPath).Should().BeLessThan(0);
        TypeValidationDiagnosticComparer.Instance.Compare(emptyPath, missingPath).Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void Missing_json_pointer_sorts_before_document_root_and_child_locations()
    {
        var missingPointer = Diag("BCPVT100", path: "types.json", jsonPointer: null);
        var documentRoot = Diag("BCPVT100", path: "types.json", jsonPointer: string.Empty);
        var childLocation = Diag("BCPVT100", path: "types.json", jsonPointer: "/0");

        TypeValidationDiagnosticComparer.Instance.Compare(missingPointer, documentRoot).Should().BeLessThan(0);
        TypeValidationDiagnosticComparer.Instance.Compare(documentRoot, missingPointer).Should().BeGreaterThan(0);
        TypeValidationDiagnosticComparer.Instance.Compare(documentRoot, childLocation).Should().BeLessThan(0);
        TypeValidationDiagnosticComparer.Instance.Compare(childLocation, documentRoot).Should().BeGreaterThan(0);
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

    private static TypeValidationDiagnostic Diag(
        string code,
        string? path,
        string? jsonPointer = null,
        int? line = null,
        int? column = null)
        => new(
            code,
            TypeValidationDiagnosticSeverity.Error,
            $"message for {code}",
            path: path,
            jsonPointer: jsonPointer,
            line: line,
            column: column);
}

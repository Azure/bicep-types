// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

/// <summary>
/// Builds a deterministic coverage report over the validation sample corpus: which diagnostic
/// codes each baseline produces (keyed by scenario, mode, and severity), and counts by category,
/// mode, and input kind, plus simple corpus gaps. The report is derived only from observable
/// scenario metadata and checked-in expected result baselines; it does not run the validator.
/// </summary>
public static class ValidationSampleCoverageReport
{
    /// <summary>Categories the corpus is expected to cover (see phase-8 plan §6.1).</summary>
    public static readonly IReadOnlyList<string> KnownCategories = new[]
    {
        "valid.canonical",
        "valid.input-forms",
        "structural",
        "invalid.graph",
        "invalid.semantic",
        "invalid.policy",
        "invalid.archive",
        "invalid.hygiene",
        "compatibility",
        "diagnostic-quality",
    };

    /// <summary>One diagnostic-code occurrence in a baseline, with its mode and severity context.</summary>
    public readonly struct DiagnosticCoverageRow
    {
        public DiagnosticCoverageRow(string code, string scenario, string mode, string severity)
        {
            Code = code;
            Scenario = scenario;
            Mode = mode;
            Severity = severity;
        }

        public string Code { get; }
        public string Scenario { get; }
        public string Mode { get; }
        public string Severity { get; }
    }

    /// <summary>The full coverage model, with deterministically ordered rows and counts.</summary>
    public sealed class CoverageModel
    {
        public CoverageModel(
            IReadOnlyList<DiagnosticCoverageRow> diagnosticRows,
            IReadOnlyList<KeyValuePair<string, int>> categoryCounts,
            IReadOnlyList<KeyValuePair<string, int>> modeCounts,
            IReadOnlyList<KeyValuePair<string, int>> inputKindCounts,
            IReadOnlyList<string> gaps)
        {
            DiagnosticRows = diagnosticRows;
            CategoryCounts = categoryCounts;
            ModeCounts = modeCounts;
            InputKindCounts = inputKindCounts;
            Gaps = gaps;
        }

        public IReadOnlyList<DiagnosticCoverageRow> DiagnosticRows { get; }
        public IReadOnlyList<KeyValuePair<string, int>> CategoryCounts { get; }
        public IReadOnlyList<KeyValuePair<string, int>> ModeCounts { get; }
        public IReadOnlyList<KeyValuePair<string, int>> InputKindCounts { get; }
        public IReadOnlyList<string> Gaps { get; }
    }

    /// <summary>Builds the coverage model from discovered scenarios and their baselines.</summary>
    public static CoverageModel Build()
    {
        var scenarios = ValidationSampleData.EnumerateScenarios().ToList();

        var diagnosticRows = new List<DiagnosticCoverageRow>();
        var categoryCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var modeCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var inputKindCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var categoriesSeen = new HashSet<string>(StringComparer.Ordinal);
        var gaps = new List<string>();

        foreach (var scenario in scenarios)
        {
            var category = scenario.Category ?? "(none)";
            categoriesSeen.Add(category);
            Increment(categoryCounts, category);

            foreach (var input in scenario.Inputs)
            {
                Increment(inputKindCounts, input.Kind.ToString());
            }

            foreach (var mode in scenario.Modes)
            {
                Increment(modeCounts, mode);

                var expectedResource = ValidationSampleData.GetExpectedResultResourceName(scenario.ResourcePrefix, mode);
                if (!ValidationSampleData.ResourceExists(expectedResource))
                {
                    continue;
                }

                diagnosticRows.AddRange(ReadBaselineDiagnostics(expectedResource, scenario.Name, mode));
            }
        }

        // Corpus gap: a known category with no scenarios at all. This is a coverage-presence check,
        // not a validity assertion: it does not infer expected diagnostics from a scenario's
        // category. Per-scenario validity is enforced by the baseline-internal invariants in the
        // sample health tests, not here.
        foreach (var known in KnownCategories)
        {
            if (!categoriesSeen.Contains(known))
            {
                gaps.Add($"category '{known}' has no scenarios.");
            }
        }

        diagnosticRows.Sort(CompareDiagnosticRows);
        gaps.Sort(StringComparer.Ordinal);

        return new CoverageModel(
            diagnosticRows,
            categoryCounts.ToList(),
            modeCounts.ToList(),
            inputKindCounts.ToList(),
            gaps);
    }

    /// <summary>Renders the coverage model as deterministic Markdown.</summary>
    public static string ToMarkdown(CoverageModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Validation Sample Coverage");
        sb.AppendLine();

        sb.AppendLine("## By Diagnostic Code");
        sb.AppendLine();
        sb.AppendLine("| Code | Scenario | Mode | Severity |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var row in model.DiagnosticRows)
        {
            sb.AppendLine(FormattableString.Invariant($"| {row.Code} | {row.Scenario} | {row.Mode} | {row.Severity} |"));
        }
        sb.AppendLine();

        AppendCountSection(sb, "By Category", "Category", model.CategoryCounts);
        AppendCountSection(sb, "By Mode", "Mode", model.ModeCounts);
        AppendCountSection(sb, "By Input Kind", "Input Kind", model.InputKindCounts);

        sb.AppendLine("## Corpus Gaps");
        sb.AppendLine();
        if (model.Gaps.Count == 0)
        {
            sb.AppendLine("None.");
        }
        else
        {
            sb.AppendLine("| Gap |");
            sb.AppendLine("| --- |");
            foreach (var gap in model.Gaps)
            {
                sb.AppendLine(FormattableString.Invariant($"| {gap} |"));
            }
        }
        sb.AppendLine();

        return sb.ToString();
    }

    private static void AppendCountSection(
        StringBuilder sb, string title, string header, IReadOnlyList<KeyValuePair<string, int>> counts)
    {
        sb.AppendLine(FormattableString.Invariant($"## {title}"));
        sb.AppendLine();
        sb.AppendLine(FormattableString.Invariant($"| {header} | Count |"));
        sb.AppendLine("| --- | --- |");
        foreach (var pair in counts)
        {
            sb.AppendLine(FormattableString.Invariant($"| {pair.Key} | {pair.Value} |"));
        }
        sb.AppendLine();
    }

    private static IReadOnlyList<DiagnosticCoverageRow> ReadBaselineDiagnostics(
        string expectedResource, string scenarioName, string mode)
    {
        using var document = JsonDocument.Parse(ValidationSampleData.ReadResource(expectedResource));
        var rows = new List<DiagnosticCoverageRow>();

        if (document.RootElement.TryGetProperty("diagnostics", out var diagnostics)
            && diagnostics.ValueKind == JsonValueKind.Array)
        {
            foreach (var diagnostic in diagnostics.EnumerateArray())
            {
                var code = diagnostic.TryGetProperty("code", out var codeElement)
                    ? codeElement.GetString() ?? "(none)"
                    : "(none)";
                var severity = diagnostic.TryGetProperty("severity", out var severityElement)
                    ? severityElement.GetString() ?? "(none)"
                    : "(none)";

                rows.Add(new DiagnosticCoverageRow(code, scenarioName, mode, severity));
            }
        }

        return rows;
    }

    private static int CompareDiagnosticRows(DiagnosticCoverageRow x, DiagnosticCoverageRow y)
    {
        var byCode = string.CompareOrdinal(x.Code, y.Code);
        if (byCode != 0) { return byCode; }

        var byScenario = string.CompareOrdinal(x.Scenario, y.Scenario);
        if (byScenario != 0) { return byScenario; }

        var byMode = string.CompareOrdinal(x.Mode, y.Mode);
        if (byMode != 0) { return byMode; }

        return string.CompareOrdinal(x.Severity, y.Severity);
    }

    private static void Increment(IDictionary<string, int> counts, string key)
    {
        counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;
    }
}

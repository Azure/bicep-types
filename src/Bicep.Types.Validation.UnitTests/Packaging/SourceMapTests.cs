// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class SourceMapTests
{
    // ── Root value location ─────────────────────────────────────────────────

    [TestMethod]
    public void Root_value_location_is_1_based()
    {
        byte[] json = Encoding.UTF8.GetBytes("{\"x\":1}");
        var sm = SourceMap.Create(json);
        var loc = sm.GetLocation(0);
        loc.Line.Should().Be(1);
        loc.Column.Should().Be(1);
    }

    // ── Object property locations ───────────────────────────────────────────

    [TestMethod]
    public void Object_property_value_location_is_available_via_byte_offset()
    {
        // {"x":42}
        // offset 0 = { (line 1 col 1)
        // offset 1 = " (start of "x")
        // offset 5 = 4 (start of 42)
        byte[] json = Encoding.UTF8.GetBytes("{\"x\":42}");
        SourceMap.TryParse(json, "test.json", out var root, out var sm, out _);
        root.Should().NotBeNull();
        root!.TryGetProperty("x", out var xNode).Should().BeTrue();
        var loc = sm.GetLocation(xNode.ByteOffset);
        loc.Line.Should().Be(1);
        loc.Column.Should().BePositive();
    }

    // ── Array element location ──────────────────────────────────────────────

    [TestMethod]
    public void Array_element_location_is_available()
    {
        byte[] json = Encoding.UTF8.GetBytes("[1, 2, 3]");
        SourceMap.TryParse(json, "test.json", out var root, out var sm, out _);
        root!.Elements.Count.Should().Be(3);
        var elem1Loc = sm.GetLocation(root.Elements[0].ByteOffset);
        var elem2Loc = sm.GetLocation(root.Elements[1].ByteOffset);
        elem1Loc.Column.Should().BeLessThan(elem2Loc.Column);
    }

    // ── Newline handling ────────────────────────────────────────────────────

    [TestMethod]
    public void LF_newline_resets_column_to_1_on_next_line()
    {
        byte[] json = Encoding.UTF8.GetBytes("{\n\"x\":1\n}");
        SourceMap.TryParse(json, "test.json", out var root, out var sm, out _);
        root!.TryGetProperty("x", out var xNode).Should().BeTrue();
        // "x" key is on line 2
        var keyProp = root.Properties[0];
        var keyLoc = sm.GetLocation(keyProp.NameByteOffset);
        keyLoc.Line.Should().Be(2);
        keyLoc.Column.Should().Be(1);
    }

    [TestMethod]
    public void CRLF_newline_treated_as_single_line_break()
    {
        byte[] json = Encoding.UTF8.GetBytes("{\r\n\"x\":1\r\n}");
        SourceMap.TryParse(json, "test.json", out var root, out var sm, out _);
        root!.TryGetProperty("x", out var xNode).Should().BeTrue();
        var keyLoc = sm.GetLocation(root.Properties[0].NameByteOffset);
        keyLoc.Line.Should().Be(2);
        keyLoc.Column.Should().Be(1);
    }

    // ── UTF-16 column counting ──────────────────────────────────────────────

    [TestMethod]
    public void Columns_are_1_based_utf16_code_units_including_non_ascii()
    {
        // Line with a 3-byte UTF-8 character (€ = U+20AC) before the property
        // "€": 1   → key starts at UTF-16 column 2 (after the "{")
        string line = "{\"" + "\u20AC" + "\":1}";
        byte[] json = Encoding.UTF8.GetBytes(line);
        SourceMap.TryParse(json, "test.json", out var root, out var sm, out _);
        root!.Properties.Count.Should().Be(1);
        // Key byte offset is 1 (the quote after {)
        var keyLoc = sm.GetLocation(root.Properties[0].NameByteOffset);
        keyLoc.Line.Should().Be(1);
        // Column should reflect UTF-16 units: "{" takes column 1, the quote is column 2
        keyLoc.Column.Should().Be(2);
    }

    // ── Single-pass build ───────────────────────────────────────────────────

    [TestMethod]
    public void TryParse_builds_value_nodes_and_source_map_in_one_call()
    {
        byte[] json = Encoding.UTF8.GetBytes("{\"a\":1,\"b\":\"hello\"}");
        var success = SourceMap.TryParse(json, "test.json", out var root, out var sm, out var err);
        success.Should().BeTrue();
        err.Should().BeNull();
        root.Should().NotBeNull();
        sm.Should().NotBeNull();
        root!.Properties.Count.Should().Be(2);
    }

    // ── Malformed JSON ──────────────────────────────────────────────────────

    [TestMethod]
    public void Malformed_json_returns_false_with_line_column()
    {
        byte[] json = Encoding.UTF8.GetBytes("{ invalid }");
        var success = SourceMap.TryParse(json, "test.json", out var root, out _, out var err);
        success.Should().BeFalse();
        root.Should().BeNull();
        err.Should().NotBeNull();
        err!.Value.line.Should().BeGreaterThan(0);
        err.Value.column.Should().BeGreaterThan(0);
    }

    // ── Trailing content after root value ───────────────────────────────────

    [TestMethod]
    public void Trailing_garbage_after_object_root_is_rejected()
    {
        byte[] json = Encoding.UTF8.GetBytes("{\"x\":1} garbage");
        var success = SourceMap.TryParse(json, "test.json", out var root, out _, out var err);
        success.Should().BeFalse();
        root.Should().BeNull();
        err.Should().NotBeNull();
    }

    [TestMethod]
    public void Second_top_level_value_after_array_root_is_rejected()
    {
        byte[] json = Encoding.UTF8.GetBytes("[1,2] [3,4]");
        var success = SourceMap.TryParse(json, "test.json", out var root, out _, out var err);
        success.Should().BeFalse();
        root.Should().BeNull();
        err.Should().NotBeNull();
    }

    [TestMethod]
    public void Trailing_whitespace_after_root_value_is_accepted()
    {
        byte[] json = Encoding.UTF8.GetBytes("{\"x\":1}\r\n   \n");
        var success = SourceMap.TryParse(json, "test.json", out var root, out _, out var err);
        success.Should().BeTrue();
        err.Should().BeNull();
        root.Should().NotBeNull();
    }
}

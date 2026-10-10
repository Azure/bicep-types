// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Helper that navigates a <see cref="JsonValueNode"/> tree and produces
    /// consistent structural diagnostics.  Carries the current document context
    /// so callers do not need to pass path/source-map to every check.
    /// </summary>
    internal sealed class JsonShapeReader
    {
        private readonly StructuralValidationContext context;

        public JsonShapeReader(StructuralValidationContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private PackageDocument Doc => context.CurrentDocument;
        private SourceMap SM => Doc.SourceMap;
        private string FilePath => Doc.PackageRelativePath;

        // ── Root-shape checks ────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if <paramref name="root"/> is a JSON object.</summary>
        public bool RequireRootObject(JsonValueNode root, out JsonValueNode obj)
        {
            if (root.Kind == JsonValueKind.Object) { obj = root; return true; }
            var loc = SM.GetLocation(root.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.IndexRootMustBeObject(FilePath, loc.Line, loc.Column));
            obj = root;
            return false;
        }

        /// <summary>Returns <c>true</c> if <paramref name="root"/> is a JSON array.</summary>
        public bool RequireRootArray(JsonValueNode root, out IReadOnlyList<JsonValueNode> elements)
        {
            if (root.Kind == JsonValueKind.Array) { elements = root.Elements; return true; }
            var loc = SM.GetLocation(root.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.TypeFileRootMustBeArray(FilePath, loc.Line, loc.Column));
            elements = root.Elements;
            return false;
        }

        // ── Property existence ───────────────────────────────────────────────────

        /// <summary>
        /// Looks up a required property.  Adds a <c>RequiredPropertyMissing</c> diagnostic on
        /// failure and returns <c>false</c>.  On success sets <paramref name="value"/> and returns
        /// <c>true</c>.  The containing-object location is used for missing-property diagnostics.
        /// </summary>
        public bool RequireProperty(JsonValueNode obj, string pointer, string name, out JsonValueNode value)
        {
            if (obj.TryGetProperty(name, out value)) { return true; }
            var loc = SM.GetLocation(obj.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.RequiredPropertyMissing(FilePath, pointer, name, loc.Line, loc.Column));
            return false;
        }

        // ── Type checks ──────────────────────────────────────────────────────────

        /// <summary>Checks that <paramref name="node"/> is a JSON string.  Returns <c>false</c> and adds diagnostic on failure.</summary>
        public bool RequireString(JsonValueNode node, string pointer, string name, out string value)
        {
            if (node.Kind == JsonValueKind.String && node.StringValue != null)
            {
                value = node.StringValue;
                return true;
            }
            var loc = SM.GetLocation(node.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.PropertyTypeMismatch(
                FilePath, pointer, name, "string", DescribeKind(node.Kind), loc.Line, loc.Column));
            value = string.Empty;
            return false;
        }

        /// <summary>Checks that <paramref name="node"/> is a JSON boolean.</summary>
        public bool RequireBool(JsonValueNode node, string pointer, string name, out bool value)
        {
            if (node.Kind == JsonValueKind.True) { value = true; return true; }
            if (node.Kind == JsonValueKind.False) { value = false; return true; }
            var loc = SM.GetLocation(node.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.PropertyTypeMismatch(
                FilePath, pointer, name, "boolean", DescribeKind(node.Kind), loc.Line, loc.Column));
            value = false;
            return false;
        }

        /// <summary>Checks that <paramref name="node"/> is a JSON integer (a number with no fractional part).</summary>
        public bool RequireInteger(JsonValueNode node, string pointer, string name, out long value)
        {
            if (node.Kind == JsonValueKind.Number && node.IsValidInt64)
            {
                value = node.Int64Value;
                return true;
            }
            var loc = SM.GetLocation(node.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.PropertyTypeMismatch(
                FilePath, pointer, name, "integer", DescribeKind(node.Kind), loc.Line, loc.Column));
            value = 0;
            return false;
        }

        /// <summary>Checks that <paramref name="node"/> is a JSON object.</summary>
        public bool RequireObject(JsonValueNode node, string pointer, string name)
        {
            if (node.Kind == JsonValueKind.Object) { return true; }
            var loc = SM.GetLocation(node.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.PropertyTypeMismatch(
                FilePath, pointer, name, "object", DescribeKind(node.Kind), loc.Line, loc.Column));
            return false;
        }

        /// <summary>Checks that <paramref name="node"/> is a JSON array.</summary>
        public bool RequireArray(JsonValueNode node, string pointer, string name, out IReadOnlyList<JsonValueNode> elements)
        {
            if (node.Kind == JsonValueKind.Array) { elements = node.Elements; return true; }
            var loc = SM.GetLocation(node.ByteOffset);
            context.Add(TypeValidationDiagnosticBuilder.PropertyTypeMismatch(
                FilePath, pointer, name, "array", DescribeKind(node.Kind), loc.Line, loc.Column));
            elements = node.Elements;
            return false;
        }

        // ── Optional property helpers ────────────────────────────────────────────

        /// <summary>
        /// Checks a field whose JSON type is determined by <paramref name="shape"/>.
        /// Called for each field descriptor in the type-shape catalog.
        /// </summary>
        public void CheckFieldShape(JsonValueNode fieldValue, string fieldPointer, TypeFieldDescriptor descriptor)
        {
            switch (descriptor.Shape)
            {
                case FieldShape.String:
                    RequireString(fieldValue, fieldPointer, descriptor.Name, out _);
                    break;
                case FieldShape.Bool:
                    // Accept null as intentional read-side leniency for bool? fields
                    if (fieldValue.Kind != JsonValueKind.Null)
                    {
                        RequireBool(fieldValue, fieldPointer, descriptor.Name, out _);
                    }
                    break;
                case FieldShape.Integer:
                    RequireInteger(fieldValue, fieldPointer, descriptor.Name, out _);
                    break;
                case FieldShape.Ref:
                    ReferenceSyntax.Validate(fieldValue, descriptor.Name, fieldPointer, context);
                    break;
                case FieldShape.ArrayOfRefs:
                    if (RequireArray(fieldValue, fieldPointer, descriptor.Name, out var refElements))
                    {
                        for (int i = 0; i < refElements.Count; i++)
                        {
                            ReferenceSyntax.Validate(refElements[i], descriptor.Name + "[" + i + "]", fieldPointer + "/" + i, context);
                        }
                    }
                    break;
                case FieldShape.ObjectMap:
                    RequireObject(fieldValue, fieldPointer, descriptor.Name);
                    break;
                case FieldShape.ArrayOfObjects:
                    if (RequireArray(fieldValue, fieldPointer, descriptor.Name, out var objElements))
                    {
                        for (int i = 0; i < objElements.Count; i++)
                        {
                            if (objElements[i].Kind != JsonValueKind.Object)
                            {
                                var loc = SM.GetLocation(objElements[i].ByteOffset);
                                context.Add(TypeValidationDiagnosticBuilder.PropertyTypeMismatch(
                                    FilePath, fieldPointer + "/" + i, descriptor.Name + "[" + i + "]",
                                    "object", DescribeKind(objElements[i].Kind), loc.Line, loc.Column));
                            }
                        }
                    }
                    break;
            }
        }

        // ── Location helper ──────────────────────────────────────────────────────

        public SourceLocation GetLocation(JsonValueNode node) => SM.GetLocation(node.ByteOffset);
        public SourceLocation GetKeyLocation(JsonProperty prop) => SM.GetLocation(prop.NameByteOffset);

        // ── String description helpers ───────────────────────────────────────────

        private static string DescribeKind(JsonValueKind kind) => JsonValueKindText.Describe(kind);
    }
}

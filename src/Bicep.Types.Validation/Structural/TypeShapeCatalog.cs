// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Descriptor for one supported type-object kind, including all field metadata.
    /// </summary>
    internal sealed class TypeKindDescriptor
    {
        public TypeKindDescriptor(string discriminator, TypeFieldDescriptor[] fields)
        {
            Discriminator = discriminator;
            Fields = fields ?? new TypeFieldDescriptor[0];
        }

        /// <summary>The <c>$type</c> discriminator string, e.g. <c>"ResourceType"</c>.</summary>
        public string Discriminator { get; }

        /// <summary>All known fields for this type kind (required and optional).</summary>
        public IReadOnlyList<TypeFieldDescriptor> Fields { get; }
    }

    /// <summary>
    /// Central catalog that maps <c>$type</c> discriminator values to their structural
    /// shape descriptors.  Matches the <c>[JsonDerivedType]</c> registrations on
    /// <c>TypeBase</c> exactly; intentionally excludes enum values such as <c>ScopeType</c>
    /// which are field values, not discriminators.
    /// </summary>
    internal static class TypeShapeCatalog
    {
        private static readonly Dictionary<string, TypeKindDescriptor> Catalog = BuildCatalog();

        private static readonly string[] AllKeys;

        static TypeShapeCatalog()
        {
            AllKeys = new string[Catalog.Count];
            Catalog.Keys.CopyTo(AllKeys, 0);
        }

        /// <summary>Returns the descriptor for a discriminator, or <c>null</c> if unknown.</summary>
        public static TypeKindDescriptor? GetDescriptor(string discriminator)
        {
            return Catalog.TryGetValue(discriminator, out var d) ? d : null;
        }

        /// <summary>All supported <c>$type</c> discriminator values.</summary>
        public static IReadOnlyList<string> AllDiscriminators => AllKeys;

        private static Dictionary<string, TypeKindDescriptor> BuildCatalog()
        {
            var catalog = new Dictionary<string, TypeKindDescriptor>();

            // Simple scalar types with no fields beyond $type
            Add(catalog, "AnyType");
            Add(catalog, "NullType");
            Add(catalog, "BooleanType");

            // IntegerType: optional minValue/maxValue
            Add(catalog, "IntegerType",
                Opt("minValue", FieldShape.Integer),
                Opt("maxValue", FieldShape.Integer));

            // StringType: optional constraints
            Add(catalog, "StringType",
                Opt("sensitive", FieldShape.Bool),
                Opt("minLength", FieldShape.Integer),
                Opt("maxLength", FieldShape.Integer),
                Opt("pattern", FieldShape.String));

            // StringLiteralType: required value
            Add(catalog, "StringLiteralType",
                Req("value", FieldShape.String));

            // ObjectType: required name + properties, optional additionalProperties + sensitive
            Add(catalog, "ObjectType",
                Req("name", FieldShape.String),
                Req("properties", FieldShape.ObjectMap),
                Opt("additionalProperties", FieldShape.Ref),
                Opt("sensitive", FieldShape.Bool));

            // ArrayType: required itemType, optional length constraints
            Add(catalog, "ArrayType",
                Req("itemType", FieldShape.Ref),
                Opt("minLength", FieldShape.Integer),
                Opt("maxLength", FieldShape.Integer));

            // UnionType: required elements array of refs
            Add(catalog, "UnionType",
                Req("elements", FieldShape.ArrayOfRefs));

            // DiscriminatedObjectType
            Add(catalog, "DiscriminatedObjectType",
                Req("name", FieldShape.String),
                Req("discriminator", FieldShape.String),
                Req("baseProperties", FieldShape.ObjectMap),
                Req("elements", FieldShape.ObjectMap));

            // FunctionType: required parameters (array of objects) and output (ref)
            Add(catalog, "FunctionType",
                Req("parameters", FieldShape.ArrayOfObjects),
                Req("output", FieldShape.Ref));

            // ResourceFunctionType
            Add(catalog, "ResourceFunctionType",
                Req("name", FieldShape.String),
                Req("resourceType", FieldShape.String),
                Req("apiVersion", FieldShape.String),
                Req("output", FieldShape.Ref),
                Opt("input", FieldShape.Ref));

            // NamespaceFunctionType
            Add(catalog, "NamespaceFunctionType",
                Req("name", FieldShape.String),
                Req("parameters", FieldShape.ArrayOfObjects),
                Req("outputType", FieldShape.Ref),
                Opt("description", FieldShape.String),
                Opt("evaluatedLanguageExpression", FieldShape.String),
                Opt("visibleInFileKind", FieldShape.Integer));

            // ResourceType: modern scope fields required; legacy scope fields are compat-only
            Add(catalog, "ResourceType",
                Req("name", FieldShape.String),
                Req("body", FieldShape.Ref),
                Req("readableScopes", FieldShape.Integer),
                Req("writableScopes", FieldShape.Integer),
                Opt("functions", FieldShape.ObjectMap),
                // legacy fields: accepted in CompatibleReader, rejected as unknown in CanonicalWriter
                Legacy("scopeType", FieldShape.Integer),
                Legacy("readOnlyScopes", FieldShape.Integer),
                Legacy("flags", FieldShape.Integer));

            // BuiltInType: required kind integer (enum values 1-8)
            Add(catalog, "BuiltInType",
                Req("kind", FieldShape.Integer));

            return catalog;
        }

        private static void Add(Dictionary<string, TypeKindDescriptor> catalog, string discriminator, params TypeFieldDescriptor[] fields)
        {
            catalog[discriminator] = new TypeKindDescriptor(discriminator, fields);
        }

        private static TypeFieldDescriptor Req(string name, FieldShape shape) =>
            new TypeFieldDescriptor(name, shape, required: true);

        private static TypeFieldDescriptor Opt(string name, FieldShape shape) =>
            new TypeFieldDescriptor(name, shape, required: false);

        private static TypeFieldDescriptor Legacy(string name, FieldShape shape) =>
            new TypeFieldDescriptor(name, shape, required: false, legacyCompatOnly: true);
    }
}

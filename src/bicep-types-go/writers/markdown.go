package writers

import (
	"fmt"
	"io"
	"sort"
	"strings"

	"github.com/Azure/bicep-types/src/bicep-types-go/index"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

// MarkdownWriter handles writing types and indices to Markdown format
type MarkdownWriter struct {
	includeTableOfContents bool
	resolver               TypeResolver
}

// TypeResolver interface for resolving type references
type TypeResolver interface {
	ResolveReference(ref types.ITypeReference) (types.Type, error)
}

// NewMarkdownWriter creates a new Markdown writer
func NewMarkdownWriter() *MarkdownWriter {
	return &MarkdownWriter{
		includeTableOfContents: true,
	}
}

// NewMarkdownWriterWithResolver creates a new Markdown writer with a type resolver
func NewMarkdownWriterWithResolver(resolver TypeResolver) *MarkdownWriter {
	return &MarkdownWriter{
		includeTableOfContents: true,
		resolver:               resolver,
	}
}

// SetIncludeTableOfContents sets whether to include a table of contents
func (w *MarkdownWriter) SetIncludeTableOfContents(include bool) {
	w.includeTableOfContents = include
}

// WriteTypes writes types to Markdown format
func (w *MarkdownWriter) WriteTypes(writer io.Writer, typesList []types.Type) error {
	if _, err := fmt.Fprintf(writer, "# Types\n\n"); err != nil {
		return err
	}

	// Group types by category
	var resourceTypes []*types.ResourceType
	var functionTypes []*types.FunctionType
	var resourceFunctionTypes []*types.ResourceFunctionType
	var objectTypes []*types.ObjectType
	var otherTypes []types.Type

	for _, typ := range typesList {
		switch v := typ.(type) {
		case *types.ResourceType:
			resourceTypes = append(resourceTypes, v)
		case *types.FunctionType:
			functionTypes = append(functionTypes, v)
		case *types.ResourceFunctionType:
			resourceFunctionTypes = append(resourceFunctionTypes, v)
		case *types.ObjectType:
			objectTypes = append(objectTypes, v)
		default:
			otherTypes = append(otherTypes, typ)
		}
	}

	// Write table of contents if enabled
	if w.includeTableOfContents {
		if err := w.writeTableOfContents(writer, resourceTypes, functionTypes, resourceFunctionTypes, objectTypes, otherTypes); err != nil {
			return err
		}
	}

	// Write resource types
	if len(resourceTypes) > 0 {
		if err := w.writeResourceTypes(writer, resourceTypes); err != nil {
			return err
		}
	}

	// Write function types
	if len(functionTypes) > 0 {
		if err := w.writeFunctionTypes(writer, functionTypes); err != nil {
			return err
		}
	}

	// Write resource function types
	if len(resourceFunctionTypes) > 0 {
		if err := w.writeResourceFunctionTypes(writer, resourceFunctionTypes); err != nil {
			return err
		}
	}

	// Write object types
	if len(objectTypes) > 0 {
		if err := w.writeObjectTypes(writer, objectTypes); err != nil {
			return err
		}
	}

	// Write other types
	if len(otherTypes) > 0 {
		if err := w.writeOtherTypes(writer, otherTypes); err != nil {
			return err
		}
	}

	return nil
}

// WriteTypeIndex writes a type index to Markdown format
func (w *MarkdownWriter) WriteTypeIndex(writer io.Writer, idx *index.TypeIndex) error {
	if _, err := fmt.Fprintf(writer, "# Type Index\n\n"); err != nil {
		return err
	}

	// Write resources section
	if len(idx.Resources) > 0 {
		if err := w.writeResourcesIndex(writer, idx.Resources); err != nil {
			return err
		}
	}

	// Write resource functions section
	if len(idx.ResourceFunctions) > 0 {
		if err := w.writeResourceFunctionsIndex(writer, idx.ResourceFunctions); err != nil {
			return err
		}
	}

	// Write settings if present
	if idx.Settings != nil {
		if err := w.writeSettings(writer, idx.Settings); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeTableOfContents(writer io.Writer, resourceTypes []*types.ResourceType, functionTypes []*types.FunctionType, resourceFunctionTypes []*types.ResourceFunctionType, objectTypes []*types.ObjectType, otherTypes []types.Type) error {
	if _, err := fmt.Fprintf(writer, "## Table of Contents\n\n"); err != nil {
		return err
	}

	if len(resourceTypes) > 0 {
		if _, err := fmt.Fprintf(writer, "- [Resource Types](#resource-types)\n"); err != nil {
			return err
		}
	}

	if len(functionTypes) > 0 {
		if _, err := fmt.Fprintf(writer, "- [Function Types](#function-types)\n"); err != nil {
			return err
		}
	}

	if len(resourceFunctionTypes) > 0 {
		if _, err := fmt.Fprintf(writer, "- [Resource Function Types](#resource-function-types)\n"); err != nil {
			return err
		}
	}

	if len(objectTypes) > 0 {
		if _, err := fmt.Fprintf(writer, "- [Object Types](#object-types)\n"); err != nil {
			return err
		}
	}

	if len(otherTypes) > 0 {
		if _, err := fmt.Fprintf(writer, "- [Other Types](#other-types)\n"); err != nil {
			return err
		}
	}

	if _, err := fmt.Fprintf(writer, "\n"); err != nil {
		return err
	}

	return nil
}

func (w *MarkdownWriter) writeResourceTypes(writer io.Writer, resourceTypes []*types.ResourceType) error {
	if _, err := fmt.Fprintf(writer, "## Resource Types\n\n"); err != nil {
		return err
	}

	// Sort by name
	sort.Slice(resourceTypes, func(i, j int) bool {
		return resourceTypes[i].Name < resourceTypes[j].Name
	})

	for _, rt := range resourceTypes {
		if err := w.writeResourceType(writer, rt); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeResourceType(writer io.Writer, rt *types.ResourceType) error {
	if _, err := fmt.Fprintf(writer, "### %s\n\n", rt.Name); err != nil {
		return err
	}

	// Parse resource name to extract parts (since Name now contains full resource name)
	// Example: "Microsoft.Storage/storageAccounts@2023-01-01"
	parts := strings.Split(rt.Name, "@")
	if len(parts) == 2 {
		resourceTypeId := parts[0]
		apiVersion := parts[1]

		if _, err := fmt.Fprintf(writer, "**Resource Type ID:** `%s`\n\n", resourceTypeId); err != nil {
			return err
		}

		if _, err := fmt.Fprintf(writer, "**API Version:** `%s`\n\n", apiVersion); err != nil {
			return err
		}
	}

	// Write scope information using the new fields
	if rt.ReadableScopes != types.ScopeTypeNone {
		if _, err := fmt.Fprintf(writer, "**Readable Scopes:** %s\n\n", w.formatScopeType(rt.ReadableScopes)); err != nil {
			return err
		}
	}

	if rt.WritableScopes != types.ScopeTypeNone {
		if _, err := fmt.Fprintf(writer, "**Writable Scopes:** %s\n\n", w.formatScopeType(rt.WritableScopes)); err != nil {
			return err
		}
	}

	// Write functions if present
	if len(rt.Functions) > 0 {
		if _, err := fmt.Fprintf(writer, "**Functions:**\n\n"); err != nil {
			return err
		}

		// Sort function names
		var funcNames []string
		for name := range rt.Functions {
			funcNames = append(funcNames, name)
		}
		sort.Strings(funcNames)

		for _, name := range funcNames {
			function := rt.Functions[name]
			if _, err := fmt.Fprintf(writer, "- `%s`", name); err != nil {
				return err
			}

			if function.Description != "" {
				if _, err := fmt.Fprintf(writer, ": %s", function.Description); err != nil {
					return err
				}
			}

			if _, err := fmt.Fprintf(writer, "\n"); err != nil {
				return err
			}
		}

		if _, err := fmt.Fprintf(writer, "\n"); err != nil {
			return err
		}
	}

	// Write body type information if resolver is available
	if w.resolver != nil && rt.Body != nil {
		if bodyType, err := w.resolver.ResolveReference(rt.Body); err == nil {
			if _, err := fmt.Fprintf(writer, "**Body Type:** `%s`\n\n", bodyType.Type()); err != nil {
				return err
			}
		}
	}

	if _, err := fmt.Fprintf(writer, "---\n\n"); err != nil {
		return err
	}

	return nil
}

func (w *MarkdownWriter) writeFunctionTypes(writer io.Writer, functionTypes []*types.FunctionType) error {
	if _, err := fmt.Fprintf(writer, "## Function Types\n\n"); err != nil {
		return err
	}

	for i, ft := range functionTypes {
		if err := w.writeFunctionType(writer, ft, i); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeFunctionType(writer io.Writer, ft *types.FunctionType, index int) error {
	if _, err := fmt.Fprintf(writer, "### Function Type %d\n\n", index); err != nil {
		return err
	}

	// FunctionType doesn't have Description field in the new structure
	// Remove the description section

	if len(ft.Parameters) > 0 {
		if _, err := fmt.Fprintf(writer, "**Parameters:**\n\n"); err != nil {
			return err
		}

		for _, param := range ft.Parameters {
			if _, err := fmt.Fprintf(writer, "- `%s`", param.Name); err != nil {
				return err
			}

			if param.Description != "" {
				if _, err := fmt.Fprintf(writer, ": %s", param.Description); err != nil {
					return err
				}
			}

			if _, err := fmt.Fprintf(writer, "\n"); err != nil {
				return err
			}
		}

		if _, err := fmt.Fprintf(writer, "\n"); err != nil {
			return err
		}
	}

	if _, err := fmt.Fprintf(writer, "**Output Type:** (type reference)\n\n"); err != nil {
		return err
	}

	if _, err := fmt.Fprintf(writer, "---\n\n"); err != nil {
		return err
	}

	return nil
}

func (w *MarkdownWriter) writeResourceFunctionTypes(writer io.Writer, resourceFunctionTypes []*types.ResourceFunctionType) error {
	if _, err := fmt.Fprintf(writer, "## Resource Function Types\n\n"); err != nil {
		return err
	}

	// Sort by resource type and name
	sort.Slice(resourceFunctionTypes, func(i, j int) bool {
		if resourceFunctionTypes[i].ResourceType != resourceFunctionTypes[j].ResourceType {
			return resourceFunctionTypes[i].ResourceType < resourceFunctionTypes[j].ResourceType
		}
		return resourceFunctionTypes[i].Name < resourceFunctionTypes[j].Name
	})

	for _, rft := range resourceFunctionTypes {
		if err := w.writeResourceFunctionType(writer, rft); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeResourceFunctionType(writer io.Writer, rft *types.ResourceFunctionType) error {
	if _, err := fmt.Fprintf(writer, "### %s\n\n", rft.Name); err != nil {
		return err
	}

	if _, err := fmt.Fprintf(writer, "**Resource Type:** `%s`\n\n", rft.ResourceType); err != nil {
		return err
	}

	if _, err := fmt.Fprintf(writer, "**API Version:** `%s`\n\n", rft.ApiVersion); err != nil { // Fixed: APIVersion -> ApiVersion
		return err
	}

	if rft.Input != nil {
		if _, err := fmt.Fprintf(writer, "**Input Type:** (type reference)\n\n"); err != nil {
			return err
		}
	}

	if _, err := fmt.Fprintf(writer, "**Output Type:** (type reference)\n\n"); err != nil {
		return err
	}

	if _, err := fmt.Fprintf(writer, "---\n\n"); err != nil {
		return err
	}

	return nil
}

func (w *MarkdownWriter) writeObjectTypes(writer io.Writer, objectTypes []*types.ObjectType) error {
	if _, err := fmt.Fprintf(writer, "## Object Types\n\n"); err != nil {
		return err
	}

	// Sort by name
	sort.Slice(objectTypes, func(i, j int) bool {
		return objectTypes[i].Name < objectTypes[j].Name
	})

	for _, ot := range objectTypes {
		if err := w.writeObjectType(writer, ot); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeObjectType(writer io.Writer, ot *types.ObjectType) error {
	name := ot.Name
	if name == "" {
		name = "Object Type"
	}

	if _, err := fmt.Fprintf(writer, "### %s\n\n", name); err != nil {
		return err
	}

	if len(ot.Properties) > 0 {
		if _, err := fmt.Fprintf(writer, "**Properties:**\n\n"); err != nil {
			return err
		}

		// Sort properties by name
		var propNames []string
		for propName := range ot.Properties {
			propNames = append(propNames, propName)
		}
		sort.Strings(propNames)

		for _, propName := range propNames {
			prop := ot.Properties[propName]
			if _, err := fmt.Fprintf(writer, "- `%s`", propName); err != nil {
				return err
			}

			if prop.Description != "" {
				if _, err := fmt.Fprintf(writer, ": %s", prop.Description); err != nil {
					return err
				}
			}

			// Add flags information
			if prop.Flags != types.TypePropertyFlagsNone {
				flagsStr := w.formatPropertyFlags(prop.Flags)
				if flagsStr != "" {
					if _, err := fmt.Fprintf(writer, " (%s)", flagsStr); err != nil {
						return err
					}
				}
			}

			if _, err := fmt.Fprintf(writer, "\n"); err != nil {
				return err
			}
		}

		if _, err := fmt.Fprintf(writer, "\n"); err != nil {
			return err
		}
	}

	if _, err := fmt.Fprintf(writer, "---\n\n"); err != nil {
		return err
	}

	return nil
}

func (w *MarkdownWriter) writeOtherTypes(writer io.Writer, otherTypes []types.Type) error {
	if _, err := fmt.Fprintf(writer, "## Other Types\n\n"); err != nil {
		return err
	}

	for i, t := range otherTypes {
		if _, err := fmt.Fprintf(writer, "### Type %d\n\n", i); err != nil {
			return err
		}

		if _, err := fmt.Fprintf(writer, "**Type:** `%s`\n\n", t.Type()); err != nil {
			return err
		}

		if _, err := fmt.Fprintf(writer, "---\n\n"); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeResourcesIndex(writer io.Writer, resources map[string]index.ResourceVersionMap) error {
	if _, err := fmt.Fprintf(writer, "## Resources\n\n"); err != nil {
		return err
	}

	// Sort resource names
	var resourceNames []string
	for name := range resources {
		resourceNames = append(resourceNames, name)
	}
	sort.Strings(resourceNames)

	for _, name := range resourceNames {
		versions := resources[name]

		if _, err := fmt.Fprintf(writer, "### %s\n\n", name); err != nil {
			return err
		}

		// Sort versions
		var versionNames []string
		for version := range versions {
			versionNames = append(versionNames, version)
		}
		sort.Strings(versionNames)

		if _, err := fmt.Fprintf(writer, "**API Versions:**\n\n"); err != nil {
			return err
		}

		for _, version := range versionNames {
			if _, err := fmt.Fprintf(writer, "- `%s`\n", version); err != nil {
				return err
			}
		}

		if _, err := fmt.Fprintf(writer, "\n---\n\n"); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeResourceFunctionsIndex(writer io.Writer, resourceFunctions map[string]index.ResourceFunctionVersionMap) error {
	if _, err := fmt.Fprintf(writer, "## Resource Functions\n\n"); err != nil {
		return err
	}

	// Sort resource names
	var resourceNames []string
	for name := range resourceFunctions {
		resourceNames = append(resourceNames, name)
	}
	sort.Strings(resourceNames)

	for _, name := range resourceNames {
		versionMap := resourceFunctions[name]

		if _, err := fmt.Fprintf(writer, "### %s\n\n", name); err != nil {
			return err
		}

		// Sort versions
		var versionNames []string
		for version := range versionMap {
			versionNames = append(versionNames, version)
		}
		sort.Strings(versionNames)

		for _, version := range versionNames {
			functionMap := versionMap[version]

			if _, err := fmt.Fprintf(writer, "**API Version %s:**\n\n", version); err != nil {
				return err
			}

			// Sort function names
			var functionNames []string
			for funcName := range functionMap {
				functionNames = append(functionNames, funcName)
			}
			sort.Strings(functionNames)

			for _, funcName := range functionNames {
				if _, err := fmt.Fprintf(writer, "- `%s`\n", funcName); err != nil {
					return err
				}
			}

			if _, err := fmt.Fprintf(writer, "\n"); err != nil {
				return err
			}
		}

		if _, err := fmt.Fprintf(writer, "---\n\n"); err != nil {
			return err
		}
	}

	return nil
}

func (w *MarkdownWriter) writeSettings(writer io.Writer, settings *index.TypeSettings) error {
	if _, err := fmt.Fprintf(writer, "## Settings\n\n"); err != nil {
		return err
	}

	if settings.ConfigurationType != nil {
		if _, err := fmt.Fprintf(writer, "**Configuration Type:** (type reference)\n\n"); err != nil {
			return err
		}
	}

	return nil
}

// Remove the old formatScopeTypes function and replace with:
func (w *MarkdownWriter) formatScopeType(scopeType types.ScopeType) string {
	var names []string

	if scopeType&types.ScopeTypeTenant != 0 {
		names = append(names, "Tenant")
	}
	if scopeType&types.ScopeTypeManagementGroup != 0 {
		names = append(names, "Management Group")
	}
	if scopeType&types.ScopeTypeSubscription != 0 {
		names = append(names, "Subscription")
	}
	if scopeType&types.ScopeTypeResourceGroup != 0 {
		names = append(names, "Resource Group")
	}
	if scopeType&types.ScopeTypeExtension != 0 {
		names = append(names, "Extension")
	}

	if len(names) == 0 {
		return "None"
	}

	return strings.Join(names, ", ")
}

func (w *MarkdownWriter) formatPropertyFlags(flags types.TypePropertyFlags) string {
	var flagNames []string

	if flags&types.TypePropertyFlagsRequired != 0 {
		flagNames = append(flagNames, "required")
	}
	if flags&types.TypePropertyFlagsReadOnly != 0 {
		flagNames = append(flagNames, "read-only")
	}
	if flags&types.TypePropertyFlagsWriteOnly != 0 {
		flagNames = append(flagNames, "write-only")
	}
	if flags&types.TypePropertyFlagsDeployTimeConstant != 0 {
		flagNames = append(flagNames, "deploy-time-constant")
	}
	if flags&types.TypePropertyFlagsIdentifier != 0 {
		flagNames = append(flagNames, "identifier")
	}

	return strings.Join(flagNames, ", ")
}

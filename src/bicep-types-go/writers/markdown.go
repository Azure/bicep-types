// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package writers

import (
	"fmt"
	"io"
	"regexp"
	"sort"
	"strings"

	"github.com/Azure/bicep-types/src/bicep-types-go/index"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

var anchorSanitizer = regexp.MustCompile(`[^a-zA-Z0-9-]`)

// MarkdownWriter emits types and type indexes in the same markdown format as the TypeScript implementation.
type MarkdownWriter struct{}

// NewMarkdownWriter creates a new Markdown writer.
func NewMarkdownWriter() *MarkdownWriter {
	return &MarkdownWriter{}
}

// SetIncludeTableOfContents retained for backwards compatibility; table of contents is not emitted by the TypeScript implementation.
func (w *MarkdownWriter) SetIncludeTableOfContents(bool) {}

// WriteTypes writes the provided types to markdown using the TypeScript formatting.
func (w *MarkdownWriter) WriteTypes(writer io.Writer, typesList []types.Type) error {
	content, err := generateTypesMarkdown(typesList, "Bicep Types")
	if err != nil {
		return err
	}

	_, err = io.WriteString(writer, content)
	return err
}

// WriteTypeIndex writes the provided type index to markdown using the TypeScript formatting.
func (w *MarkdownWriter) WriteTypeIndex(writer io.Writer, idx *index.TypeIndex, typeFiles []index.TypeFile) error {
	content := generateIndexMarkdown(idx, typeFiles)
	_, err := io.WriteString(writer, content)
	return err
}

type markdownBuilder struct {
	builder strings.Builder
}

func (m *markdownBuilder) writeHeading(nesting int, message string) {
	if nesting < 1 {
		nesting = 1
	}

	m.builder.WriteString(strings.Repeat("#", nesting))
	m.builder.WriteByte(' ')
	m.builder.WriteString(message)
	m.writeNewLine()
}

func (m *markdownBuilder) writeBullet(key, value string) {
	m.builder.WriteString("* **")
	m.builder.WriteString(key)
	m.builder.WriteString("**: ")
	m.builder.WriteString(value)
	m.writeNewLine()
}

func (m *markdownBuilder) writeNumbered(index int, key, value string) {
	m.builder.WriteString(fmt.Sprintf("%d. **%s**: %s", index, key, value))
	m.writeNewLine()
}

func (m *markdownBuilder) writeNotaBene(content string) {
	m.builder.WriteByte('*')
	m.builder.WriteString(content)
	m.builder.WriteByte('*')
	m.writeNewLine()
}

func (m *markdownBuilder) writeNewLine() {
	m.builder.WriteByte('\n')
}

func (m *markdownBuilder) generateAnchorLink(name string) string {
	return fmt.Sprintf("[%s](#%s)", name, anchorize(name))
}

func (m *markdownBuilder) String() string {
	return m.builder.String()
}

func anchorize(value string) string {
	return strings.ToLower(anchorSanitizer.ReplaceAllString(value, ""))
}

func generateTypesMarkdown(typesList []types.Type, fileHeading string) (string, error) {
	md := &markdownBuilder{}
	md.writeHeading(1, fileHeading)
	md.writeNewLine()

	var resourceTypes []*types.ResourceType
	var resourceFunctionTypes []*types.ResourceFunctionType
	var namespaceFunctionTypes []*types.NamespaceFunctionType

	for _, t := range typesList {
		switch concrete := t.(type) {
		case *types.ResourceType:
			resourceTypes = append(resourceTypes, concrete)
		case *types.ResourceFunctionType:
			resourceFunctionTypes = append(resourceFunctionTypes, concrete)
		case *types.NamespaceFunctionType:
			namespaceFunctionTypes = append(namespaceFunctionTypes, concrete)
		}
	}

	sort.SliceStable(resourceTypes, func(i, j int) bool {
		baseI := resourceBaseName(resourceTypes[i].Name)
		baseJ := resourceBaseName(resourceTypes[j].Name)
		if baseI == baseJ {
			return strings.ToLower(resourceTypes[i].Name) < strings.ToLower(resourceTypes[j].Name)
		}
		return baseI < baseJ
	})

	sort.SliceStable(resourceFunctionTypes, func(i, j int) bool {
		baseI := resourceBaseName(resourceFunctionTypes[i].Name)
		baseJ := resourceBaseName(resourceFunctionTypes[j].Name)
		if baseI == baseJ {
			return strings.ToLower(resourceFunctionTypes[i].Name) < strings.ToLower(resourceFunctionTypes[j].Name)
		}
		return baseI < baseJ
	})

	typesToWrite := make([]types.Type, 0)

	for _, resource := range resourceTypes {
		findTypesToWrite(typesList, &typesToWrite, resource.Body)
	}

	for _, fn := range resourceFunctionTypes {
		if fn.Input != nil {
			appendTypeForReference(typesList, &typesToWrite, fn.Input)
			findTypesToWrite(typesList, &typesToWrite, fn.Input)
		}

		appendTypeForReference(typesList, &typesToWrite, fn.Output)
		findTypesToWrite(typesList, &typesToWrite, fn.Output)
	}

	for _, nfn := range namespaceFunctionTypes {
		if nfn.Parameters != nil {
			for _, param := range nfn.Parameters {
				appendTypeForReference(typesList, &typesToWrite, param.Type)
				findTypesToWrite(typesList, &typesToWrite, param.Type)
			}
		}
		appendTypeForReference(typesList, &typesToWrite, nfn.OutputType)
		findTypesToWrite(typesList, &typesToWrite, nfn.OutputType)
	}

	deduped := dedupeTypes(typesToWrite)
	sortTypesByName(deduped)

	combined := make([]types.Type, 0, len(resourceTypes)+len(resourceFunctionTypes)+len(namespaceFunctionTypes)+len(deduped))
	for _, rt := range resourceTypes {
		combined = append(combined, rt)
	}
	for _, rft := range resourceFunctionTypes {
		combined = append(combined, rft)
	}
	for _, nft := range namespaceFunctionTypes {
		combined = append(combined, nft)
	}
	combined = append(combined, deduped...)

	for _, t := range combined {
		if err := writeComplexType(md, typesList, t, 2, true); err != nil {
			return "", err
		}
	}

	return md.String(), nil
}

func generateIndexMarkdown(idx *index.TypeIndex, typeFiles []index.TypeFile) string {
	md := &markdownBuilder{}
	md.writeHeading(1, "Bicep Types")

	if idx == nil || len(idx.Resources) == 0 {
		return md.String()
	}

	flattened := make(map[string]types.ITypeReference)
	for resourceType, versionMap := range idx.Resources {
		for version, ref := range versionMap {
			typeString := fmt.Sprintf("%s@%s", resourceType, version)
			flattened[typeString] = ref
		}
	}

	if len(flattened) == 0 {
		return md.String()
	}

	byProvider := make(map[string][]string)
	for typeString := range flattened {
		provider := providerKey(typeString)
		byProvider[provider] = append(byProvider[provider], typeString)
	}

	providerKeys := sortedKeys(byProvider)

	for _, provider := range providerKeys {
		md.writeHeading(2, provider)

		byResourceType := make(map[string][]string)
		for _, typeString := range byProvider[provider] {
			key := resourceTypeKey(typeString)
			byResourceType[key] = append(byResourceType[key], typeString)
		}

		resourceKeys := sortedKeys(byResourceType)
		for _, resource := range resourceKeys {
			md.writeHeading(3, resource)

			entries := byResourceType[resource]
			sort.SliceStable(entries, func(i, j int) bool {
				return strings.ToLower(entries[i]) < strings.ToLower(entries[j])
			})

			for _, typeString := range entries {
				version := versionFromTypeString(typeString)
				ref := flattened[typeString]
				mdPath := relativeMarkdownPath(ref)
				anchor := fmt.Sprintf("resource-%s", anchorize(typeString))
				md.writeBullet("Link", fmt.Sprintf("[%s](%s#%s)", version, mdPath, anchor))
			}

			md.writeNewLine()
		}
	}

	if len(idx.NamespaceFunctions) > 0 {
		md.writeHeading(2, "Namespace Functions")

		for _, ref := range idx.NamespaceFunctions {
			mdPath := relativeMarkdownPath(ref)

			// Find the function name from typeFiles if available
			functionName := "#unknown"
			for _, tf := range typeFiles {
				if tf.RelativePath == getRelativePath(ref) {
					refIndex := getRefIndex(ref)
					if refIndex >= 0 && refIndex < len(tf.Types) {
						if nft, ok := tf.Types[refIndex].(*types.NamespaceFunctionType); ok {
							functionName = nft.Name
						}
					}
					break
				}
			}

			// Generate anchor from heading text "Namespace Function <name>"
			headingText := fmt.Sprintf("Namespace Function %s", functionName)
			anchor := strings.ToLower(strings.ReplaceAll(headingText, " ", "-"))
			anchor = anchorize(anchor)

			md.writeHeading(3, functionName)
			md.writeBullet("Link", fmt.Sprintf("[%s](%s#%s)", functionName, mdPath, anchor))
		}

		md.writeNewLine()
	}

	return md.String()
}

func writeComplexType(md *markdownBuilder, typesList []types.Type, t types.Type, nesting int, includeHeader bool) error {
	switch concrete := t.(type) {
	case *types.ResourceType:
		md.writeHeading(nesting, fmt.Sprintf("Resource %s", concrete.Name))
		md.writeBullet("Readable Scope(s)", formatScopeList(concrete.ReadableScopes))
		md.writeBullet("Writable Scope(s)", formatScopeList(concrete.WritableScopes))

		if bodyType, ok := resolveTypeRef(typesList, concrete.Body); ok {
			if err := writeComplexType(md, typesList, bodyType, nesting, false); err != nil {
				return err
			}
		}

		if len(concrete.Functions) > 0 {
			functionNames := sortedKeys(concrete.Functions)
			for _, name := range functionNames {
				fn := concrete.Functions[name]
				if functionType, ok := resolveFunctionType(typesList, fn.Type); ok {
					if err := writeFunctionType(md, typesList, name, functionType, nesting+1); err != nil {
						return err
					}
				}
			}
		}
	case *types.ResourceFunctionType:
		md.writeHeading(nesting, fmt.Sprintf("Function %s (%s@%s)", concrete.Name, concrete.ResourceType, concrete.ApiVersion))
		md.writeBullet("Resource", concrete.ResourceType)
		md.writeBullet("ApiVersion", concrete.ApiVersion)

		if concrete.Input != nil {
			value, err := getTypeName(md, typesList, concrete.Input)
			if err != nil {
				return err
			}
			if value != "" {
				md.writeBullet("Input", value)
			}
		}

		output, err := getTypeName(md, typesList, concrete.Output)
		if err != nil {
			return err
		}
		md.writeBullet("Output", output)
		md.writeNewLine()
	case *types.NamespaceFunctionType:
		md.writeHeading(nesting, fmt.Sprintf("Namespace Function %s", concrete.Name))
		description := "(none)"
		if concrete.Description != "" {
			description = concrete.Description
		}
		md.writeBullet("Description", description)

		if concrete.EvaluatedLanguageExpression != "" {
			md.writeBullet("Evaluated language expression", fmt.Sprintf("`%s`", concrete.EvaluatedLanguageExpression))
		}

		if concrete.VisibleInFileKind != nil {
			md.writeBullet("Visible only in bicep file kind", formatBicepSourceFileKind(*concrete.VisibleInFileKind))
		}

		if len(concrete.Parameters) > 0 {
			md.writeHeading(nesting+1, "Parameters")
			for i, param := range concrete.Parameters {
				paramTypeName, err := getTypeName(md, typesList, param.Type)
				if err != nil {
					return err
				}
				value := paramTypeName
				if flags := formatNamespaceFunctionParameterFlags(param.Flags); flags != "" {
					value += fmt.Sprintf(" (%s)", flags)
				}
				if param.Description != "" {
					value += ": " + param.Description
				}
				md.writeNumbered(i+1, param.Name, value)
			}
		}

		outputTypeName, err := getTypeName(md, typesList, concrete.OutputType)
		if err != nil {
			return err
		}
		md.writeBullet("Output type", outputTypeName)
		md.writeNewLine()
	case *types.ObjectType:
		if includeHeader {
			md.writeHeading(nesting, concrete.Name)
		}

		if concrete.Sensitive != nil && *concrete.Sensitive {
			md.writeNotaBene("Sensitive")
		}

		md.writeHeading(nesting+1, "Properties")
		propertyNames := sortedKeys(concrete.Properties)
		for _, name := range propertyNames {
			if err := writeTypeProperty(md, typesList, name, concrete.Properties[name]); err != nil {
				return err
			}
		}

		if concrete.AdditionalProperties != nil {
			md.writeHeading(nesting+1, "Additional Properties")
			additional, err := getTypeName(md, typesList, concrete.AdditionalProperties)
			if err != nil {
				return err
			}
			if additional != "" {
				md.writeBullet("Additional Properties Type", additional)
			}
		}

		md.writeNewLine()
	case *types.DiscriminatedObjectType:
		if includeHeader {
			md.writeHeading(nesting, concrete.Name)
		}

		md.writeBullet("Discriminator", concrete.Discriminator)
		md.writeNewLine()

		md.writeHeading(nesting+1, "Base Properties")
		basePropertyNames := sortedKeys(concrete.BaseProperties)
		for _, name := range basePropertyNames {
			if err := writeTypeProperty(md, typesList, name, concrete.BaseProperties[name]); err != nil {
				return err
			}
		}

		md.writeNewLine()

		elementNames := sortedKeys(concrete.Elements)
		for _, elementName := range elementNames {
			if elementType, ok := resolveTypeRef(typesList, concrete.Elements[elementName]); ok {
				if err := writeComplexType(md, typesList, elementType, nesting+1, true); err != nil {
					return err
				}
			}
		}

		md.writeNewLine()
	}

	return nil
}

func writeFunctionType(md *markdownBuilder, typesList []types.Type, name string, functionType *types.FunctionType, nesting int) error {
	md.writeHeading(nesting, fmt.Sprintf("Function %s", name))

	output, err := getTypeName(md, typesList, functionType.Output)
	if err != nil {
		return err
	}
	md.writeBullet("Output", output)

	md.writeHeading(nesting+1, "Parameters")
	for index, parameter := range functionType.Parameters {
		value, err := getTypeName(md, typesList, parameter.Type)
		if err != nil {
			return err
		}
		md.writeNumbered(index, parameter.Name, value)
	}

	md.writeNewLine()
	return nil
}

func writeTypeProperty(md *markdownBuilder, typesList []types.Type, name string, property types.ObjectTypeProperty) error {
	value, err := getTypeName(md, typesList, property.Type)
	if err != nil {
		return err
	}

	if flags := formatPropertyFlags(property.Flags); flags != "" {
		value = fmt.Sprintf("%s (%s)", value, flags)
	}

	if description := strings.TrimSpace(property.Description); description != "" {
		value = fmt.Sprintf("%s: %s", value, description)
	}

	md.writeBullet(name, value)
	return nil
}

func getTypeName(md *markdownBuilder, typesList []types.Type, ref types.ITypeReference) (string, error) {
	switch concrete := ref.(type) {
	case types.TypeReference:
		return describeType(md, typesList, concrete.Ref)
	case *types.TypeReference:
		return describeType(md, typesList, concrete.Ref)
	case types.CrossFileTypeReference:
		return fmt.Sprintf("%s#/%d", concrete.RelativePath, concrete.Ref), nil
	case *types.CrossFileTypeReference:
		return fmt.Sprintf("%s#/%d", concrete.RelativePath, concrete.Ref), nil
	case nil:
		return "", nil
	default:
		return "", fmt.Errorf("unsupported type reference %T", ref)
	}
}

func describeType(md *markdownBuilder, typesList []types.Type, index int) (string, error) {
	if index < 0 || index >= len(typesList) {
		return "", fmt.Errorf("type reference %d out of bounds", index)
	}

	switch concrete := typesList[index].(type) {
	case *types.BuiltInType:
		return strings.ToLower(builtInTypeLabel(concrete.Kind)), nil
	case *types.ObjectType:
		return md.generateAnchorLink(concrete.Name), nil
	case *types.DiscriminatedObjectType:
		return md.generateAnchorLink(concrete.Name), nil
	case *types.ArrayType:
		itemName, err := getTypeName(md, typesList, concrete.ItemType)
		if err != nil {
			return "", err
		}
		if strings.Contains(itemName, " ") {
			itemName = fmt.Sprintf("(%s)", itemName)
		}
		modifiers := formatModifiers(lengthModifier("minLength", concrete.MinLength), lengthModifier("maxLength", concrete.MaxLength))
		return fmt.Sprintf("%s[]%s", itemName, modifiers), nil
	case *types.ResourceType:
		return concrete.Name, nil
	case *types.ResourceFunctionType:
		return fmt.Sprintf("%s (%s@%s)", concrete.Name, concrete.ResourceType, concrete.ApiVersion), nil
	case *types.UnionType:
		names := make([]string, 0, len(concrete.Elements))
		for _, element := range concrete.Elements {
			value, err := getTypeName(md, typesList, element)
			if err != nil {
				return "", err
			}
			names = append(names, value)
		}
		sort.Strings(names)
		return strings.Join(names, " | "), nil
	case *types.StringLiteralType:
		return fmt.Sprintf("'%s'", concrete.Value), nil
	case *types.AnyType:
		return "any", nil
	case *types.NullType:
		return "null", nil
	case *types.BooleanType:
		return "bool", nil
	case *types.IntegerType:
		modifiers := formatModifiers(integerModifier("minValue", concrete.MinValue), integerModifier("maxValue", concrete.MaxValue))
		return fmt.Sprintf("int%s", modifiers), nil
	case *types.StringType:
		modifiers := []string{}
		if concrete.Sensitive {
			modifiers = append(modifiers, "sensitive")
		}
		modifiers = append(modifiers, lengthModifier("minLength", concrete.MinLength))
		modifiers = append(modifiers, lengthModifier("maxLength", concrete.MaxLength))
		if concrete.Pattern != "" {
			escaped := strings.ReplaceAll(concrete.Pattern, "\"", "\\\"")
			modifiers = append(modifiers, fmt.Sprintf("pattern: \"%s\"", escaped))
		}
		return fmt.Sprintf("string%s", formatModifiers(modifiers...)), nil
	case *types.FunctionType:
		return "function", nil
	default:
		return "", fmt.Errorf("unrecognized type %T", concrete)
	}
}

func builtInTypeLabel(kind string) string {
	labels := map[string]string{
		"Any":         "Any",
		"Null":        "Null",
		"Bool":        "Bool",
		"Int":         "Int",
		"String":      "String",
		"Object":      "Object",
		"Array":       "Array",
		"ResourceRef": "ResourceRef",
	}

	if label, ok := labels[kind]; ok {
		return label
	}

	return kind
}

func formatModifiers(modifiers ...string) string {
	filtered := make([]string, 0, len(modifiers))
	for _, modifier := range modifiers {
		if modifier != "" {
			filtered = append(filtered, modifier)
		}
	}

	if len(filtered) == 0 {
		return ""
	}

	return fmt.Sprintf(" {%s}", strings.Join(filtered, ", "))
}

func lengthModifier(label string, value *int64) string {
	if value == nil {
		return ""
	}
	return fmt.Sprintf("%s: %d", label, *value)
}

func integerModifier(label string, value *int64) string {
	if value == nil {
		return ""
	}
	return fmt.Sprintf("%s: %d", label, *value)
}

func formatScopeList(scope types.ScopeType) string {
	labels := getScopeTypeLabels(scope)
	if len(labels) == 0 {
		return "None"
	}

	return strings.Join(labels, ", ")
}

func getScopeTypeLabels(scope types.ScopeType) []string {
	entries := []struct {
		flag  types.ScopeType
		label string
	}{
		{types.ScopeTypeTenant, "Tenant"},
		{types.ScopeTypeManagementGroup, "ManagementGroup"},
		{types.ScopeTypeSubscription, "Subscription"},
		{types.ScopeTypeResourceGroup, "ResourceGroup"},
		{types.ScopeTypeExtension, "Extension"},
	}

	result := make([]string, 0, len(entries))
	for _, entry := range entries {
		if scope&entry.flag == entry.flag {
			result = append(result, entry.label)
		}
	}

	return result
}

func formatBicepSourceFileKind(kind types.BicepSourceFileKind) string {
	switch kind {
	case types.BicepSourceFileKindBicepFile:
		return "BicepFile"
	case types.BicepSourceFileKindParamsFile:
		return "ParamsFile"
	default:
		return fmt.Sprintf("Unknown(%d)", int(kind))
	}
}

func formatNamespaceFunctionParameterFlags(flags types.NamespaceFunctionParameterFlags) string {
	labels := []struct {
		flag  types.NamespaceFunctionParameterFlags
		label string
	}{
		{types.NamespaceFunctionParameterFlagsRequired, "Required"},
		{types.NamespaceFunctionParameterFlagsCompileTimeConstant, "CompileTimeConstant"},
		{types.NamespaceFunctionParameterFlagsDeployTimeConstant, "DeployTimeConstant"},
	}

	names := make([]string, 0, len(labels))
	for _, entry := range labels {
		if flags&entry.flag == entry.flag {
			names = append(names, entry.label)
		}
	}

	return strings.Join(names, ", ")
}

func formatPropertyFlags(flags types.TypePropertyFlags) string {
	labels := []struct {
		flag  types.TypePropertyFlags
		label string
	}{
		{types.TypePropertyFlagsRequired, "Required"},
		{types.TypePropertyFlagsReadOnly, "ReadOnly"},
		{types.TypePropertyFlagsWriteOnly, "WriteOnly"},
		{types.TypePropertyFlagsDeployTimeConstant, "DeployTimeConstant"},
		{types.TypePropertyFlagsIdentifier, "Identifier"},
	}

	names := make([]string, 0, len(labels))
	for _, entry := range labels {
		if flags&entry.flag == entry.flag {
			names = append(names, entry.label)
		}
	}

	return strings.Join(names, ", ")
}

func findTypesToWrite(typesList []types.Type, typesToWrite *[]types.Type, ref types.ITypeReference) {
	findTypesToWriteInternal(typesList, typesToWrite, ref, make(map[int]struct{}))
}

func findTypesToWriteInternal(typesList []types.Type, typesToWrite *[]types.Type, ref types.ITypeReference, visited map[int]struct{}) {
	index, ok := typeReferenceIndex(ref)
	if !ok || index < 0 || index >= len(typesList) {
		return
	}

	if _, seen := visited[index]; seen {
		return
	}
	visited[index] = struct{}{}

	process := func(inner types.ITypeReference, skipParent bool) {
		innerIndex, ok := typeReferenceIndex(inner)
		if !ok || innerIndex < 0 || innerIndex >= len(typesList) {
			return
		}

		innerType := typesList[innerIndex]
		if !skipParent && !typeSliceContains(*typesToWrite, innerType) {
			*typesToWrite = append(*typesToWrite, innerType)
		}

		findTypesToWriteInternal(typesList, typesToWrite, inner, visited)
	}

	switch concrete := typesList[index].(type) {
	case *types.ArrayType:
		process(concrete.ItemType, false)
	case *types.ObjectType:
		for _, name := range sortedKeys(concrete.Properties) {
			process(concrete.Properties[name].Type, false)
		}
		if concrete.AdditionalProperties != nil {
			process(concrete.AdditionalProperties, false)
		}
	case *types.DiscriminatedObjectType:
		for _, name := range sortedKeys(concrete.BaseProperties) {
			process(concrete.BaseProperties[name].Type, false)
		}
		for _, name := range sortedKeys(concrete.Elements) {
			process(concrete.Elements[name], true)
		}
	}
}

func appendTypeForReference(typesList []types.Type, target *[]types.Type, ref types.ITypeReference) {
	if resolved, ok := resolveTypeRef(typesList, ref); ok {
		*target = append(*target, resolved)
	}
}

func resolveTypeRef(typesList []types.Type, ref types.ITypeReference) (types.Type, bool) {
	index, ok := typeReferenceIndex(ref)
	if !ok || index < 0 || index >= len(typesList) {
		return nil, false
	}
	return typesList[index], true
}

func resolveFunctionType(typesList []types.Type, ref types.ITypeReference) (*types.FunctionType, bool) {
	if resolved, ok := resolveTypeRef(typesList, ref); ok {
		if fn, ok := resolved.(*types.FunctionType); ok {
			return fn, true
		}
	}

	return nil, false
}

func dedupeTypes(values []types.Type) []types.Type {
	result := make([]types.Type, 0, len(values))
	seen := make(map[types.Type]struct{})

	for _, value := range values {
		if _, exists := seen[value]; exists {
			continue
		}
		seen[value] = struct{}{}
		result = append(result, value)
	}

	return result
}

func sortTypesByName(values []types.Type) {
	sort.SliceStable(values, func(i, j int) bool {
		nameI, okI := lowerTypeName(values[i])
		nameJ, okJ := lowerTypeName(values[j])

		if !okI {
			if !okJ {
				return false
			}
			return false
		}

		if !okJ {
			return true
		}

		return nameI < nameJ
	})
}

func lowerTypeName(value types.Type) (string, bool) {
	switch concrete := value.(type) {
	case *types.ObjectType:
		return strings.ToLower(concrete.Name), true
	case *types.DiscriminatedObjectType:
		return strings.ToLower(concrete.Name), true
	default:
		return "", false
	}
}

func resourceBaseName(name string) string {
	parts := strings.Split(name, "@")
	if len(parts) == 0 {
		return strings.ToLower(name)
	}
	return strings.ToLower(parts[0])
}

func providerKey(typeString string) string {
	parts := strings.SplitN(typeString, "/", 2)
	if len(parts) == 0 {
		return strings.ToLower(typeString)
	}
	return strings.ToLower(parts[0])
}

func resourceTypeKey(typeString string) string {
	parts := strings.SplitN(typeString, "@", 2)
	if len(parts) == 0 {
		return strings.ToLower(typeString)
	}
	return strings.ToLower(parts[0])
}

func versionFromTypeString(typeString string) string {
	parts := strings.SplitN(typeString, "@", 2)
	if len(parts) < 2 {
		return ""
	}
	return parts[1]
}

func typeSliceContains(slice []types.Type, candidate types.Type) bool {
	for _, existing := range slice {
		if existing == candidate {
			return true
		}
	}
	return false
}

func typeReferenceIndex(ref types.ITypeReference) (int, bool) {
	switch concrete := ref.(type) {
	case types.TypeReference:
		return concrete.Ref, true
	case *types.TypeReference:
		return concrete.Ref, true
	default:
		return 0, false
	}
}

func sortedKeys[T any](m map[string]T) []string {
	keys := make([]string, 0, len(m))
	for key := range m {
		keys = append(keys, key)
	}

	sort.SliceStable(keys, func(i, j int) bool {
		lowerI := strings.ToLower(keys[i])
		lowerJ := strings.ToLower(keys[j])
		if lowerI == lowerJ {
			return keys[i] < keys[j]
		}
		return lowerI < lowerJ
	})

	return keys
}

func relativeMarkdownPath(ref types.ITypeReference) string {
	switch concrete := ref.(type) {
	case types.CrossFileTypeReference:
		return convertJsonPathToMarkdown(concrete.RelativePath)
	case *types.CrossFileTypeReference:
		return convertJsonPathToMarkdown(concrete.RelativePath)
	default:
		return ""
	}
}

func convertJsonPathToMarkdown(path string) string {
	lower := strings.ToLower(path)
	idx := strings.LastIndex(lower, ".json")
	if idx == -1 {
		return path
	}

	return path[:idx] + ".md"
}

func getRelativePath(ref types.ITypeReference) string {
	switch concrete := ref.(type) {
	case types.CrossFileTypeReference:
		return concrete.RelativePath
	case *types.CrossFileTypeReference:
		return concrete.RelativePath
	default:
		return ""
	}
}

func getRefIndex(ref types.ITypeReference) int {
	switch concrete := ref.(type) {
	case types.CrossFileTypeReference:
		return concrete.Ref
	case *types.CrossFileTypeReference:
		return concrete.Ref
	case types.TypeReference:
		return concrete.Ref
	case *types.TypeReference:
		return concrete.Ref
	default:
		return -1
	}
}

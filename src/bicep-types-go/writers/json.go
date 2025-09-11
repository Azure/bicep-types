package writers

import (
	"encoding/json"
	"fmt"
	"io"

	"github.com/Azure/bicep-types/src/bicep-types-go/index"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

// JSONWriter handles writing types and indices to JSON format
type JSONWriter struct {
	indentSize int
}

// NewJSONWriter creates a new JSON writer
func NewJSONWriter() *JSONWriter {
	return &JSONWriter{
		indentSize: 2,
	}
}

// NewJSONWriterWithIndent creates a new JSON writer with custom indentation
func NewJSONWriterWithIndent(indentSize int) *JSONWriter {
	return &JSONWriter{
		indentSize: indentSize,
	}
}

// WriteTypes writes a slice of types to JSON
func (w *JSONWriter) WriteTypes(writer io.Writer, types []types.Type) error {
	indent := ""
	if w.indentSize > 0 {
		for i := 0; i < w.indentSize; i++ {
			indent += " "
		}
	}

	encoder := json.NewEncoder(writer)
	encoder.SetIndent("", indent)
	encoder.SetEscapeHTML(false)

	return encoder.Encode(types)
}

// WriteTypeIndex writes a type index to JSON
func (w *JSONWriter) WriteTypeIndex(writer io.Writer, idx *index.TypeIndex) error {
	indent := ""
	if w.indentSize > 0 {
		for i := 0; i < w.indentSize; i++ {
			indent += " "
		}
	}

	encoder := json.NewEncoder(writer)
	encoder.SetIndent("", indent)
	encoder.SetEscapeHTML(false)

	return encoder.Encode(idx)
}

// WriteTypesToString returns types as a JSON string
func (w *JSONWriter) WriteTypesToString(types []types.Type) (string, error) {
	var buf []byte

	if w.indentSize > 0 {
		indent := ""
		for i := 0; i < w.indentSize; i++ {
			indent += " "
		}
		data, err := json.MarshalIndent(types, "", indent)
		if err != nil {
			return "", fmt.Errorf("failed to marshal types with indent: %w", err)
		}
		buf = data
	} else {
		data, err := json.Marshal(types)
		if err != nil {
			return "", fmt.Errorf("failed to marshal types: %w", err)
		}
		buf = data
	}

	return string(buf), nil
}

// WriteTypeIndexToString returns a type index as a JSON string
func (w *JSONWriter) WriteTypeIndexToString(idx *index.TypeIndex) (string, error) {
	var buf []byte

	if w.indentSize > 0 {
		indent := ""
		for i := 0; i < w.indentSize; i++ {
			indent += " "
		}
		data, err := json.MarshalIndent(idx, "", indent)
		if err != nil {
			return "", fmt.Errorf("failed to marshal type index with indent: %w", err)
		}
		buf = data
	} else {
		data, err := json.Marshal(idx)
		if err != nil {
			return "", fmt.Errorf("failed to marshal type index: %w", err)
		}
		buf = data
	}

	return string(buf), nil
}

// WriteType writes a single type to JSON
func (w *JSONWriter) WriteType(writer io.Writer, t types.Type) error {
	indent := ""
	if w.indentSize > 0 {
		for i := 0; i < w.indentSize; i++ {
			indent += " "
		}
	}

	encoder := json.NewEncoder(writer)
	encoder.SetIndent("", indent)
	encoder.SetEscapeHTML(false)

	return encoder.Encode(t)
}

// WriteTypeToString returns a single type as a JSON string
func (w *JSONWriter) WriteTypeToString(t types.Type) (string, error) {
	var buf []byte

	if w.indentSize > 0 {
		indent := ""
		for i := 0; i < w.indentSize; i++ {
			indent += " "
		}
		data, err := json.MarshalIndent(t, "", indent)
		if err != nil {
			return "", fmt.Errorf("failed to marshal type with indent: %w", err)
		}
		buf = data
	} else {
		data, err := json.Marshal(t)
		if err != nil {
			return "", fmt.Errorf("failed to marshal type: %w", err)
		}
		buf = data
	}

	return string(buf), nil
}

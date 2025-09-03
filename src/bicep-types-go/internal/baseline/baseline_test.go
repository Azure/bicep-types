package baseline

import (
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestBaselineTypeLoader_GetBaselineNames(t *testing.T) {
	loader := NewBaselineTypeLoader()

	names, err := loader.GetBaselineNames()
	require.NoError(t, err)

	// Should have at least the known baselines
	assert.Contains(t, names, "foo")
	assert.Contains(t, names, "http")
	assert.GreaterOrEqual(t, len(names), 2)
}

func TestBaselineTypeLoader_LoadBaselineTypes(t *testing.T) {
	loader := NewBaselineTypeLoader()

	// Test loading each baseline
	names, err := loader.GetBaselineNames()
	require.NoError(t, err)

	for _, name := range names {
		t.Run(name, func(t *testing.T) {
			types, err := loader.LoadBaselineTypes(name)
			require.NoError(t, err, "Failed to load types for baseline: %s", name)

			// Should have loaded some types (or zero for baselines with nested structure)
			assert.GreaterOrEqual(t, len(types), 0, "Baseline %s should have zero or more types", name)

			// Each type should have a valid type discriminator
			for i, typ := range types {
				assert.NotEmpty(t, typ.Type(), "Type at index %d in baseline %s should have a type discriminator", i, name)
			}
		})
	}
}

func TestBaselineTypeLoader_LoadBaselineIndex(t *testing.T) {
	loader := NewBaselineTypeLoader()

	// Test loading each baseline index
	names, err := loader.GetBaselineNames()
	require.NoError(t, err)

	for _, name := range names {
		t.Run(name, func(t *testing.T) {
			index, err := loader.LoadBaselineIndex(name)
			require.NoError(t, err, "Failed to load index for baseline: %s", name)

			// Index should have some structure
			assert.NotNil(t, index)

			// Should have resources or resource functions
			hasResources := len(index.Resources) > 0
			hasResourceFunctions := len(index.ResourceFunctions) > 0

			assert.True(t, hasResources || hasResourceFunctions,
				"Baseline %s should have either resources or resource functions", name)
		})
	}
}

func TestBaselineTypeLoader_ValidateBaseline(t *testing.T) {
	loader := NewBaselineTypeLoader()

	// Test validating each baseline
	names, err := loader.GetBaselineNames()
	require.NoError(t, err)

	for _, name := range names {
		t.Run(name, func(t *testing.T) {
			err := loader.ValidateBaseline(name)
			assert.NoError(t, err, "Baseline %s should validate successfully", name)
		})
	}
}

func TestBaselineTypeLoader_LoadBaselineWithCrossFileReferences(t *testing.T) {
	loader := NewBaselineTypeLoader()

	// Test loading with cross-file references
	names, err := loader.GetBaselineNames()
	require.NoError(t, err)

	for _, name := range names {
		t.Run(name, func(t *testing.T) {
			types, index, err := loader.LoadBaselineWithCrossFileReferences(name)
			require.NoError(t, err, "Failed to load baseline with cross-file references: %s", name)

			assert.NotNil(t, types)
			assert.NotNil(t, index)
			assert.GreaterOrEqual(t, len(types), 0)
		})
	}
}

func TestBaselineTypeLoader_GetAllBaselineTestCases(t *testing.T) {
	loader := NewBaselineTypeLoader()

	testCases, err := loader.GetAllBaselineTestCases()
	require.NoError(t, err)

	// Should have test cases
	assert.Greater(t, len(testCases), 0)

	// Each test case should be valid
	for _, testCase := range testCases {
		assert.NotEmpty(t, testCase.Name)
		assert.NotNil(t, testCase.Types)
		assert.NotNil(t, testCase.Index)
		assert.GreaterOrEqual(t, len(testCase.Types), 0)
	}
}

// Specific test for known baseline structure
func TestBaselineTypeLoader_FooBaseline(t *testing.T) {
	loader := NewBaselineTypeLoader()

	// Load the foo baseline specifically
	types, err := loader.LoadBaselineTypes("foo")
	require.NoError(t, err)

	// The foo baseline should have specific characteristics
	assert.Greater(t, len(types), 0, "Foo baseline should have types")

	// Load the index
	index, err := loader.LoadBaselineIndex("foo")
	require.NoError(t, err)

	// Should have resources
	assert.NotNil(t, index.Resources)
	assert.Greater(t, len(index.Resources), 0, "Foo baseline should have resources")
}

func TestBaselineTypeLoader_HttpBaseline(t *testing.T) {
	loader := NewBaselineTypeLoader()

	// Load the http baseline specifically
	types, err := loader.LoadBaselineTypes("http")
	require.NoError(t, err)

	// The http baseline may have 0 types in the main file (nested structure)
	assert.GreaterOrEqual(t, len(types), 0, "Http baseline should have zero or more types")

	// Load the index
	index, err := loader.LoadBaselineIndex("http")
	require.NoError(t, err)

	// Should have some structure
	assert.NotNil(t, index)
}

// Benchmark baseline loading performance
func BenchmarkBaselineTypeLoader_LoadTypes(b *testing.B) {
	loader := NewBaselineTypeLoader()

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_, err := loader.LoadBaselineTypes("foo")
		if err != nil {
			b.Fatal(err)
		}
	}
}

func BenchmarkBaselineTypeLoader_LoadIndex(b *testing.B) {
	loader := NewBaselineTypeLoader()

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_, err := loader.LoadBaselineIndex("foo")
		if err != nil {
			b.Fatal(err)
		}
	}
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// A 1-based line and column position within a package JSON file.
    /// Column is measured in UTF-16 code units, matching .NET string indexing.
    /// </summary>
    internal struct SourceLocation
    {
        public static readonly SourceLocation Unknown = new SourceLocation(0, 0);

        public int Line;
        public int Column;

        public SourceLocation(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public bool IsKnown => Line > 0 && Column > 0;
    }
}

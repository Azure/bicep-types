// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Maps byte offsets in a UTF-8 JSON file to 1-based line/column positions.
    /// Columns are measured in UTF-16 code units, matching .NET <c>string</c> indexing.
    /// Both <c>\n</c> and <c>\r\n</c> are treated as single line breaks.
    /// </summary>
    internal sealed class SourceMap
    {
        private readonly byte[] rawBytes;

        // lineStartOffsets[i] = byte offset of the first byte of line (i+1).
        // lineStartOffsets[0] = 0  (line 1 starts at offset 0).
        private readonly int[] lineStartOffsets;

        private SourceMap(byte[] rawBytes, int[] lineStartOffsets)
        {
            this.rawBytes = rawBytes;
            this.lineStartOffsets = lineStartOffsets;
        }

        /// <summary>Builds a <see cref="SourceMap"/> from the raw UTF-8 bytes of a file.</summary>
        public static SourceMap Create(byte[] rawBytes)
        {
            if (rawBytes == null)
            {
                throw new ArgumentNullException(nameof(rawBytes));
            }

            var starts = new List<int> { 0 };
            for (int i = 0; i < rawBytes.Length; i++)
            {
                if (rawBytes[i] == 0x0A) // \n
                {
                    starts.Add(i + 1);
                }
                else if (rawBytes[i] == 0x0D) // \r
                {
                    // \r\n counts as one line break; skip the \n
                    if (i + 1 < rawBytes.Length && rawBytes[i + 1] == 0x0A)
                    {
                        i++;
                    }
                    starts.Add(i + 1);
                }
            }

            return new SourceMap(rawBytes, starts.ToArray());
        }

        /// <summary>
        /// Converts a <see cref="JsonValueNode"/> byte offset to a 1-based line/column location.
        /// </summary>
        public SourceLocation GetLocation(long byteOffset)
        {
            if (byteOffset < 0 || rawBytes.Length == 0)
            {
                return new SourceLocation(1, 1);
            }

            int offset = (int)Math.Min(byteOffset, (long)(rawBytes.Length - 1));
            int lineIndex = FindLineIndex(offset);
            int line = lineIndex + 1; // 1-based
            int lineStart = lineStartOffsets[lineIndex];
            int prefixByteCount = offset - lineStart;

            int column;
            if (prefixByteCount <= 0)
            {
                column = 1;
            }
            else
            {
                try
                {
                    // Decode the bytes from line start to the target offset as UTF-8 to get UTF-16 length
                    string prefix = Encoding.UTF8.GetString(rawBytes, lineStart, prefixByteCount);
                    column = prefix.Length + 1; // string.Length = UTF-16 code unit count; +1 for 1-based
                }
                catch
                {
                    column = prefixByteCount + 1; // fallback: byte count approximation
                }
            }

            return new SourceLocation(line, column);
        }

        /// <summary>
        /// Converts the error position from a <see cref="JsonException"/> to a
        /// 1-based line/column location.  <paramref name="zeroBasedLine"/> and
        /// <paramref name="bytePositionInLine"/> are 0-based as reported by
        /// <see cref="JsonException.LineNumber"/> and <see cref="JsonException.BytePositionInLine"/>.
        /// </summary>
        public SourceLocation GetLocationForException(long zeroBasedLine, long bytePositionInLine)
        {
            int lineIndex = (int)Math.Max(0L, Math.Min(zeroBasedLine, (long)(lineStartOffsets.Length - 1)));
            long lineStart = lineStartOffsets[lineIndex];
            long byteOffset = lineStart + Math.Max(0L, bytePositionInLine);
            return GetLocation(byteOffset);
        }

        /// <summary>Binary search for the 0-based line index that contains <paramref name="byteOffset"/>.</summary>
        private int FindLineIndex(int byteOffset)
        {
            int lo = 0;
            int hi = lineStartOffsets.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (lineStartOffsets[mid] <= byteOffset)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return lo;
        }

        /// <summary>
        /// Parses the UTF-8 bytes as JSON and returns the root <see cref="JsonValueNode"/> node.
        /// On success, also provides a <see cref="SourceMap"/> built from the same bytes.
        /// On parse failure, <paramref name="root"/> is <c>null</c> and
        /// <paramref name="parseError"/> describes the first syntax error.
        /// </summary>
        public static bool TryParse(
            byte[] utf8Bytes,
            string packageRelativePath,
            out JsonValueNode? root,
            out SourceMap sourceMap,
            out (int line, int column, string message)? parseError)
        {
            sourceMap = Create(utf8Bytes);

            if (utf8Bytes.Length == 0)
            {
                root = null;
                parseError = (1, 1, "The JSON file is empty.");
                return false;
            }

            try
            {
                var readerOptions = new JsonReaderOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                };
                var span = new ReadOnlySpan<byte>(utf8Bytes);
                var reader = new Utf8JsonReader(span, readerOptions);

                if (!reader.Read())
                {
                    root = null;
                    parseError = (1, 1, "Unexpected end of JSON input.");
                    return false;
                }

                root = ParseCurrentToken(ref reader);

                // Ensure the top-level value is the entire document. Any non-whitespace
                // content after the root value is malformed JSON (e.g. "{} garbage" or
                // two top-level values). Read() returns false at EOF (trailing whitespace
                // only) and throws JsonException for invalid trailing bytes.
                if (reader.Read())
                {
                    var trailingLoc = sourceMap.GetLocation(reader.TokenStartIndex);
                    root = null;
                    parseError = (trailingLoc.Line, trailingLoc.Column, "Unexpected content after the top-level JSON value.");
                    return false;
                }

                parseError = null;
                return true;
            }
            catch (JsonException ex)
            {
                root = null;
                var loc = sourceMap.GetLocationForException(
                    ex.LineNumber ?? 0L,
                    ex.BytePositionInLine ?? 0L);
                string msg = ex.Message ?? "Invalid JSON.";
                parseError = (loc.Line, loc.Column, msg);
                return false;
            }
        }

        private static JsonValueNode ParseCurrentToken(ref Utf8JsonReader reader)
        {
            long offset = reader.TokenStartIndex;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var properties = new List<JsonProperty>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    long nameOffset = reader.TokenStartIndex;
                    string propName = reader.GetString()!;
                    reader.Read();
                    var propValue = ParseCurrentToken(ref reader);
                    properties.Add(new JsonProperty(propName, nameOffset, propValue));
                }
                return JsonValueNode.CreateObject(offset, properties);
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var elements = new List<JsonValueNode>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    elements.Add(ParseCurrentToken(ref reader));
                }
                return JsonValueNode.CreateArray(offset, elements);
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return JsonValueNode.CreateString(offset, reader.GetString()!);
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                bool ok = reader.TryGetInt64(out long val);
                return JsonValueNode.CreateNumber(offset, ok, val);
            }

            if (reader.TokenType == JsonTokenType.True)
            {
                return JsonValueNode.CreateTrue(offset);
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return JsonValueNode.CreateFalse(offset);
            }

            // JsonTokenType.Null
            return JsonValueNode.CreateNull(offset);
        }
    }
}

/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if HAS_ASYNC
using System.Threading.Tasks;
#endif

// These suggestions from Resharper apply because we don't want it to recommend fixing things needed for Net20:
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment
// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchExpression
// ReSharper disable ConvertIfStatementToSwitchStatement

namespace CSVFile
{

    /// <summary>
    /// Root class that contains static functions for straightforward CSV parsing
    /// </summary>
    public static class CSV
    {
        /// <summary>
        /// Use this to determine what version of DotNet was used to build this library
        /// </summary>
#if NET2_0
        public const string VERSION = "NET20";
#elif NET4_0
        public const string VERSION = "NET40";
#elif NET4_5
        public const string VERSION = "NET45";
#elif NET5_0
        public const string VERSION = "NET50";
#elif NET6_0
        public const string VERSION = "NET60";
#elif NETSTANDARD1_0
        public const string VERSION = "NETSTANDARD10";
#elif NETSTANDARD2_0
        public const string VERSION = "NETSTANDARD20";
#else
        public const string VERSION = "UNKNOWN";
#endif

        /// <summary>
        /// Parse a CSV stream into <![CDATA[ IEnumerable<string[]> ]]>, while permitting embedded newlines
        /// </summary>
        /// <param name="inStream">The stream to read</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <returns>An enumerable object that can be examined to retrieve rows from the stream.</returns>
        public static IEnumerable<string[]> ParseStream(StreamReader inStream, CSVSettings settings = null)
        {
            int bufferSize = settings?.BufferSize ?? CSVSettings.DEFAULT_BUFFER_SIZE;
            var buffer = new char[bufferSize];
            var machine = new CSVStateMachine(settings);
            while (machine.State == CSVState.CanKeepGoing)
            {
                var line = string.Empty;
                if (machine.NeedsMoreText() && !inStream.EndOfStream)
                {
                    var readChars = inStream.ReadBlock(buffer, 0, bufferSize);
                    line = new string(buffer, 0, readChars);
                }
                var row = machine.ParseChunk(line, inStream.EndOfStream);
                if (row != null)
                {
                    yield return row;
                } 
            }
        }

#if HAS_ASYNC_IENUM
        /// <summary>
        /// Parse a CSV stream into <![CDATA[ IEnumerable<string[]> ]]> asynchronously, while permitting embedded newlines
        /// </summary>
        /// <param name="inStream">The stream to read</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <returns>An enumerable object that can be examined to retrieve rows from the stream.</returns>
        public static async IAsyncEnumerable<string[]> ParseStreamAsync(StreamReader inStream, CSVSettings settings = null)
        {
            int bufferSize = settings?.BufferSize ?? CSVSettings.DEFAULT_BUFFER_SIZE;
            var buffer = new char[bufferSize];
            var machine = new CSVStateMachine(settings);
            while (machine.State == CSVState.CanKeepGoing)
            {
                var line = string.Empty;
                if (machine.NeedsMoreText() && !inStream.EndOfStream)
                {
                    var readChars = await inStream.ReadBlockAsync(buffer, 0, bufferSize);
                    line = new string(buffer, 0, readChars);
                }
                var row = machine.ParseChunk(line, inStream.EndOfStream);
                if (row != null)
                {
                    yield return row;
                }
            }
        }
#endif

        /// <summary>
        /// Parse a line from a CSV file and return an array of fields, or null if it fails
        /// </summary>
        /// <param name="line">One line of text from a CSV file</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <param name="throwOnFailure">If this value is true, throws an exception if parsing fails</param>
        /// <returns>An array containing all fields in the next row of data, or null if it could not be parsed.</returns>
        public static string[] ParseLine(string line, CSVSettings settings = null, bool? throwOnFailure = null)
        {
            string[] row = null;
            var machine = new CSVStateMachine(settings);
            while (machine.State == CSVState.CanKeepGoing)
            {
                row = machine.ParseChunk(line, true);
                line = string.Empty;
            }

            // Anything other than success throws an error here
            if (machine.State != CSVState.Done)
            {
                throw new Exception($"Malformed CSV structure: {machine.State}");
            }
            return row;
        }

        /// <summary>
        /// Try to parse a line of CSV data.  Can only return false if an unterminated text qualifier is encountered.
        ///
        /// This function cannot recognize 'sep=' lines because it does not know whether it is parsing the first line
        /// in the overall CSV stream.
        /// </summary>
        /// <returns>False if there was an unterminated text qualifier in the <paramref name="line"/></returns>
        /// <param name="line">The line of text to parse</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <param name="row">The array of fields found in the line</param>
        public static bool TryParseLine(string line, out string[] row, CSVSettings settings = null)
        {
            row = null;
            var machine = new CSVStateMachine(settings);
            while (machine.State == CSVState.CanKeepGoing)
            {
                row = machine.ParseChunk(line, true);
                line = string.Empty;
            }
            return machine.State == CSVState.Done;
        }

        /// <summary>
        /// Deserialize a CSV string into a list of typed objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize</typeparam>
        /// <param name="settings">The CSV settings to use when parsing the source (Default: CSV)</param>
        /// <param name="source">The source CSV to deserialize</param>
        /// <returns></returns>
        public static IEnumerable<T> Deserialize<T>(string source, CSVSettings settings = null) where T : class, new()
        {
            return CSVReader.FromString(source, settings).Deserialize<T>();
        }

#if HAS_ASYNC_IENUM
        /// <summary>
        /// Deserialize a CSV string into a list of typed objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize</typeparam>
        /// <param name="settings">The CSV settings to use when parsing the source (Default: CSV)</param>
        /// <param name="source">The source CSV to deserialize</param>
        /// <returns></returns>
        public static IAsyncEnumerable<T> DeserializeAsync<T>(string source, CSVSettings settings = null) where T : class, new()
        {
            return CSVReader.FromString(source, settings).DeserializeAsync<T>();
        }
#endif

        /// <summary>
        /// Serialize a sequence of objects into a CSV string
        /// </summary>
        /// <returns>A single line of CSV encoded data containing these values</returns>
        /// <param name="row">A list or array of objects to serialize</param>
        /// <param name="settings">The field delimiter character (Default: comma)</param>
#if NET2_0
        public static string ToCSVString(IEnumerable<object> row, CSVSettings settings = null)
#else
        public static string ToCSVString(this IEnumerable<object> row, CSVSettings settings = null)
#endif
        {
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }
            var riskyChars = settings.GetRiskyChars();
            var forceQualifierTypes = settings.GetForceQualifierTypes();
            return ItemsToCsv(row, settings, riskyChars, forceQualifierTypes);
        }

        /// <summary>
        /// Serialize an array of objects to CSV format
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize from this CSV</typeparam>
        /// <param name="list">The array of objects to serialize</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>The completed CSV string representing one line per element in list</returns>
        public static string Serialize<T>(IEnumerable<T> list, CSVSettings settings = null) where T : class, new()
        {
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }
            using (var ms = new MemoryStream())
            {
                using (var cw = new CSVWriter(ms, settings))
                {
                    cw.Serialize(list);
                }

                var rawString = settings.Encoding.GetString(ms.ToArray());
                return RemoveByteOrderMarker(rawString);
            }
        }

        private static string _byteOrderMarkUtf8 =
            Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        internal static string RemoveByteOrderMarker(string rawString)
        {
            if (rawString.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
            {
                return rawString.Substring(_byteOrderMarkUtf8.Length);
            }
            return rawString;
        }

        /// <summary>
        /// Serialize an array of objects to CSV format
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize from this CSV</typeparam>
        /// <param name="list">The array of objects to serialize</param>
        /// <param name="stream">The stream to which we will send this CSV text</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>The completed CSV string representing one line per element in list</returns>
        public static void Serialize<T>(IEnumerable<T> list, Stream stream, CSVSettings settings = null) where T : class, new()
        {
            using (var cw = new CSVWriter(stream, settings))
            {
                cw.Serialize(list);
            }
        }

#if HAS_ASYNC
        /// <summary>
        /// Serialize an array of objects to CSV format
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize from this CSV</typeparam>
        /// <param name="list">The array of objects to serialize</param>
        /// <param name="stream">The stream to which we will send this CSV text</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>The completed CSV string representing one line per element in list</returns>
        public static Task SerializeAsync<T>(IEnumerable<T> list, Stream stream, CSVSettings settings = null) where T : class, new()
        {
            using (var cw = new CSVWriter(stream, settings))
            {
                return cw.SerializeAsync(list);
            }
        }
#endif

#if HAS_ASYNC_IENUM
        /// <summary>
        /// Serialize an array of objects to CSV format
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize from this CSV</typeparam>
        /// <param name="list">The array of objects to serialize</param>
        /// <param name="stream">The stream to which we will send this CSV text</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>The completed CSV string representing one line per element in list</returns>
        public static Task SerializeAsync<T>(IAsyncEnumerable<T> list, Stream stream, CSVSettings settings = null) where T : class, new()
        {
            using (var cw = new CSVWriter(stream, settings))
            {
                return cw.SerializeAsync(list);
            }
        }
#endif

        /// <summary>
        /// Add a CSV Header line to a StringBuilder for a specific type
        /// </summary>
        /// <param name="sb">The StringBuilder to append data</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
#if NET2_0
        public static void AppendCSVHeader<T>(StringBuilder sb, CSVSettings settings = null) where T: class, new()
#else
        public static void AppendCSVHeader<T>(this StringBuilder sb, CSVSettings settings = null) where T : class, new()
#endif
        {
            var header = Serialize(new T[] { }, settings);
            sb.Append(header);
        }

        /// <summary>
        /// Appends a single object to a StringBuilder in CSV format as a single line
        /// </summary>
        /// <param name="sb">The StringBuilder to append data</param>
        /// <param name="obj">The single object to append in CSV-line format</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
#if NET2_0
        public static void AppendCSVLine<T>(StringBuilder sb, T obj, CSVSettings settings = null) where T : class, new()
#else
        public static void AppendCSVLine<T>(this StringBuilder sb, T obj, CSVSettings settings = null) where T : class, new()
#endif
        {
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }

            // Duplicate settings, but flag ourselves to ignore the header
            settings = settings.CloneWithNewDelimiter(settings.FieldDelimiter);
            settings.HeaderRowIncluded = false;
            var line = Serialize(new T[] { obj }, settings);
            sb.Append(line);
        }

        /// <summary>
        /// Internal method to convert a list of things into a CSV line using the specified settings object
        /// 
        /// This function assumes:
        ///  * That the list of items is not null, but it may contain nulls
        ///  * That settings is not null
        ///  * That RiskyChars and ForceQualifierTypes have been set up correctly to match the CSV settings
        /// </summary>
        /// <param name="items"></param>
        /// <param name="settings"></param>
        /// <param name="riskyChars"></param>
        /// <param name="forceQualifierTypes"></param>
        /// <returns></returns>
        internal static string ItemsToCsv(IEnumerable<object> items, CSVSettings settings, char[] riskyChars, Dictionary<Type, int> forceQualifierTypes)
        {
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                // If this is null, check our settings for what they want us to do
                if (item == null)
                {
                    if (settings.AllowNull)
                    {
                        sb.Append(settings.NullToken);
                    }
                    sb.Append(settings.FieldDelimiter);
                    continue;
                }

                // Is this a date time?
                string s;
                if (item is DateTime)
                {
                    s = ((DateTime)item).ToString(settings.DateTimeFormat);
                }
                else
                {
                    s = item.ToString();
                }

                // Check if this item requires qualifiers
                var requiresQualifiers = settings.ForceQualifiers || s.IndexOfAny(riskyChars) >= 0 || (forceQualifierTypes != null && forceQualifierTypes.ContainsKey(item.GetType()));

                // Okay, let's handle this value normally
                if (requiresQualifiers) sb.Append(settings.TextQualifier);
                if (!string.IsNullOrEmpty(s))
                {
                    // Only go character-by-character if necessary
                    if (s.IndexOf(settings.TextQualifier) >= 0)
                    {
                        foreach (var c in s)
                        {
                            // Double up text qualifiers
                            if (c == settings.TextQualifier)
                            {
                                sb.Append(c);
                            }

                            sb.Append(c);
                        }
                    }
                    else
                    {
                        sb.Append(s);
                    }
                }

                // Move to the next cell
                if (requiresQualifiers) sb.Append(settings.TextQualifier);
                sb.Append(settings.FieldDelimiter);
            }

            // Subtract the trailing delimiter so we don't inadvertently add an empty column at the end
            sb.Length -= 1;
            return sb.ToString();
        }

        /// <summary>
        /// Parse a separator line and determine
        /// </summary>
        /// <param name="line"></param>
        /// <returns>The separator</returns>
        public static char? ParseSepLine(string line)
        {
            if (line.StartsWith("sep", StringComparison.OrdinalIgnoreCase))
            {
                var equals = line.Substring(3).Trim();
                if (equals.StartsWith("="))
                {
                    var separator = equals.Substring(1).Trim();
                    if (separator.Length > 1)
                    {
                        throw new Exception("Separator in 'sep=' line must be a single character");
                    }

                    if (separator.Length == 1)
                    {
                        return separator[0];
                    }
                }
            }

            // This wasn't a sep line
            return null;
        }
    }
}

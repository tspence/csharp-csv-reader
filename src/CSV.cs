/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if NET50
using System.Threading.Tasks;
#endif

#if NEEDS_EXTENSION_ATTRIBUTE
// Use this namespace to be able to declare extension methods
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class
         | AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
}
#endif

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
#if NET40
        public const string VERSION = "NET40";
#elif NET45
        public const string VERSION = "NET45";
#elif NET50
        public const string VERSION = "NET50";
#elif NETSTANDARD10
        public const string VERSION = "NETSTANDARD10";
#elif NETSTANDARD20
        public const string VERSION = "NETSTANDARD20";
#else
        public const string VERSION = "UNKNOWN";
#endif

        /// <summary>
        /// The default CSV field delimiter.
        /// </summary>
        public const char DEFAULT_CSV_DELIMITER = ',';

        /// <summary>
        /// The default CSV text qualifier.  This is used to encode strings that contain the field delimiter.
        /// </summary>
        public const char DEFAULT_CSV_QUALIFIER = '"';

        /// <summary>
        /// The default TSV (tab delimited file) field delimiter.
        /// </summary>
        public const char DEFAULT_TSV_DELIMITER = '\t';

        /// <summary>
        /// The default TSV (tab delimited file) text qualifier.  This is used to encode strings that contain the field delimiter.
        /// </summary>
        public const char DEFAULT_TSV_QUALIFIER = '"';


        /// <summary>
        /// Parse a CSV stream into <![CDATA[ IEnumerable<string[]> ]]>, while permitting embedded newlines
        /// </summary>
        /// <param name="inStream">The stream to read</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <returns>An enumerable object that can be examined to retrieve rows from the stream.</returns>
        public static IEnumerable<string[]> ParseStream(StreamReader inStream, CSVSettings settings = null)
        {
            var line = "";
            var i = -1;
            var list = new List<string>();
            var work = new StringBuilder();

            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null) {
                settings = CSVSettings.CSV;
            }

            // Begin reading from the stream
            while (i < line.Length || !inStream.EndOfStream)
            {
                // Consume the next character of data
                i++;
                if (i >= line.Length) {
                    var newLine = inStream.ReadLine();
                    line += newLine + settings.LineSeparator;
                }
                char c = line[i];

                // Are we at a line separator? If so, yield our work and begin again
                if (String.Equals(line.Substring(i, settings.LineSeparator.Length), settings.LineSeparator)) {
                    list.Add(work.ToString());
                    yield return list.ToArray();
                    list.Clear();
                    work.Clear();
                    if (inStream.EndOfStream)
                    {
                        break;
                    }

                    // Read in next line
                    if (i + settings.LineSeparator.Length >= line.Length)
                    {
                        line = inStream.ReadLine() + settings.LineSeparator;
                    }
                    else
                    {
                        line = line.Substring(i + settings.LineSeparator.Length);
                    }
                    i = -1;

                    // While starting a field, do we detect a text qualifier?
                }
                else if ((c == settings.TextQualifier) && (work.Length == 0))
                {
                    // Our next task is to find the end of this qualified-text field
                    int p2 = -1;
                    while (p2 < 0) {

                        // If we don't see an end in sight, read more from the stream
                        p2 = line.IndexOf(settings.TextQualifier, i + 1);
                        if (p2 < 0) {

                            // No text qualifiers yet? Let's read more from the stream and continue
                            work.Append(line.Substring(i + 1));
                            i = -1;
                            var newLine = inStream.ReadLine();
                            if (String.IsNullOrEmpty(newLine) && inStream.EndOfStream)
                            {
                                break;
                            }
                            line = newLine + settings.LineSeparator;
                            continue;
                        }

                        // Append the text between the qualifiers
                        work.Append(line.Substring(i + 1, p2 - i - 1));
                        i = p2;
                        
                        // If the user put in a doubled-up qualifier, e.g. `""`, insert a single one and continue
                        if (((p2 + 1) < line.Length) && (line[p2 + 1] == settings.TextQualifier))
                        {
                            work.Append(settings.TextQualifier);
                            i++;
                            p2 = -1;
                        }
                    }

                    // Does this start a new field?
                }
                else if (c == settings.FieldDelimiter)
                {
                    // Is this a null token, and do we permit null tokens?
                    AddToken(list, work, settings);

                    // Test for special case: when the user has written a casual comma, space, and text qualifier, skip the space
                    // Checks if the second parameter of the if statement will pass through successfully
                    // e.g. `"bob", "mary", "bill"`
                    if (i + 2 <= line.Length - 1)
                    {
                        if (line[i + 1].Equals(' ') && line[i + 2].Equals(settings.TextQualifier))
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    work.Append(c);
                }
            }
        }

#if NET50
        /// <summary>
        /// Parse a CSV stream into <![CDATA[ IEnumerable<string[]> ]]> asynchronously, while permitting embedded newlines
        /// </summary>
        /// <param name="inStream">The stream to read</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <returns>An enumerable object that can be examined to retrieve rows from the stream.</returns>
        public static async IAsyncEnumerable<string[]> ParseStreamAsync(StreamReader inStream, CSVSettings settings = null)
        {
            var line = "";
            var i = -1;
            var list = new List<string>();
            var work = new StringBuilder();

            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }

            // Begin reading from the stream
            while (i < line.Length || !inStream.EndOfStream)
            {
                // Consume the next character of data
                i++;
                if (i >= line.Length)
                {
                    var newLine = await inStream.ReadLineAsync();
                    line += newLine + settings.LineSeparator;
                }
                char c = line[i];

                // Are we at a line separator? If so, yield our work and begin again
                if (String.Equals(line.Substring(i, settings.LineSeparator.Length), settings.LineSeparator))
                {
                    list.Add(work.ToString());
                    yield return list.ToArray();
                    list.Clear();
                    work.Clear();
                    if (inStream.EndOfStream)
                    {
                        break;
                    }

                    // Read in next line
                    if (i + settings.LineSeparator.Length >= line.Length)
                    {
                        line = (await inStream.ReadLineAsync()) + settings.LineSeparator;
                    }
                    else
                    {
                        line = line.Substring(i + settings.LineSeparator.Length);
                    }
                    i = -1;

                    // While starting a field, do we detect a text qualifier?
                }
                else if ((c == settings.TextQualifier) && (work.Length == 0))
                {
                    // Our next task is to find the end of this qualified-text field
                    int p2 = -1;
                    while (p2 < 0)
                    {

                        // If we don't see an end in sight, read more from the stream
                        p2 = line.IndexOf(settings.TextQualifier, i + 1);
                        if (p2 < 0)
                        {

                            // No text qualifiers yet? Let's read more from the stream and continue
                            work.Append(line.Substring(i + 1));
                            i = -1;
                            var newLine = await inStream.ReadLineAsync();
                            if (String.IsNullOrEmpty(newLine) && inStream.EndOfStream)
                            {
                                break;
                            }
                            line = newLine + settings.LineSeparator;
                            continue;
                        }

                        // Append the text between the qualifiers
                        work.Append(line.Substring(i + 1, p2 - i - 1));
                        i = p2;

                        // If the user put in a doubled-up qualifier, e.g. `""`, insert a single one and continue
                        if (((p2 + 1) < line.Length) && (line[p2 + 1] == settings.TextQualifier))
                        {
                            work.Append(settings.TextQualifier);
                            i++;
                            p2 = -1;
                        }
                    }

                    // Does this start a new field?
                }
                else if (c == settings.FieldDelimiter)
                {
                    // Is this a null token, and do we permit null tokens?
                    AddToken(list, work, settings);

                    // Test for special case: when the user has written a casual comma, space, and text qualifier, skip the space
                    // Checks if the second parameter of the if statement will pass through successfully
                    // e.g. `"bob", "mary", "bill"`
                    if (i + 2 <= line.Length - 1)
                    {
                        if (line[i + 1].Equals(' ') && line[i + 2].Equals(settings.TextQualifier))
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    work.Append(c);
                }
            }
        }
#endif

        /// <summary>
        /// Parse a single row of data from a CSV line into an array of objects, while permitting embedded newlines
        /// DEPRECATED - Please use ParseStream instead.
        /// </summary>
        /// <param name="inStream">The stream to read</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <returns>An array containing all fields in the next row of data, or null if it could not be parsed.</returns>
        [Obsolete("Use ParseLine instead; it correctly implements multiline reading.")]
        public static string[] ParseMultiLine(StreamReader inStream, CSVSettings settings = null)
        {
            var sb = new StringBuilder();
            string[] array = null;
            
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }
            
            while (!inStream.EndOfStream)
            {
                // Read in a line
                sb.Append(inStream.ReadLine());

                // Does it parse?
                string s = sb.ToString();
                if (TryParseLine(s, out array, settings))
                {
                    return array;
                }

                // We didn't succeed on the first try - our text must have an embedded newline in it.
                // Let's assume that we were in the middle of parsing a field when we encountered a newline,
                // and continue parsing.
                sb.Append(settings.LineSeparator);
            }

            // Fails to parse - return the best array we were able to get
            return array;
        }

        /// <summary>
        /// Parse a line from a CSV file and return an array of fields, or null if 
        /// </summary>
        /// <param name="line">One line of text from a CSV file</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <returns>An array containing all fields in the next row of data, or null if it could not be parsed.</returns>
        public static string[] ParseLine(string line, CSVSettings settings = null)
        {
            TryParseLine(line, out string[] row, settings);
            return row;
        }

        /// <summary>
        /// Try to parse a line of CSV data.  Can only return false if an unterminated text qualifier is encountered.
        /// </summary>
        /// <returns>False if there was an unterminated text qualifier in the <paramref name="line"/></returns>
        /// <param name="line">The line of text to parse</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <param name="row">The array of fields found in the line</param>
        public static bool TryParseLine(string line, out string[] row, CSVSettings settings = null)
        {
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }

            // Okay, let's begin parsing
            var list = new List<string>();
            var work = new StringBuilder();
            for (var i = 0; i < line.Length; i++)
            {
                char c = line[i];

                // If we are starting a new field, is this field text qualified?
                if ((c == settings.TextQualifier) && (work.Length == 0))
                {
                    while (true)
                    {
                        var p2 = line.IndexOf(settings.TextQualifier, i + 1);

                        // If no closing qualifier is found, this string is broken; return failure.
                        if (p2 < 0)
                        {
                            work.Append(line.Substring(i + 1));
                            list.Add(work.ToString());
                            row = list.ToArray();
                            return false;
                        }

                        // Append this qualified string
                        work.Append(line.Substring(i + 1, p2 - i - 1));
                        i = p2;

                        // If this is a double quote, keep going!
                        if (((p2 + 1) < line.Length) && (line[p2 + 1] == settings.TextQualifier))
                        {
                            work.Append(settings.TextQualifier);
                            i++;

                            // otherwise, this is a single qualifier, we're done
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Does this start a new field?
                }
                else if (c == settings.FieldDelimiter)
                {
                    // Is this a null token, and do we permit null tokens?
                    AddToken(list, work, settings);

                    // Test for special case: when the user has written a casual comma, space, and text qualifier, skip the space
                    // Checks if the second parameter of the if statement will pass through successfully
                    // e.g. "bob", "mary", "bill"
                    if (i + 2 <= line.Length - 1)
                    {
                        if (line[i + 1].Equals(' ') && line[i + 2].Equals(settings.TextQualifier))
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    work.Append(c);
                }
            }

            // We always add the last work as an element.  That means `alice,bob,charlie,` will be four items long.
            AddToken(list, work, settings);
            row = list.ToArray();
            return true;
        }

        /// <summary>
        /// Add a single token to the list
        /// </summary>
        /// <param name="list">List.</param>
        /// <param name="work">Work.</param>
        /// <param name="settings">Settings.</param>
        private static void AddToken(List<string> list, StringBuilder work, CSVSettings settings)
        {
            var s = work.ToString();
            if (settings.AllowNull && String.Equals(s, settings.NullToken, StringComparison.Ordinal))
            {
                list.Add(null);
            }
            else
            {
                list.Add(s);
            }
            work.Length = 0;
        }

        /// <summary>
        /// Deserialize a CSV string into a list of typed objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize</typeparam>
        /// <param name="settings">The CSV settings to use when parsing the source (Default: CSV)</param>
        /// <param name="source">The source CSV to deserialize</param>
        /// <returns></returns>
        public static List<T> Deserialize<T>(string source, CSVSettings settings = null) where T : class, new()
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(source);
            using (var stream = new MemoryStream(byteArray))
            {
                using (CSVReader cr = new CSVReader(new StreamReader(stream), settings))
                {
                    return cr.Deserialize<T>();
                }
            }
        }

#if NET50
        /// <summary>
        /// Deserialize a CSV string into a list of typed objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize</typeparam>
        /// <param name="settings">The CSV settings to use when parsing the source (Default: CSV)</param>
        /// <param name="source">The source CSV to deserialize</param>
        /// <returns></returns>
        public static async Task<List<T>> DeserializeAsync<T>(string source, CSVSettings settings = null) where T : class, new()
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(source);
            using (var stream = new MemoryStream(byteArray))
            {
                using (var cr = await CSVAsyncReader.From(new StreamReader(stream), settings))
                {
                    return await cr.Deserialize<T>();
                }
            }
        }
#endif

        /// <summary>
        /// Serialize a sequence of objects into a CSV string
        /// </summary>
        /// <returns>A single line of CSV encoded data containing these values</returns>
        /// <param name="row">A list or array of objects to serialize</param>
        /// <param name="settings">The field delimiter character (Default: comma)</param>
        public static string ToCSVString(this IEnumerable<object> row, CSVSettings settings = null)
        {
            var sb = new StringBuilder();
            AppendCSVRow(sb, row, settings);
            return sb.ToString();
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
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }

            // Okay, let's add headers (if desired) and objects
            var sb = new StringBuilder();
            if (settings.HeaderRowIncluded)
            {
                sb.AppendCSVHeader<T>(settings);
            }
            foreach (var obj in list)
            {
                sb.AppendCSVLine(obj, settings);
            }
            
            // Here's your data serialized in CSV format
            return sb.ToString();
        }

        /// <summary>
        /// Add a CSV Header line to a StringBuilder for a specific type
        /// </summary>
        /// <param name="sb">The StringBuilder to append data</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        public static void AppendCSVHeader<T>(this StringBuilder sb, CSVSettings settings = null)
        {
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }

            var type = typeof(T);
            var headers = type.GetFields().Select(fi => fi.Name).ToList();
            headers.AddRange(type.GetProperties().Select(pi => pi.Name));
            AppendCSVRow(sb, headers, settings);
            sb.Append(settings.LineSeparator);
        }

        /// <summary>
        /// Appends a single object to a StringBuilder in CSV format as a single line
        /// </summary>
        /// <param name="sb">The StringBuilder to append data</param>
        /// <param name="obj">The single object to append in CSV-line format</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static void AppendCSVLine<T>(this StringBuilder sb, T obj, CSVSettings settings = null) where T : class, new()
        {
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }
            
            // Retrieve reflection information
            var type = typeof(T);
            var values = type.GetFields().Select(fi => fi.GetValue(obj)).ToList();
            values.AddRange(type.GetProperties().Select(pi => pi.GetValue(obj, null)));

            // Output all the CSV items
            AppendCSVRow(sb, values, settings);
            sb.Append(settings.LineSeparator);
        }

        /// <summary>
        /// Append an array of objects to a StringBuilder in CSV format
        /// </summary>
        /// <param name="sb">The StringBuilder to append</param>
        /// <param name="row">The list of objects to append</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        private static void AppendCSVRow(this StringBuilder sb, IEnumerable<object> row, CSVSettings settings = null)
        {
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }
            var q = settings.TextQualifier.ToString();

            var riskyChars = new char[3];
            riskyChars[0] = settings.FieldDelimiter;
            riskyChars[1] = settings.TextQualifier;
            riskyChars[2] = '\n';  // this includes \r\n sequence as well
            var riskyLineSeparator = !settings.LineSeparator.Contains("\n");
            
            // Okay, let's begin
            foreach (var o in row)
            {
                // If this is null, check our settings for what they want us to do
                if (o == null)
                {
                    if (settings.AllowNull)
                    {
                        sb.Append(settings.NullToken);
                        sb.Append(settings.FieldDelimiter);
                    }
                    continue;
                }

                // Okay, let's handle this value normally
                var s = o.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    // Does this string contain any risky characters, or are we in force-qualifiers / allow-null mode?
                    if (settings.ForceQualifiers || settings.AllowNull || (s.IndexOfAny(riskyChars) >= 0) || riskyLineSeparator && s.Contains(settings.LineSeparator))
                    {
                        sb.Append(q);

                        // Double up any qualifiers that may occur
                        sb.Append(s.Replace(q, q + q));
                        sb.Append(q);
                    }
                    else
                    {
                        sb.Append(s);
                    }
                }

                // Move to the next cell
                sb.Append(settings.FieldDelimiter);
            }

            // Subtract the trailing delimiter so we don't inadvertently add an empty column at the end
            sb.Length -= 1;
        }
    }
}

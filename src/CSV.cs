/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
#if NET20
        public const string VERSION = "NET20";
#elif NET40
        public const string VERSION = "NET40";
#elif NET45
        public const string VERSION = "NET45";
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
        /// The default TSV (tabe delimited file) text qualifier.  This is used to encode strings that contain the field delimiter.
        /// </summary>
        public const char DEFAULT_TSV_QUALIFIER = '"';


#region Methods to read CSV data
        /// <summary>
        /// Parse a single row of data from a CSV line into an array of objects, while permitting embedded newlines
        /// </summary>
        /// <param name="inStream">The stream to read</param>
        /// <param name="settings">The CSV settings to use for this parsing operation (Default: CSV)</param>
        /// <returns>An array containing all fields in the next row of data, or null if it could not be parsed.</returns>
        public static string[] ParseMultiLine(StreamReader inStream, CSVSettings settings = null)
        {
            StringBuilder sb = new StringBuilder();
            string[] array = null;
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
            // Ensure settings are non-null
            if (settings == null) settings = CSVSettings.CSV;

            // Okay, let's begin parsing
            List<string> list = new List<string>();
            var work = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                // If we are starting a new field, is this field text qualified?
                if ((c == settings.TextQualifier) && (work.Length == 0))
                {
                    int p2;
                    while (true)
                    {
                        p2 = line.IndexOf(settings.TextQualifier, i + 1);

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
            AddToken(list, work, settings);

            // Return the array we parsed
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
#endregion

#region CSV Output Functions
        /// <summary>
        /// Serialize a sequence of objects into a CSV string
        /// </summary>
        /// <returns>A single line of CSV encoded data containing these values</returns>
        /// <param name="row">A list or array of objects to serialize</param>
        /// <param name="settings">The field delimiter character (Default: comma)</param>
        public static string ToCSVString(this IEnumerable<object> row, CSVSettings settings = null)
        {
            StringBuilder sb = new StringBuilder();
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
            // Use CSV as default.
            if (settings == null) settings = CSVSettings.CSV;

            // Okay, let's begin
            StringBuilder sb = new StringBuilder();

            // Did the caller want the header row?
            if (settings.HeaderRowIncluded)
            {
                sb.AppendCSVHeader(typeof(T), settings);
                sb.Append(settings.LineSeparator);
            }

            // Let's go through the array of objects
            // Iterate through all the objects
            var values = new List<object>();
            foreach (T obj in list)
            {
                sb.AppendAsCSV<T>(obj, settings);
                sb.Append(settings.LineSeparator);
            }

            // Here's your data serialized in CSV format
            return sb.ToString();
        }
#endregion

#region StringBuilder append functions
        /// <summary>
        /// Add a CSV Header line to a StringBuilder
        /// </summary>
        /// <param name="sb">The stringbuilder to append data</param>
        /// <param name="type">The type of data to emit a header</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        public static void AppendCSVHeader(this StringBuilder sb, Type type, CSVSettings settings = null)
        {
            // Use CSV as default.
            if (settings == null) settings = CSVSettings.CSV;

            // Retrieve reflection information
            var filist = type.GetFields();
            var pilist = type.GetProperties();

            // Gather information about headers
            var headers = new List<object>();
            foreach (var fi in filist)
            {
                headers.Add(fi.Name);
            }
            foreach (var pi in pilist)
            {
                headers.Add(pi.Name);
            }
            AppendCSVRow(sb, headers, settings);
        }

        /// <summary>
        /// Appends a single object to a StringBuilder in CSV format as a single line
        /// </summary>
        /// <param name="sb">The stringbuilder to append data</param>
        /// <param name="obj">The single object to append in CSV-line format</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static void AppendAsCSV<T>(this StringBuilder sb, T obj, CSVSettings settings = null) where T : class, new()
        {
            // Skip any null objects
            if (obj == null) return;

            // Use CSV as default.
            if (settings == null) settings = CSVSettings.CSV;

            // Retrieve reflection information
            var type = typeof(T);
            var filist = type.GetFields();
            var pilist = type.GetProperties();

            // Retrieve all the fields and properties
            List<object> values = new List<object>();
            foreach (var fi in filist)
            {
                values.Add(fi.GetValue(obj));
            }
            foreach (var pi in pilist)
            {
                values.Add(pi.GetValue(obj, null));
            }

            // Output one line of CSV
            AppendCSVRow(sb, values, settings);
        }

        /// <summary>
        /// Append an array of objects to a StringBuilder in CSV format
        /// </summary>
        /// <param name="sb">The StringBuilder to append</param>
        /// <param name="row">The list of objects to append</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        private static void AppendCSVRow(this StringBuilder sb, IEnumerable<object> row, CSVSettings settings = null)
        {
            // Use CSV as default.
            if (settings == null) settings = CSVSettings.CSV;
            var q = settings.TextQualifier.ToString();

            // Okay, let's begin
            foreach (object o in row)
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
                string s = o.ToString();
                if (s.Length > 0)
                {

                    // Does this string contain any risky characters?  Risky is defined as delim, qual, or newline
                    if (settings.ForceQualifiers || (s.IndexOf(settings.FieldDelimiter) >= 0) || (s.IndexOf(settings.TextQualifier) >= 0) || s.Contains(settings.LineSeparator))
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
#endregion
    }
}

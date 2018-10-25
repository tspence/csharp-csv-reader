using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSVFile
{
    public static class CSVStream
    {
        #region FileStream related functions (not available on dot-net-portable)
        /// <summary>
        /// Serialize an object array to a stream in CSV format
        /// </summary>
        /// <param name="list">The object array to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="save_column_names">True if you wish the first line of the file to have column names</param>
        /// <param name="delim">The delimiter (comma, tab, pipe, etc) to separate fields</param>
        /// <param name="qual">The text qualifier (double-quote) that encapsulates fields that include delimiters</param>
#if DOTNET20
        public static void WriteToStream<T>(IEnumerable<T> list, StreamWriter sw, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#else
        public static void WriteToStream<T>(this IEnumerable<T> list, StreamWriter sw, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#endif
        {
            using (CSVWriter cw = new CSVWriter(sw, delim, qual))
            {
                cw.WriteObjects(list, save_column_names);
            }
        }

        /// <summary>
        /// Serialize an object array to a stream in CSV format
        /// </summary>
        /// <param name="list">The object array to write</param>
        /// <param name="filename">The stream where the CSV text will be written</param>
        /// <param name="save_column_names">True if you wish the first line of the file to have column names</param>
        /// <param name="delim">The delimiter (comma, tab, pipe, etc) to separate fields</param>
        /// <param name="qual">The text qualifier (double-quote) that encapsulates fields that include delimiters</param>
#if DOTNET20
        public static void WriteToStream<T>(IEnumerable<T> list, string filename, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#else
        public static void WriteToStream<T>(this IEnumerable<T> list, string filename, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#endif
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                WriteToStream<T>(list, sw, save_column_names, delim, qual);
            }
        }

        /// <summary>
        /// Serialize an object array to a string in CSV format
        /// </summary>
        /// <param name="list">The object array to write</param>
        /// <param name="save_column_names">True if you wish the first line of the file to have column names</param>
        /// <param name="delim">The delimiter (comma, tab, pipe, etc) to separate fields</param>
        /// <param name="qual">The text qualifier (double-quote) that encapsulates fields that include delimiters</param>
        /// <returns>The CSV string representing the object array.</returns>
#if DOTNET20
        public static string WriteToString<T>(IEnumerable<T> list, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#else
        public static string WriteToString<T>(this IEnumerable<T> list, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#endif
        {
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);
                var cw = new CSVWriter(sw, delim, qual);
                cw.WriteObjects(list, save_column_names);
                sw.Flush();
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Read in a single CSV file as an array of objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize from this CSV.</typeparam>
        /// <param name="filename">The path of the file to read.</param>
        /// <param name="ignore_dimension_errors">Set to true if you wish to ignore rows that have a different number of columns.</param>
        /// <param name="ignore_bad_columns">Set to true if you wish to ignore column headers that don't match up to object attributes.</param>
        /// <param name="ignore_type_conversion_errors">Set to true if you wish to overlook elements in the CSV array that can't be properly converted.</param>
        /// <param name="delim">The CSV field delimiter character.</param>
        /// <param name="qual">The CSV text qualifier character.</param>
        /// <returns>An array of objects that were retrieved from the CSV file.</returns>
        public static List<T> LoadArray<T>(string filename, bool ignore_dimension_errors = true, bool ignore_bad_columns = true, bool ignore_type_conversion_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER) where T : class, new()
        {
            return LoadArray<T>(new StreamReader(filename), ignore_dimension_errors, ignore_bad_columns, ignore_type_conversion_errors, delim, qual);
        }
        
        /// <summary>
        /// Read in a single CSV file as an array of objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize from this CSV.</typeparam>
        /// <param name="stream">The stream to read.</param>
        /// <param name="ignore_dimension_errors">Set to true if you wish to ignore rows that have a different number of columns.</param>
        /// <param name="ignore_bad_columns">Set to true if you wish to ignore column headers that don't match up to object attributes.</param>
        /// <param name="ignore_type_conversion_errors">Set to true if you wish to overlook elements in the CSV array that can't be properly converted.</param>
        /// <param name="delim">The CSV field delimiter character.</param>
        /// <param name="qual">The CSV text qualifier character.</param>
        /// <returns>An array of objects that were retrieved from the CSV file.</returns>
        public static List<T> LoadArray<T>(StreamReader stream, bool ignore_dimension_errors = true, bool ignore_bad_columns = true, bool ignore_type_conversion_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER) where T : class, new()
        {
            using (CSVReader cr = new CSVReader(stream, delim, qual))
            {
                return cr.Deserialize<T>(ignore_dimension_errors, ignore_bad_columns, ignore_type_conversion_errors);
            }
        }
        #endregion

        #region Minimal portable functions
        /// <summary>
        /// Convert a CSV file (in string form) into a list of string arrays 
        /// </summary>
        /// <param name="source_string"></param>
        /// <param name="first_row_are_headers"></param>
        /// <param name="ignore_dimension_errors"></param>
        /// <returns></returns>
        public static List<string[]> LoadString(string source_string, bool first_row_are_headers, bool ignore_dimension_errors)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(source_string);
            MemoryStream stream = new MemoryStream(byteArray);
            var results = new List<string[]>();
            using (CSVReader cr = new CSVReader(new StreamReader(stream))) {
                foreach (var line in cr) {
                    results.Add(line);
                }
            }
            return results;
        }
        #endregion
    }
}

/*
 * 2006 - 2012 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://code.google.com/p/csharp-csv-reader/
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;

namespace CSVFile
{
    public static class CSV
    {
        public const char DEFAULT_DELIMITER = ',';
        public const char DEFAULT_QUALIFIER = '"';
        public const char DEFAULT_TAB_DELIMITER = '\t';

        #region Reading CSV Formatted Data
        /// <summary>
        /// Parse a line whose values may include newline symbols or CR/LF
        /// </summary>
        /// <param name="sr"></param>
        /// <returns></returns>
        public static string[] ParseMultiLine(StreamReader sr, char delimiter = DEFAULT_DELIMITER, char text_qualifier = DEFAULT_QUALIFIER)
        {
            StringBuilder sb = new StringBuilder();
            string[] array = null;
            while (!sr.EndOfStream) {

                // Read in a line
                sb.Append(sr.ReadLine());

                // Does it parse?
                string s = sb.ToString();
                if (TryParseLine(s, delimiter, text_qualifier, out array)) {
                    return array;
                }

                // We didn't succeed on the first try - our line must have an embedded newline in it
                sb.Append("\n");
            }

            // Fails to parse - return the best array we were able to get
            return array;
        }

        /// <summary>
        /// Parse the line and return the array if it succeeds, or as best as we can get
        /// </summary>
        /// <param name="s"></param>
        /// <param name="delimiter"></param>
        /// <param name="text_qualifier"></param>
        /// <returns></returns>
        public static string[] ParseLine(string s, char delimiter = DEFAULT_DELIMITER, char text_qualifier = DEFAULT_QUALIFIER)
        {
            string[] array = null;
            TryParseLine(s, delimiter, text_qualifier, out array);
            return array;
        }

        /// <summary>
        /// Read in a line of text, and use the Add() function to add these items to the current CSV structure
        /// </summary>
        /// <param name="s"></param>
        public static bool TryParseLine(string s, char delimiter, char text_qualifier, out string[] array)
        {
            bool success = true;
            List<string> list = new List<string>();
            StringBuilder work = new StringBuilder();
            for (int i = 0; i < s.Length; i++) {
                char c = s[i];

                // If we are starting a new field, is this field text qualified?
                if ((c == text_qualifier) && (work.Length == 0)) {
                    int p2;
                    while (true) {
                        p2 = s.IndexOf(text_qualifier, i + 1);

                        // for some reason, this text qualifier is broken
                        if (p2 < 0) {
                            work.Append(s.Substring(i + 1));
                            i = s.Length;
                            success = false;
                            break;
                        }

                        // Append this qualified string
                        work.Append(s.Substring(i + 1, p2 - i - 1));
                        i = p2;

                        // If this is a double quote, keep going!
                        if (((p2 + 1) < s.Length) && (s[p2 + 1] == text_qualifier)) {
                            work.Append(text_qualifier);
                            i++;

                            // otherwise, this is a single qualifier, we're done
                        } else {
                            break;
                        }
                    }

                    // Does this start a new field?
                } else if (c == delimiter) {
                    list.Add(work.ToString());
                    work.Length = 0;

                    // Test for special case: when the user has written a casual comma, space, and text qualifier, skip the space
                    // Checks if the second parameter of the if statement will pass through successfully
                    // e.g. "bob", "mary", "bill"
                    if (i + 2 <= s.Length - 1) {
                        if (s[i + 1].Equals(' ') && s[i + 2].Equals(text_qualifier)) {
                            i++;
                        }
                    }
                } else {
                    work.Append(c);
                }
            }
            list.Add(work.ToString());

            // If we have nothing in the list, and it's possible that this might be a tab delimited list, try that before giving up
            if (list.Count == 1 && delimiter != DEFAULT_TAB_DELIMITER) {
                string[] tab_delimited_array = ParseLine(s, DEFAULT_TAB_DELIMITER, DEFAULT_QUALIFIER);
                if (tab_delimited_array.Length > list.Count) {
                    array = tab_delimited_array;
                    return success;
                }
            }

            // Return the array we parsed
            array = list.ToArray();
            return success;
        }
        #endregion

        #region Output CSV formatted files from raw data
        /// <summary>
        /// Write a data table to disk at the designated file name in CSV format
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fn"></param>
        public static void SaveAsCSV(this DataTable dt, string filename, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
        {
            using (StreamWriter sw = new StreamWriter(filename)) {
                WriteToStream(dt, sw, save_column_names, delim, qual);
            }
        }

        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="save_column_names">True if you wish the first line of the file to have column names</param>
        /// <param name="delim">The delimiter (comma, tab, pipe, etc) to separate fields</param>
        /// <param name="qual">The text qualifier (double-quote) that encapsulates fields that include delimiters</param>
        public static void WriteToStream(this DataTable dt, StreamWriter sw, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
        {
            // Write headers, if the caller requested we do so
            if (save_column_names) {
                List<string> headers = new List<string>();
                foreach (DataColumn col in dt.Columns) {
                    headers.Add(col.ColumnName);
                }
                sw.WriteLine(Output(headers, delim, qual));
            }

            // Now produce the rows
            foreach (DataRow dr in dt.Rows) {
                sw.WriteLine(Output(dr.ItemArray, delim, qual));
            }
        }

        /// <summary>
        /// Output a single field value as appropriate
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Output(IEnumerable<object> line, char delimiter, char qualifier)
        {
            StringBuilder sb = new StringBuilder();
            foreach (object o in line) {

                // Null strings are just a delimiter
                if (o != null) {
                    string s = o.ToString();
                    if (s.Length > 0) {

                        // Does this string contain any risky characters?  Risky is defined as delim, qual, or newline
                        if (s.Contains(delimiter) || s.Contains(qualifier) || s.Contains(Environment.NewLine)) {
                            sb.Append(qualifier);

                            // Double up any qualifiers that may occur
                            sb.Append(s.Replace(qualifier.ToString(), qualifier.ToString() + qualifier.ToString()));
                            sb.Append(qualifier);
                        } else {
                            sb.Append(s);
                        }
                    }
                }

                // Move to the next cell
                sb.Append(delimiter);
            }

            // Subtract the trailing delimiter so we don't inadvertently add a column
            sb.Length -= 1;
            return sb.ToString();
        }
        #endregion
    }
}

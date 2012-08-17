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
using System.Reflection;

namespace CSVFile
{
    public class CSVReader : IDisposable
    {
        protected char _delimiter, _text_qualifier;

        protected StreamReader _instream;

        protected static readonly List<string> allowed_extensions = new List<string>() { ".csv", ".tsv", ".data" };

        #region Constructors
        /// <summary>
        /// Construct a new CSV reader off a streamed source
        /// </summary>
        public CSVReader(StreamReader source, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            _instream = source;
            _delimiter = delim;
            _text_qualifier = qual;
        }

        /// <summary>
        /// Construct a new CSV reader off a streamed source
        /// </summary>
        public CSVReader(Stream source, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            _instream = new StreamReader(source);
            _delimiter = delim;
            _text_qualifier = qual;
        }

        /// <summary>
        /// Initialize a new CSV file structure in memory
        /// </summary>
        public CSVReader(string filename, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            _instream = new StreamReader(filename);
            _delimiter = delim;
            _text_qualifier = qual;

        }
        #endregion

        #region Iterate through a CSV File
        /// <summary>
        /// Iterate through all lines in this CSV file
        /// </summary>
        /// <returns></returns>
        IEnumerable<string[]> Lines()
        {
            while (true) {

                // Attempt to parse the line successfully
                string[] line = CSV.ParseMultiLine(_instream, _delimiter, _text_qualifier);

                // If we were unable to parse the line successfully, that's all the file has
                if (line == null) break;

                // We got something - give the caller an object
                yield return line;
            }
        }
        #endregion

        #region Read a file into a data table
        /// <summary>
        /// Read this file into a data table in memory
        /// </summary>
        /// <param name="first_row_are_headers"></param>
        /// <returns></returns>
        public DataTable ReadAsDataTable(bool first_row_are_headers, bool ignore_dimension_errors)
        {
            DataTable dt = new DataTable();

            // Read in the first line
            string[] first_line = CSV.ParseMultiLine(_instream, _delimiter, _text_qualifier);
            int num_columns = first_line.Length;

            // File contains column names - so name each column properly
            if (first_row_are_headers) {
                foreach (string header in first_line) {
                    dt.Columns.Add(new DataColumn(header, typeof(string)));
                }

            // Okay, just create some untitled columns
            } else {
                for (int i = 0; i < num_columns; i++) {
                    dt.Columns.Add(new DataColumn(String.Format("Column{0}", i), typeof(string)));
                }

                // Add this first line
                dt.Rows.Add(first_line);
            }

            // Start reading through the file
            int row_num = 1;
            foreach (string[] line in Lines()) {

                // Does this line match the length of the first line?
                if (line.Length != num_columns) {
                    if (!ignore_dimension_errors) {
                        throw new Exception(String.Format("Line #{0} contains {1} columns; expected {2}", row_num, line.Length, num_columns));
                    } else {

                        // Add as best we can - construct a new line and make it fit
                        List<string> list = new List<string>();
                        list.AddRange(line);
                        while (list.Count < num_columns) {
                            list.Add("");
                        }
                        dt.Rows.Add(list.GetRange(0, num_columns).ToArray());
                    }
                } else {
                    dt.Rows.Add(line);
                }

                // Keep track of where we are in the file
                row_num++;
            }

            // Here's your data table
            return dt;
        }
        #endregion

        #region Static interface
        /// <summary>
        /// Read in a single CSV file into a datatable in memory in one call
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delim"></param>
        /// <param name="qual"></param>
        /// <returns></returns>
        public static DataTable LoadDataTable(string filename, bool first_row_are_headers = true, bool ignore_dimension_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            return LoadDataTable(new StreamReader(filename), first_row_are_headers, ignore_dimension_errors, delim, qual);
        }

        /// <summary>
        /// Read in a single CSV file into a datatable in memory in one call
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delim"></param>
        /// <param name="qual"></param>
        /// <returns></returns>
        public static DataTable LoadDataTable(StreamReader stream, bool first_row_are_headers = true, bool ignore_dimension_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            using (CSVReader cr = new CSVReader(stream, delim, qual)) {
                return cr.ReadAsDataTable(first_row_are_headers, ignore_dimension_errors);
            }
        }
        #endregion

        #region Disposables
        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _instream.Close();
            _instream.Dispose();
        }
        #endregion

        #region Deserialization
        /// <summary>
        /// Deserialize a CSV file into a list of typed objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> Deserialize<T>(bool ignore_dimension_errors = false, bool ignore_bad_columns = false, bool ignore_type_conversion_errors = false) where T : class, new()
        {
            List<T> result = new List<T>();
            Type return_type = typeof(T);

            // Read in the first line - we have to have headers!
            string[] first_line = CSV.ParseMultiLine(_instream, _delimiter, _text_qualifier);
            int num_columns = first_line.Length;

            // Determine how to handle each column in the file - check properties, fields, and methods
            Type[] column_types = new Type[num_columns];
            PropertyInfo[] prop_handlers = new PropertyInfo[num_columns];
            FieldInfo[] field_handlers = new FieldInfo[num_columns];
            MethodInfo[] method_handlers = new MethodInfo[num_columns];
            for (int i = 0; i < num_columns; i++) {
                prop_handlers[i] = return_type.GetProperty(first_line[i]);

                // If we failed to get a property handler, let's try a field handler
                if (prop_handlers[i] == null) {
                    field_handlers[i] = return_type.GetField(first_line[i]);

                    // If we failed to get a field handler, let's try a method
                    if (field_handlers[i] == null) {

                        // Methods must be treated differently - we have to ensure that the method has a single parameter
                        MethodInfo mi = return_type.GetMethod(first_line[i]);
                        if (mi != null) {
                            if (mi.GetParameters().Length == 1) {
                                method_handlers[i] = mi;
                                column_types[i] = mi.GetParameters()[0].ParameterType;
                            } else if (!ignore_bad_columns) {
                                throw new Exception(String.Format("The column header '{0}' matched a method with more than one parameter.", first_line[i]));
                            }

                        // Does the caller want us to throw an error on bad columns?
                        } else if (!ignore_bad_columns) {
                            throw new Exception(String.Format("The column header '{0}' was not found in the class '{1}'.", first_line[i], return_type.FullName));
                        }
                    } else {
                        column_types[i] = field_handlers[i].FieldType;
                    }
                } else {
                    column_types[i] = prop_handlers[i].PropertyType;
                }
            }

            // Alright, let's retrieve CSV lines and parse each one!
            int row_num = 1;
            foreach (string[] line in Lines()) {

                // Does this line match the length of the first line?  Does the caller want us to complain?
                if (line.Length != num_columns) {
                    if (!ignore_dimension_errors) {
                        throw new Exception(String.Format("Line #{0} contains {1} columns; expected {2}", row_num, line.Length, num_columns));
                    }
                }

                // Construct a new object and execute each column on it
                T obj = new T();
                for (int i = 0; i < Math.Min(line.Length, num_columns); i++) {

                    // Attempt to convert this to the specified type
                    object value = null;
                    if (ignore_type_conversion_errors) {
                        try {
                            value = Convert.ChangeType(line[i], column_types[i]);
                        } catch {
                            // ignore this error
                        }
                    } else {
                        value = Convert.ChangeType(line[i], column_types[i]);
                    }

                    // Can we set this value to the object as a property?
                    if (prop_handlers[i] != null) {
                        prop_handlers[i].SetValue(obj, value, null);

                    // Can we set this value to the object as a property?
                    } else if (field_handlers[i] != null) {
                        field_handlers[i].SetValue(obj, value);

                    // Can we set this value to the object as a property?
                    } else if (method_handlers[i] != null) {
                        method_handlers[i].Invoke(obj, new object[] { value });
                    }
                }

                // Keep track of where we are in the file
                result.Add(obj);
                row_num++;
            }

            // Here's your array!
            return result;
        }
        #endregion
    }
}

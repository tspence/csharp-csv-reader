/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if HAS_DATATABLE
using System.Data;
#endif
using System.Reflection;
using System.ComponentModel;

namespace CSVFile
{
    public class CSVReader : IEnumerable<string[]>, IDisposable
    {
        protected CSVSettings _settings;

        protected StreamReader _instream;

        #region Public Variables
        /// <summary>
        /// If the first row in the file is a header row, this will be populated
        /// </summary>
        public string[] Headers = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Construct a new CSV reader off a streamed source
        /// </summary>
        /// <param name="source">The stream source</param>
        /// <param name="settings">The CSV settings to use for this reader (Default: CSV)</param>
        public CSVReader(StreamReader source, CSVSettings settings = null)
        {
            _instream = source;
            _settings = settings;
            if (_settings == null) _settings = CSVSettings.CSV;

            // Do we need to parse headers?
            if (_settings.HeaderRowIncluded)
            {
                Headers = NextLine();
            }
            else
            {
                Headers = _settings.AssumedHeaders?.ToArray();
            }
        }
        #endregion

        #region Iterate through a CSV File
        /// <summary>
        /// Iterate through all lines in this CSV file
        /// </summary>
        /// <returns>An array of all data columns in the line</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Lines().GetEnumerator();
        }

        /// <summary>
        /// Iterate through all lines in this CSV file
        /// </summary>
        /// <returns>An array of all data columns in the line</returns>
        IEnumerator<string[]> System.Collections.Generic.IEnumerable<string[]>.GetEnumerator()
        {
            return Lines().GetEnumerator();
        }

        /// <summary>
        /// Iterate through all lines in this CSV file
        /// </summary>
        /// <returns>An array of all data columns in the line</returns>
        public IEnumerable<string[]> Lines()
        {
            while (true)
            {

                // Attempt to parse the line successfully
                string[] line = NextLine();

                // If we were unable to parse the line successfully, that's all the file has
                if (line == null) break;

                // We got something - give the caller an object
                yield return line;
            }
        }

        /// <summary>
        /// Retrieve the next line from the file.
        /// </summary>
        /// <returns>One line from the file.</returns>
        public string[] NextLine()
        {
            return CSV.ParseMultiLine(_instream, _settings);
        }
        #endregion

#region Read a file into a data table
#if HAS_DATATABLE
        /// <summary>
        /// Read this file into a data table in memory
        /// </summary>
        /// <returns></returns>
        public DataTable ReadAsDataTable()
        {
            DataTable dt = new DataTable();
            string[] firstLine = null;

            // File contains column names - so name each column properly
            if (Headers == null)
            {
                firstLine = NextLine();
                var list = new List<string>();
                for (int i = 0; i < firstLine.Length; i++) {
                    list.Add($"Column{i}");
                }
            }

            // Add headers
            int numColumns = Headers.Length;
            for (int i = 0; i < Headers.Length; i++)
            {
                dt.Columns.Add(new DataColumn(Headers[i], typeof(string)));
            }

            // If we had to read the first line to get dimensions, add it
            int row_num = 1;
            if (firstLine != null)
            {
                dt.Rows.Add(firstLine);
                row_num++;
            }

            // Start reading through the file
            foreach (var line in Lines()) {

                // Does this line match the length of the first line?
                if (line.Length != numColumns) {
                    if (!_settings.IgnoreDimensionErrors) {
                        throw new Exception($"Line #{row_num} contains {line.Length} columns; expected {numColumns}");
                    } else {

                        // Add as best we can - construct a new line and make it fit
                        List<string> list = new List<string>();
                        list.AddRange(line);
                        while (list.Count < numColumns) {
                            list.Add("");
                        }
                        dt.Rows.Add(list.GetRange(0, numColumns).ToArray());
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
#endif

        /// <summary>
        /// Deserialize the CSV reader into a generic list
        /// </summary>
        public List<T> Deserialize<T>() where T : class, new()
        {
            List<T> result = new List<T>();
            Type return_type = typeof(T);

            // Read in the first line - we have to have headers!
            if (Headers == null) throw new Exception("CSV must have headers to be deserialized");
            int num_columns = Headers.Length;

            // Determine how to handle each column in the file - check properties, fields, and methods
            Type[] column_types = new Type[num_columns];
            TypeConverter[] column_convert = new TypeConverter[num_columns];
            PropertyInfo[] prop_handlers = new PropertyInfo[num_columns];
            FieldInfo[] field_handlers = new FieldInfo[num_columns];
            MethodInfo[] method_handlers = new MethodInfo[num_columns];
            for (int i = 0; i < num_columns; i++)
            {
                prop_handlers[i] = return_type.GetProperty(Headers[i]);

                // If we failed to get a property handler, let's try a field handler
                if (prop_handlers[i] == null)
                {
                    field_handlers[i] = return_type.GetField(Headers[i]);

                    // If we failed to get a field handler, let's try a method
                    if (field_handlers[i] == null)
                    {

                        // Methods must be treated differently - we have to ensure that the method has a single parameter
                        MethodInfo mi = return_type.GetMethod(Headers[i]);
                        if (mi != null)
                        {
                            if (mi.GetParameters().Length == 1)
                            {
                                method_handlers[i] = mi;
                                column_types[i] = mi.GetParameters()[0].ParameterType;
                            }
                            else if (!_settings.IgnoreHeaderErrors)
                            {
                                throw new Exception($"The column header '{Headers[i]}' matched a method with more than one parameter.");
                            }

                            // Does the caller want us to throw an error on bad columns?
                        }
                        else if (!_settings.IgnoreHeaderErrors)
                        {
                            throw new Exception($"The column header '{Headers[i]}' was not found in the class '{return_type.FullName}'.");
                        }
                    }
                    else
                    {
                        column_types[i] = field_handlers[i].FieldType;
                    }
                }
                else
                {
                    column_types[i] = prop_handlers[i].PropertyType;
                }

                // Retrieve a converter
                if (column_types[i] != null)
                {
                    column_convert[i] = TypeDescriptor.GetConverter(column_types[i]);
                    if ((column_convert[i] == null) && !_settings.IgnoreHeaderErrors)
                    {
                        throw new Exception($"The column {Headers[i]} (type {column_types[i]}) does not have a type converter.");
                    }
                }
            }

            // Alright, let's retrieve CSV lines and parse each one!
            int row_num = 1;
            foreach (string[] line in Lines())
            {

                // Does this line match the length of the first line?  Does the caller want us to complain?
                if ((line.Length != num_columns) && !_settings.IgnoreHeaderErrors) {
                    throw new Exception($"Line #{row_num} contains {line.Length} columns; expected {num_columns}");
                }

                // Construct a new object and execute each column on it
                T obj = new T();
                for (int i = 0; i < Math.Min(line.Length, num_columns); i++)
                {

                    // Attempt to convert this to the specified type
                    object value = null;
                    if (_settings.AllowNull && (line[i] == null))
                    {
                        value = null;
                    } 
                    else if (column_convert[i] != null && column_convert[i].IsValid(line[i]))
                    {
                        value = column_convert[i].ConvertFromString(line[i]);
                    }
                    else if (!_settings.IgnoreHeaderErrors)
                    {
                        throw new Exception(String.Format("The value '{0}' cannot be converted to the type {1}.", line[i], column_types[i]));
                    }

                    // Can we set this value to the object as a property?
                    if (prop_handlers[i] != null)
                    {
                        prop_handlers[i].SetValue(obj, value, null);

                        // Can we set this value to the object as a property?
                    }
                    else if (field_handlers[i] != null)
                    {
                        field_handlers[i].SetValue(obj, value);

                        // Can we set this value to the object as a property?
                    }
                    else if (method_handlers[i] != null)
                    {
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

#region Disposal
        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _instream.Dispose();
        }
#endregion

#region Chopping a CSV file into chunks
        /// <summary>
        /// Take a CSV file and chop it into multiple chunks of a specified maximum size.
        /// </summary>
        /// <param name="filename">The input filename to chop</param>
        /// <param name="out_folder">The folder where the chopped CSV will be saved</param>
        /// <param name="maxLinesPerFile">The maximum number of lines to put into each file</param>
        /// <param name="settings">The CSV settings to use when chopping this file into chunks (Default: CSV)</param>
        /// <returns>Number of files chopped</returns>
        public static int ChopFile(string filename, string out_folder, int maxLinesPerFile, CSVSettings settings = null)
        {
            // Default settings
            if (settings == null) settings = CSVSettings.CSV;

            // Let's begin parsing
            int file_id = 1;
            int line_count = 0;
            string file_prefix = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);
            CSVWriter cw = null;
            StreamWriter sw = null;

            // Read in lines from the file
            using (var sr = new StreamReader(filename))
            {
                using (CSVReader cr = new CSVReader(sr, settings))
                {

                    // Okay, let's do the real work
                    foreach (string[] line in cr.Lines())
                    {

                        // Do we need to create a file for writing?
                        if (cw == null)
                        {
                            string fn = Path.Combine(out_folder, file_prefix + file_id.ToString() + ext);
                            sw = new StreamWriter(fn);
                            cw = new CSVWriter(sw, settings);
                            if (settings.HeaderRowIncluded)
                            {
                                cw.WriteLine(cr.Headers);
                            }
                        }

                        // Write one line
                        cw.WriteLine(line);

                        // Count lines - close the file if done
                        line_count++;
                        if (line_count >= maxLinesPerFile)
                        {
                            cw.Dispose();
                            cw = null;
                            file_id++;
                            line_count = 0;
                        }
                    }
                }
            }

            // Ensore the final CSVWriter is closed properly
            if (cw != null)
            {
                cw.Dispose();
                cw = null;
            }
            return file_id;
        }
#endregion
    }
}

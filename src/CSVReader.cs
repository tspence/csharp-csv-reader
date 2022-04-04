/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Text;
using System.Threading;
#if HAS_ASYNC
using System.Threading.Tasks;
#endif

// These suggestions from Resharper apply because we don't want it to recommend fixing things needed for Net20:
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment
// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToNullCoalescingExpression

namespace CSVFile
{
    /// <summary>
    /// A reader that reads from a stream and emits CSV records
    /// </summary>
#if HAS_ASYNC_IENUM
    public class CSVReader : IAsyncEnumerable<string[]>, IEnumerable<string[]>, IDisposable
#else
    public class CSVReader : IEnumerable<string[]>, IDisposable
#endif
    
    {
        private readonly CSVSettings _settings;
        private readonly StreamReader _stream;

        /// <summary>
        /// If the first row in the file is a header row, this will be populated
        /// </summary>
        public string[] Headers { get; private set; }
        
        /// <summary>
        /// Returns the settings currently in use for this reader
        /// </summary>
        public CSVSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Convenience function to read from a string
        /// </summary>
        /// <param name="source">The string to read</param>
        /// <param name="settings">The CSV settings to use for this reader (Default: CSV)</param>
        /// <param name="encoding">The string encoding to use for the reader (Default: UTF8)</param>
        /// <returns></returns>
        public static CSVReader FromString(string source, CSVSettings settings = null, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            var byteArray = encoding.GetBytes(source);
            var stream = new MemoryStream(byteArray);
            var sr = new StreamReader(stream);
            return new CSVReader(sr, settings);
        }

        /// <summary>
        /// Convenience function to read from a file on disk
        /// </summary>
        /// <param name="filename">The file to read</param>
        /// <param name="settings">The CSV settings to use for this reader (Default: CSV)</param>
        /// <param name="encoding">The string encoding to use for the reader (Default: UTF8)</param>
        /// <returns></returns>
        public static CSVReader FromFile(string filename, CSVSettings settings = null, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            var sr = new StreamReader(filename, encoding);
            return new CSVReader(sr, settings);
        }
        
        /// <summary>
        /// Construct a new CSV reader off a streamed source
        /// </summary>
        /// <param name="source">The stream source. Note that when disposed, the CSV Reader will dispose the stream reader.</param>
        /// <param name="settings">The CSV settings to use for this reader (Default: CSV)</param>
        public CSVReader(StreamReader source, CSVSettings settings = null)
        {
            _stream = source;
            _settings = settings;
            if (_settings == null)
            {
                _settings = CSVSettings.CSV;
            }

            // Do we need to parse headers?
            if (_settings.HeaderRowIncluded)
            {
                var line = source.ReadLine();
                if (_settings.AllowSepLine)
                {
                    var newDelimiter = CSV.ParseSepLine(line);
                    if (newDelimiter != null)
                    {
                        // We don't want to change the original settings, since they may be a singleton
                        _settings = _settings.CloneWithNewDelimiter(newDelimiter.Value);
                        line = source.ReadLine();
                    }
                }

                Headers = CSV.ParseLine(line, _settings);
            }
            else
            {
                Headers = _settings.AssumedHeaders;
            }
        }

        /// <summary>
        /// Iterate through all lines in this CSV file
        /// </summary>
        /// <returns>An array of all data columns in the line</returns>
        public IEnumerable<string[]> Lines()
        {
            return CSV.ParseStream(_stream, _settings);
        }

        /// <summary>
        /// Iterate through all lines in this CSV file
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string[]> GetEnumerator()
        {
            return CSV.ParseStream(_stream, _settings).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
#if HAS_ASYNC_IENUM
        /// <summary>
        /// Iterate through all lines in this CSV file using async
        /// </summary>
        /// <returns>An array of all data columns in the line</returns>
        public IAsyncEnumerable<string[]> LinesAsync()
        {
            return CSV.ParseStreamAsync(_stream, _settings);
        }

        /// <summary>
        /// Iterate through all lines in this CSV file using async
        /// </summary>
        /// <returns>An array of all data columns in the line</returns>
        public IAsyncEnumerator<string[]> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return CSV.ParseStreamAsync(_stream, _settings).GetAsyncEnumerator(cancellationToken);
        }

        /// <summary>
        /// Deserialize the CSV reader into a generic list
        /// </summary>
        /// <typeparam name="T">The type of data to deserialize</typeparam>
        /// <returns>A streaming collection of records from the CSV source</returns>
        /// <exception cref="Exception">If the CSV source cannot be parsed into the type, throws exceptions</exception>
        public async IAsyncEnumerable<T> DeserializeAsync<T>() where T : class, new()
        {
            var helper = SetupDeserializer<T>();

            // Alright, let's retrieve CSV lines and parse each one!
            var row_num = 0;
            await foreach (var line in this)
            {
                row_num++;
                var obj = helper.Deserialize<T>(line, row_num, _settings);
                if (obj != null)
                {
                    yield return obj;
                }
            }
        }
#endif


        /// <summary>
        /// Read this file into a data table in memory
        /// </summary>
        /// <returns></returns>
        public DataTable ReadAsDataTable()
        {
            var dt = new DataTable();
            string[] firstLine = null;

            // File contains column names - so name each column properly
            if (Headers == null)
            {
                var rawLine = _stream.ReadLine();
                firstLine = CSV.ParseLine(rawLine, _settings);
                var list = new List<string>();
                for (var i = 0; i < firstLine.Length; i++) {
                    list.Add($"Column{i}");
                }

                this.Headers = list.ToArray();
            }

            // Add headers
            var numColumns = Headers.Length;
            foreach (var t in Headers)
            {
                dt.Columns.Add(new DataColumn(t, typeof(string)));
            }

            // If we had to read the first line to get dimensions, add it
            var row_num = 1;
            if (firstLine != null)
            {
                dt.Rows.Add(firstLine);
                row_num++;
            }

            // Start reading through the file
            foreach (var line in CSV.ParseStream(_stream, _settings)) {

                // Does this line match the length of the first line?
                if (line.Length != numColumns) {
                    if (!_settings.IgnoreDimensionErrors) {
                        throw new Exception($"Line #{row_num} contains {line.Length} columns; expected {numColumns}");
                    } else {

                        // Add as best we can - construct a new line and make it fit
                        var list = new List<string>();
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

        /// <summary>
        /// Deserialize the CSV reader into a generic list
        /// </summary>
        /// <typeparam name="T">The type to deserialize</typeparam>
        /// <returns>A streaming collection of objects as they are read from the source</returns>
        /// <exception cref="Exception">If the CSV formatting does not match the object, throw errors</exception>
        public IEnumerable<T> Deserialize<T>() where T : class, new()
        {
            var helper = SetupDeserializer<T>();

            // Alright, let's retrieve CSV lines and parse each one!
            var row_num = 0;
            foreach (var line in this)
            {
                row_num++;
                var obj = helper.Deserialize<T>(line, row_num, _settings);
                if (obj != null)
                {
                    yield return obj;
                }
            }
        }

        private class DeserializationHelper
        {
            public int NumColumns;
            public Type[] ColumnTypes;
            public TypeConverter[] Converters;
            public PropertyInfo[] Properties;
            public FieldInfo[] Fields;
            public MethodInfo[] Methods;

            /// <summary>
            /// Deserialize a single row using precomputed converters
            /// </summary>
            /// <param name="line"></param>
            /// <param name="row_num"></param>
            /// <param name="settings"></param>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public T Deserialize<T>(string[] line, int row_num, CSVSettings settings) where T : class, new()
            {
                // If this line is completely empty, do our settings permit us to ignore the empty line?
                if (line.Length == 0 || (line.Length == 1 && line[0] == string.Empty) && settings.IgnoreEmptyLineForDeserialization)
                {
                    return null;
                }

                // Does this line match the length of the first line?  Does the caller want us to complain?
                if (line.Length != NumColumns && !settings.IgnoreHeaderErrors) {
                    throw new Exception($"Line #{row_num} contains {line.Length} columns; expected {NumColumns}");
                }

                // Construct a new object and execute each column on it
                var obj = new T();
                for (var i = 0; i < Math.Min(line.Length, NumColumns); i++)
                {

                    // Attempt to convert this to the specified type
                    object value = null;
                    if (settings.AllowNull && (line[i] == null || line[i] == settings.NullToken))
                    {
                        value = null;
                    } 
                    else if (Converters[i] != null && Converters[i].IsValid(line[i]))
                    {
                        value = Converters[i].ConvertFromString(line[i]);
                    }
                    else if (!settings.IgnoreHeaderErrors)
                    {
                        throw new Exception($"The value '{line[i]}' cannot be converted to the type {ColumnTypes[i]}.");
                    }

                    // Can we set this value to the object as a property?
                    if (Properties[i] != null)
                    {
                        Properties[i].SetValue(obj, value, null);
                    }
                    else if (Fields[i] != null)
                    {
                        Fields[i].SetValue(obj, value);
                    }
                    else if (Methods[i] != null)
                    {
                        Methods[i].Invoke(obj, new object[] { value });
                    }
                }

                return obj;
            }
        }
        
        private DeserializationHelper SetupDeserializer<T>()
            where T : class, new()
        {
            var helper = new DeserializationHelper();
            var return_type = typeof(T);

            // We have to have headers!
            if (Headers == null) throw new Exception("CSV must have headers to be deserialized");
            helper.NumColumns = Headers.Length;

            // Set binding flags correctly
            var bindings = BindingFlags.Public | BindingFlags.Instance;
            if (!_settings.HeadersCaseSensitive)
            {
                bindings |= BindingFlags.IgnoreCase;
            }

            // Determine how to handle each column in the file - check properties, fields, and methods
            helper.ColumnTypes = new Type[helper.NumColumns];
            helper.Converters = new TypeConverter[helper.NumColumns];
            helper.Properties = new PropertyInfo[helper.NumColumns];
            helper.Fields = new FieldInfo[helper.NumColumns];
            helper.Methods = new MethodInfo[helper.NumColumns];
            for (var i = 0; i < helper.NumColumns; i++)
            {
                helper.Properties[i] = return_type.GetProperty(Headers[i], bindings);
                
                // Throw an easy-to-understand error for properties that lack a setter
                if (helper.Properties[i] != null && !helper.Properties[i].CanWrite)
                {
                    if (_settings.IgnoreReadOnlyProperties && _settings.IgnoreHeaderErrors)
                    {
                        helper.Properties[i] = null;
                        continue;
                    }
                    throw new Exception($"The column header '{Headers[i]}' matches a read-only property. To ignore this exception, enable IgnoreReadOnlyProperties and IgnoreHeaderErrors.");
                }

                // If we failed to get a property handler, let's try a field handler
                if (helper.Properties[i] == null)
                {
                    helper.Fields[i] = return_type.GetField(Headers[i], bindings);

                    // If we failed to get a field handler, let's try a method
                    if (helper.Fields[i] == null)
                    {
                        // Methods must be treated differently - we have to ensure that the method has a single parameter
                        var mi = return_type.GetMethod(Headers[i], bindings);
                        if (mi != null)
                        {
                            if (mi.GetParameters().Length == 1)
                            {
                                helper.Methods[i] = mi;
                                helper.ColumnTypes[i] = mi.GetParameters()[0].ParameterType;
                            }
                            else if (!_settings.IgnoreHeaderErrors)
                            {
                                throw new Exception(
                                    $"The column header '{Headers[i]}' matched a method with more than one parameter.");
                            }
                        }
                        else if (!_settings.IgnoreHeaderErrors)
                        {
                            throw new Exception(
                                $"The column header '{Headers[i]}' was not found in the class '{return_type.FullName}'.");
                        }
                    }
                    else
                    {
                        helper.ColumnTypes[i] = helper.Fields[i].FieldType;
                    }
                }
                else
                {
                    helper.ColumnTypes[i] = helper.Properties[i].PropertyType;
                }

                if (helper.ColumnTypes[i] != null)
                {
                    helper.Converters[i] = TypeDescriptor.GetConverter(helper.ColumnTypes[i]);
                    if ((helper.Converters[i] == null) && !_settings.IgnoreHeaderErrors)
                    {
                        throw new Exception(
                            $"The column {Headers[i]} (type {helper.ColumnTypes[i]}) does not have a type converter.");
                    }
                }
            }

            return helper;
        }

        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }

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
            var file_id = 1;
            var line_count = 0;
            var file_prefix = Path.GetFileNameWithoutExtension(filename);
            var ext = Path.GetExtension(filename);
            CSVWriter cw = null;
            StreamWriter sw = null;

            // Read in lines from the file
            using (var sr = new StreamReader(filename))
            {
                using (var cr = new CSVReader(sr, settings))
                {

                    // Okay, let's do the real work
                    foreach (string[] line in cr.Lines())
                    {

                        // Do we need to create a file for writing?
                        if (cw == null)
                        {
                            var fn = Path.Combine(out_folder, file_prefix + file_id.ToString() + ext);
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

            // Ensure the final CSVWriter is closed properly
            if (cw != null)
            {
                cw.Dispose();
                cw = null;
            }
            return file_id;
        }
    }
}

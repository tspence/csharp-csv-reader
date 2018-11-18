/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ComponentModel;
#if HAS_ASYNC
using System.Threading.Tasks;
#endif

namespace CSVFile
{
    /// <summary>
    /// Read a stream in as CSV, parsing it to an enumerable
    /// </summary>
    public class CSVReader : IDisposable
    {
        /// <summary>
        /// The settings to use for this reader session
        /// </summary>
        public CSVSettings Settings { get; private set; }

        /// <summary>
        /// The stream from which this reader gets its data
        /// </summary>
        /// <value>The stream.</value>
        public StreamReader Stream { get; private set; }

        /// <summary>
        /// If the first row in the file is a header row, this will be populated
        /// </summary>
        private string[] _headers;

        #region Constructors
        /// <summary>
        /// Construct a new CSV reader off a streamed source
        /// </summary>
        /// <param name="source">The stream source</param>
        /// <param name="settings">The CSV settings to use for this reader (Default: CSV)</param>
        public CSVReader(StreamReader source, CSVSettings settings = null)
        {
            Stream = source;
            Settings = settings;
            if (Settings == null) Settings = CSVSettings.CSV;

        }
        #endregion

        #region Iterate through a CSV File
#if HAS_ASYNC
        /// <summary>
        /// Retrieve the next line from the file.
        /// </summary>
        /// <param name="line">The next available CSV line retrieved from the stream</param>
        /// <returns>One line from the file.</returns>
        public async Task<string[]> NextLine()
        {
            // If this is the firest line, retrieve headers before gathering data
            await Headers().ConfigureAwait(false);

            // Get the next line
            return await CSV.ParseMultiLine(Stream, Settings).ConfigureAwait(false);
        }


        /// <summary>
        /// Retrieve headers for this CSV file
        /// </summary>
        /// <returns>The for headers.</returns>
        public async Task<string[]> Headers()
        {
            if (_headers == null)
            {
                if (Settings.HeaderRowIncluded)
                {
                    _headers = await CSV.ParseMultiLine(Stream, Settings).ConfigureAwait(false); 
                }
                else 
                {
                    _headers = Settings.AssumedHeaders?.ToArray();
                }
            }
            return _headers;
        }
#else
        /// <summary>
        /// Retrieve the next line from the file.
        /// </summary>
        /// <param name="line">The next available CSV line retrieved from the stream</param>
        /// <returns>One line from the file.</returns>
        public string[] NextLine()
        {
            // If this is the firest line, retrieve headers before gathering data
            Headers();

            // Get the next line
            return CSV.ParseMultiLine(Stream, Settings);
        }


        /// <summary>
        /// Retrieve headers for this CSV file
        /// </summary>
        /// <returns>The for headers.</returns>
        public string[] Headers()
        {
            if (_headers == null)
            {
                if (Settings.HeaderRowIncluded)
                {
                    _headers = CSV.ParseMultiLine(Stream, Settings);
                }
                else
                {
                    _headers = Settings.AssumedHeaders?.ToArray();
                }
            }
            return _headers;
        }
#endif


        /// <summary>
        /// Deserialize the CSV reader into a generic list
        /// </summary>
#if HAS_ASYNC
        public async Task<List<T>> Deserialize<T>() where T : class, new()
#else
        public List<T> Deserialize<T>() where T : class, new()
#endif
        {
            List<T> result = new List<T>();
            Type return_type = typeof(T);

            // Read in the first line - we have to have headers!
#if HAS_ASYNC
            var h = await Headers().ConfigureAwait(false);
#else
            var h = Headers();
#endif
            if (h == null) throw new Exception("CSV must have headers to be deserialized");
            int num_columns = h.Length;

            // Determine how to handle each column in the file - check properties, fields, and methods
            Type[] column_types = new Type[num_columns];
            TypeConverter[] column_convert = new TypeConverter[num_columns];
            PropertyInfo[] prop_handlers = new PropertyInfo[num_columns];
            FieldInfo[] field_handlers = new FieldInfo[num_columns];
            MethodInfo[] method_handlers = new MethodInfo[num_columns];
            for (int i = 0; i < num_columns; i++)
            {
                prop_handlers[i] = return_type.GetProperty(h[i]);

                // If we failed to get a property handler, let's try a field handler
                if (prop_handlers[i] == null)
                {
                    field_handlers[i] = return_type.GetField(h[i]);

                    // If we failed to get a field handler, let's try a method
                    if (field_handlers[i] == null)
                    {

                        // Methods must be treated differently - we have to ensure that the method has a single parameter
                        MethodInfo mi = return_type.GetMethod(h[i]);
                        if (mi != null)
                        {
                            if (mi.GetParameters().Length == 1)
                            {
                                method_handlers[i] = mi;
                                column_types[i] = mi.GetParameters()[0].ParameterType;
                            }
                            else if (!Settings.IgnoreHeaderErrors)
                            {
                                throw new Exception($"The column header '{h[i]}' matched a method with more than one parameter.");
                            }

                            // Does the caller want us to throw an error on bad columns?
                        }
                        else if (!Settings.IgnoreHeaderErrors)
                        {
                            throw new Exception($"The column header '{h[i]}' was not found in the class '{return_type.FullName}'.");
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
                    if ((column_convert[i] == null) && !Settings.IgnoreHeaderErrors)
                    {
                        throw new Exception($"The column {h[i]} (type {column_types[i]}) does not have a type converter.");
                    }
                }
            }

            // Alright, let's retrieve CSV lines and parse each one!
            int row_num = 1;
            while (true)
            {
#if HAS_ASYNC
                var line = await NextLine().ConfigureAwait(false);
#else
                var line = NextLine();
#endif
                if (line == null) break;

                // Does this line match the length of the first line?  Does the caller want us to complain?
                if ((line.Length != num_columns) && !Settings.IgnoreHeaderErrors) {
                    throw new Exception($"Line #{row_num} contains {line.Length} columns; expected {num_columns}");
                }

                // Construct a new object and execute each column on it
                T obj = new T();
                for (int i = 0; i < Math.Min(line.Length, num_columns); i++)
                {

                    // Attempt to convert this to the specified type
                    object value = null;
                    if (Settings.AllowNull && (line[i] == null))
                    {
                        value = null;
                    } 
                    else if (column_convert[i] != null && column_convert[i].IsValid(line[i]))
                    {
                        value = column_convert[i].ConvertFromString(line[i]);
                    }
                    else if (!Settings.IgnoreHeaderErrors)
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
            Stream.Dispose();
        }
#endregion
    }
}

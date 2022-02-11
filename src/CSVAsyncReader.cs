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
using System.Threading.Tasks;
using System.Threading;

namespace CSVFile
{
    public class CSVAsyncReader : IAsyncEnumerable<string[]>, IDisposable
    {
        private readonly CSVSettings _settings;
        private IAsyncEnumerable<string[]> _enumerable;
        private readonly StreamReader _stream;

        /// <summary>
        /// If the first row in the file is a header row, this will be populated
        /// </summary>
        public string[] Headers;

        /// <summary>
        /// Construct a new CSV reader off a streamed source
        /// </summary>
        /// <param name="source">The stream source</param>
        /// <param name="settings">The CSV settings to use for this reader (Default: CSV)</param>
        private CSVAsyncReader(StreamReader source, CSVSettings settings = null)
        {
            _stream = source;
            _settings = settings ?? CSVSettings.CSV;
            _enumerable = null;
        }

        public static async Task<CSVAsyncReader> From(StreamReader source, CSVSettings settings = null)
        {
            var reader = new CSVAsyncReader(source, settings);
            reader._enumerable = CSV.ParseStreamAsync(reader._stream, reader._settings);

            // Do we need to parse the first line of the file as headers?
            if (reader._settings.HeaderRowIncluded)
            {
                await using (var e = reader._enumerable.GetAsyncEnumerator())
                {
                    await e.MoveNextAsync();
                    reader.Headers = e.Current;
                }
            }
            else
            {
                reader.Headers = reader._settings.AssumedHeaders;
            }
            return reader;
        }

        /// <summary>
        /// Read all lines from the file in async mode
        /// </summary>
        /// <returns></returns>
        public IAsyncEnumerable<string[]> Lines()
        {
            return _enumerable;
        }

        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }

        public IAsyncEnumerator<string[]> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return _enumerable.GetAsyncEnumerator(cancellationToken);
        }


        /// <summary>
        /// Deserialize the CSV reader into a generic list
        /// </summary>
        public async Task<List<T>> Deserialize<T>() where T : class, new()
        {
            var result = new List<T>();
            var return_type = typeof(T);

            // Read in the first line - we have to have headers!
            if (Headers == null) throw new Exception("CSV must have headers to be deserialized");
            var num_columns = Headers.Length;

            // Determine how to handle each column in the file - check properties, fields, and methods
            var column_types = new Type[num_columns];
            var column_convert = new TypeConverter[num_columns];
            var prop_handlers = new PropertyInfo[num_columns];
            var field_handlers = new FieldInfo[num_columns];
            var method_handlers = new MethodInfo[num_columns];
            for (var i = 0; i < num_columns; i++)
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
                        var mi = return_type.GetMethod(Headers[i]);
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
            var row_num = 1;
            await foreach (var line in this)
            {

                // Does this line match the length of the first line?  Does the caller want us to complain?
                if ((line.Length != num_columns) && !_settings.IgnoreHeaderErrors)
                {
                    throw new Exception($"Line #{row_num} contains {line.Length} columns; expected {num_columns}");
                }

                // Construct a new object and execute each column on it
                var obj = new T();
                for (var i = 0; i < Math.Min(line.Length, num_columns); i++)
                {

                    // Attempt to convert this to the specified type
                    object value = null;
                    if (column_convert[i] != null && column_convert[i].IsValid(line[i]))
                    {
                        value = column_convert[i].ConvertFromString(line[i]);
                    }
                    else if (!_settings.IgnoreHeaderErrors)
                    {
                        throw new Exception($"The value '{line[i]}' cannot be converted to the type {column_types[i]}.");
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
                        method_handlers[i].Invoke(obj, new[] { value });
                    }
                }

                // Keep track of where we are in the file
                result.Add(obj);
                row_num++;
            }

            // Here's your array!
            return result;
        }
    }
}

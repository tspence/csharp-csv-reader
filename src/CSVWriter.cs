/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Reflection;
#if HAS_ASYNC
using System.Threading.Tasks;
#endif

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ConvertIfStatementToNullCoalescingExpression

namespace CSVFile
{
    /// <summary>
    /// Helper object that implements serialization separately from the string or stream I/O
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SerializationHelper<T> where T : class, new()
    {
        private readonly CSVSettings _settings;
        private readonly PropertyInfo[] _properties;
        private readonly FieldInfo[] _fields;
        private readonly char[] _riskyChars;
        private readonly Dictionary<Type, int> _forceQualifierTypes;

        /// <summary>
        /// Constructs a serialization helper object separate from I/O
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="riskyChars"></param>
        /// <param name="forceQualifierTypes"></param>
        public SerializationHelper(CSVSettings settings, char[] riskyChars, Dictionary<Type, int> forceQualifierTypes)
        {
            _settings = settings;
            if (_settings == null)
            {
                _settings = CSVSettings.CSV;
            }

            // Extract properties and fields that are not excluded
            var excluded = new ExcludedColumnHelper(_settings);
            var props = new List<PropertyInfo>();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (!excluded.IsExcluded(prop.Name))
                {
                    props.Add(prop);
                }
            }

            var fields = new List<FieldInfo>();
            foreach (var field in typeof(T).GetFields())
            {
                if (!excluded.IsExcluded(field.Name))
                {
                    fields.Add(field);
                }
            }

            _properties = props.ToArray();
            _fields = fields.ToArray();
            _riskyChars = riskyChars;
            _forceQualifierTypes = forceQualifierTypes;
        }

        /// <summary>
        /// Serialize the header for the CSV file
        /// </summary>
        /// <returns></returns>
        public string SerializeHeader()
        {
            var headers = new List<object>();
            foreach (var field in _fields)
            {
                headers.Add(field.Name);
            }
            foreach (var prop in _properties)
            {
                headers.Add(prop.Name);
            }

            return CSV.ItemsToCsv(headers, _settings, _riskyChars, _forceQualifierTypes);
        }

        /// <summary>
        /// Serialize a single row for the CSV file
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Serialize(T obj)
        {
            var items = new List<object>();
            foreach (var field in _fields)
            {
                items.Add(field.GetValue(obj));
            }
            foreach (var prop in _properties)
            {
                items.Add(prop.GetValue(obj, null));
            }
            return CSV.ItemsToCsv(items, _settings, _riskyChars, _forceQualifierTypes);
        }
    }

    /// <summary>
    /// Writes CSV objects to a stream
    /// </summary>
    public class CSVWriter : IDisposable
    {
        private readonly CSVSettings _settings;
        private readonly StreamWriter _writer;
        private readonly char[] _riskyChars;
        private readonly Dictionary<Type, int> _forceQualifierTypes;

        /// <summary>
        /// Construct a new CSV writer to produce output on the enclosed StreamWriter
        /// </summary>
        /// <param name="dest">The stream where this CSV will be outputted</param>
        /// <param name="settings">The CSV settings to use when writing to the stream (Default: CSV)</param>
        public CSVWriter(StreamWriter dest, CSVSettings settings = null)
        {
            _writer = dest;
            _settings = settings;
            if (_settings == null)
            {
                _settings = CSVSettings.CSV;
            }
            _riskyChars = _settings.GetRiskyChars();
            _forceQualifierTypes = _settings.GetForceQualifierTypes();
        }

        /// <summary>
        /// Construct a new CSV writer to produce output on the enclosed stream
        /// </summary>
        /// <param name="dest">The stream where this CSV will be outputted</param>
        /// <param name="settings">The CSV settings to use when writing to the stream (Default: CSV)</param>
        public CSVWriter(Stream dest, CSVSettings settings = null)
        {
            _settings = settings;
            if (_settings == null)
            {
                _settings = CSVSettings.CSV;
            }
            _writer = new StreamWriter(dest, _settings.Encoding);
            _riskyChars = _settings.GetRiskyChars();
            _forceQualifierTypes = _settings.GetForceQualifierTypes();
        }

        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        public void Write(DataTable dt)
        {
            if (_settings.HeaderRowIncluded)
            {
                var headers = new List<object>();
                foreach (DataColumn col in dt.Columns)
                {
                    headers.Add(col.ColumnName);
                }
                _writer.Write(CSV.ItemsToCsv(headers, _settings, _riskyChars, _forceQualifierTypes));
                _writer.Write(_settings.LineSeparator);
            }

            foreach (DataRow dr in dt.Rows)
            {
                _writer.Write(CSV.ItemsToCsv(dr.ItemArray, _settings, _riskyChars, _forceQualifierTypes));
                _writer.Write(_settings.LineSeparator);
            }
        }

        /// <summary>
        /// Write a single line to this CSV
        /// </summary>
        /// <param name="items"></param>
        public void WriteLine(IEnumerable<object> items)
        {
            _writer.Write(CSV.ItemsToCsv(items, _settings, _riskyChars, _forceQualifierTypes));
            _writer.Write(_settings.LineSeparator);
        }

#if HAS_ASYNC
        /// <summary>
        /// Write a single line to this CSV
        /// </summary>
        /// <param name="items"></param>
        public async Task WriteLineAsync(IEnumerable<object> items)
        {
            await _writer.WriteAsync(CSV.ItemsToCsv(items, _settings, _riskyChars, _forceQualifierTypes));
            await _writer.WriteAsync(_settings.LineSeparator);
        }

        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        public async Task WriteAsync(DataTable dt)
        {
            if (_settings.HeaderRowIncluded)
            {
                var headers = new List<object>();
                foreach (DataColumn col in dt.Columns)
                {
                    headers.Add(col.ColumnName);
                }
                await _writer.WriteAsync(CSV.ItemsToCsv(headers, _settings, _riskyChars, _forceQualifierTypes));
                await _writer.WriteAsync(_settings.LineSeparator);
            }

            foreach (DataRow dr in dt.Rows)
            {
                await _writer.WriteAsync(CSV.ItemsToCsv(dr.ItemArray, _settings, _riskyChars, _forceQualifierTypes));
                await _writer.WriteAsync(_settings.LineSeparator);
            }
        }
#endif

        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <param name="list">An IEnumerable that produces the list of objects to serialize.</param>
        public void Serialize<T>(IEnumerable<T> list) where T : class, new()
        {
            var serializer = new SerializationHelper<T>(_settings, _riskyChars, _forceQualifierTypes);
            if (_settings.HeaderRowIncluded)
            {
                _writer.Write(serializer.SerializeHeader());
                _writer.Write(_settings.LineSeparator);
            }

            foreach (var row in list)
            {
                _writer.Write(serializer.Serialize(row));
                _writer.Write(_settings.LineSeparator);
            }
        }

#if HAS_ASYNC
        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <param name="list">An IEnumerable that produces the list of objects to serialize.</param>
        public async Task SerializeAsync<T>(IEnumerable<T> list) where T : class, new()
        {
            var serializer = new SerializationHelper<T>(_settings, _riskyChars, _forceQualifierTypes);
            if (_settings.HeaderRowIncluded)
            {
                await _writer.WriteAsync(serializer.SerializeHeader());
                await _writer.WriteAsync(_settings.LineSeparator);
            }

            foreach (var row in list)
            {
                await _writer.WriteAsync(serializer.Serialize(row));
                await _writer.WriteAsync(_settings.LineSeparator);
            }
        }
#endif

#if HAS_ASYNC_IENUM
        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <param name="list">An IEnumerable that produces the list of objects to serialize.</param>
        public async Task SerializeAsync<T>(IAsyncEnumerable<T> list) where T : class, new()
        {
            var serializer = new SerializationHelper<T>(_settings, _riskyChars, _forceQualifierTypes);
            if (_settings.HeaderRowIncluded)
            {
                await _writer.WriteAsync(serializer.SerializeHeader());
                await _writer.WriteAsync(_settings.LineSeparator);
            }

            await foreach (var row in list)
            {
                await _writer.WriteAsync(serializer.Serialize(row));
                await _writer.WriteAsync(_settings.LineSeparator);
            }
        }
#endif

        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _writer.Close();
            _writer.Dispose();
        }
    }
}

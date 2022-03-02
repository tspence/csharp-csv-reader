/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSVFile
{
    /// <summary>
    /// Constructs a CSV stream that can read or write from a stream
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CSVStream<T> where T: class, new()
    {
        private readonly Stream _stream;
        private readonly CSVSettings _settings;
        private readonly List<string> _headers;
        private readonly FieldInfo[] _fields;
        private readonly PropertyInfo[] _properties;

        /// <summary>
        /// Construct a streaming interface that can read or write typed objects as CSV
        /// </summary>
        /// <param name="stream">The stream to read or write</param>
        /// <param name="settings">The settings to use</param>
        public CSVStream(Stream stream, CSVSettings settings = null)
        {
            _stream = stream;
            _settings = settings ?? CSVSettings.CSV;
            var type = typeof(T);
            _fields = type.GetFields();
            _properties = type.GetProperties();
            var headers = _fields.Select(fi => fi.Name).ToList();
            headers.AddRange(_properties.Select(pi => pi.Name));
            _headers = headers;
        }
        
        /// <summary>
        /// Serialize an object array to a stream in CSV format
        /// </summary>
        /// <param name="list">The object array to write</param>
        public void Write(IEnumerable<T> list)
        {
        }

        /// <summary>
        /// Read in a single CSV file as an array of objects
        /// </summary>
        /// <returns>An array of objects that were retrieved from the CSV stream.</returns>
        public IEnumerable<T> Read()
        {
            using (var sr = new StreamReader(_stream))
            {
                foreach (var line in CSV.ParseStream(sr, _settings))
                {
                }
                return new List<T>();
            }
        }
    }
}

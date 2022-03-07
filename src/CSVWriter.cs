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

namespace CSVFile
{
    /// <summary>
    /// Writes CSV objects to a stream
    /// </summary>
    public class CSVWriter : IDisposable
    {
        private readonly CSVSettings _settings;
        private readonly StreamWriter _stream;

        /// <summary>
        /// Construct a new CSV writer to produce output on the enclosed StreamWriter
        /// </summary>
        /// <param name="dest">The stream where this CSV will be outputted</param>
        /// <param name="settings">The CSV settings to use when writing to the stream (Default: CSV)</param>
        public CSVWriter(StreamWriter dest, CSVSettings settings = null)
        {
            _stream = dest;
            _settings = settings;
            if (_settings == null)
            {
                _settings = CSVSettings.CSV;
            }
        }

        /// <summary>
        /// Write one line to the file
        /// </summary>
        /// <param name="line">The array of values for this line</param>
        public void WriteLine(IEnumerable<object> line)
        {
            _stream.WriteLine(CSV.ToCSVString(line, _settings));
        }

#if HAS_DATATABLE
        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        public void Write(DataTable dt)
        {
            // Write headers, if the caller requested we do so
            if (_settings.HeaderRowIncluded) {
                var headers = new List<object>();
                foreach (DataColumn col in dt.Columns) {
                    headers.Add(col.ColumnName);
                }
                WriteLine(headers);
            }

            // Now produce the rows
            foreach (DataRow dr in dt.Rows) {
                WriteLine(dr.ItemArray);
            }

            // Flush the stream
            _stream.Flush();
        }
#endif

        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <param name="list">An IEnumerable that produces the list of objects to serialize.</param>
        public void WriteArray<T>(IEnumerable<T> list) where T: class, new()
        {
            _stream.Write(CSV.Serialize<T>(list, _settings));

            // Flush the stream
            _stream.Flush();
        }

        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _stream.Flush();
            _stream.Close();
            _stream.Dispose();
        }
    }
}

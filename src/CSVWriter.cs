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
    public class CSVWriter : IDisposable
    {
        protected CSVSettings _settings;

        protected StreamWriter _outstream;

#region Constructors
        /// <summary>
        /// Construct a new CSV writer to produce output on the enclosed StreamWriter
        /// </summary>
        /// <param name="dest">The stream where this CSV will be outputted</param>
        /// <param name="settings">The CSV settings to use when writing to the stream (Default: CSV)</param>
        public CSVWriter(StreamWriter dest, CSVSettings settings = null)
        {
            _outstream = dest;
            _settings = settings;
            if (_settings == null)
            {
                _settings = CSVSettings.CSV;
            }
        }
#endregion

#region Writing values
        /// <summary>
        /// Write one line to the file
        /// </summary>
        /// <param name="line">The array of values for this line</param>
        public void WriteLine(IEnumerable<object> line)
        {
            _outstream.WriteLine(line.ToCSVString(_settings));
        }
#endregion

#region Data Table Functions (not available in dot-net-portable mode)
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
            _outstream.Flush();
        }
#endif
#endregion

#region Serialization
        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <typeparam name="IEnumerable">An IEnumerable that produces the list of objects to serialize.</typeparam>
        public void WriteArray<T>(IEnumerable<T> list) where T: class, new()
        {
            _outstream.Write(CSV.Serialize<T>(list, _settings));

            // Flush the stream
            _outstream.Flush();
        }
#endregion

#region Disposables
        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _outstream.Flush();
            _outstream.Close();
            _outstream.Dispose();
        }
#endregion
    }
}

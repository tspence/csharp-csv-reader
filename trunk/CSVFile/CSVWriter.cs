using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;

namespace CSVFile
{
    public class CSVWriter : IDisposable
    {
        protected char _delimiter, _text_qualifier;

        protected StreamWriter _outstream;

        #region Constructors
        /// <summary>
        /// Construct a new CSV writer to produce output on the enclosed StreamWriter
        /// </summary>
        public CSVWriter(StreamWriter source, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            _outstream = source;
            _delimiter = delim;
            _text_qualifier = qual;
        }

        /// <summary>
        /// Construct a new CSV reader to produce output on the specified stream
        /// </summary>
        public CSVWriter(Stream source, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            _outstream = new StreamWriter(source);
            _delimiter = delim;
            _text_qualifier = qual;
        }

        /// <summary>
        /// Initialize a new CSV file structure to write data to disk
        /// </summary>
        public CSVWriter(string filename, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            _outstream = new StreamWriter(filename);
            _delimiter = delim;
            _text_qualifier = qual;
        }
        #endregion

        #region Writing values
        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="save_column_names">True if you wish the first line of the file to have column names</param>
        /// <param name="delim">The delimiter (comma, tab, pipe, etc) to separate fields</param>
        /// <param name="qual">The text qualifier (double-quote) that encapsulates fields that include delimiters</param>
        public void Write(DataTable dt, bool save_column_names)
        {
            // Write headers, if the caller requested we do so
            if (save_column_names) {
                List<string> headers = new List<string>();
                foreach (DataColumn col in dt.Columns) {
                    headers.Add(col.ColumnName);
                }
                _outstream.WriteLine(CSV.Output(headers, _delimiter, _text_qualifier));
            }

            // Now produce the rows
            foreach (DataRow dr in dt.Rows) {
                _outstream.WriteLine(CSV.Output(dr.ItemArray, _delimiter, _text_qualifier));
            }
        }
        #endregion

        #region Disposables
        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            _outstream.Close();
            _outstream.Dispose();
        }
        #endregion
    }
}

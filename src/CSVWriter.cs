/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace CSVFile
{
    /// <summary>
    /// Writes arrays of objects or data tables in CSV format out to a stream
    /// </summary>
    public class CSVWriter : IDisposable
    {
        /// <summary>
        /// CSV settings for this writer
        /// </summary>
        public CSVSettings Settings { get; private set; }

        /// <summary>
        /// Stream to which this writer is attached
        /// </summary>
        public StreamWriter Stream { get; private set; }

#region Constructors
        /// <summary>
        /// Construct a new CSV writer to produce output on the enclosed StreamWriter
        /// </summary>
        /// <param name="dest">The stream where this CSV will be outputted</param>
        /// <param name="settings">The CSV settings to use when writing to the stream (Default: CSV)</param>
        public CSVWriter(StreamWriter dest, CSVSettings settings = null)
        {
            Stream = dest;
            Settings = settings;
            if (Settings == null)
            {
                Settings = CSVSettings.CSV;
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
            Stream.WriteLine(line.ToCSVString(Settings));
        }
#endregion

#region Serialization
        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <typeparam name="IEnumerable">An IEnumerable that produces the list of objects to serialize.</typeparam>
        public void WriteArray<T>(IEnumerable<T> list) where T: class, new()
        {
            Stream.Write(CSV.Serialize<T>(list, Settings));

            // Flush the stream
            Stream.Flush();
        }
#endregion

#region Disposables
        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            Stream.Flush();
            Stream.Close();
            Stream.Dispose();
        }
#endregion
    }
}

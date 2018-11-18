/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if HAS_ASYNC
using System.Threading.Tasks;
#endif

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

#if HAS_ASYNC
        /// <summary>
        /// Write one line to the file
        /// </summary>
        /// <param name="line">The array of values for this line</param>
        public async Task WriteLine(IEnumerable<object> line)
        {
            await Stream.WriteLineAsync(line.ToCSVString(Settings)).ConfigureAwait(false);
        }

        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <typeparam name="IEnumerable">An IEnumerable that produces the list of objects to serialize.</typeparam>
        public async Task WriteArray<T>(IEnumerable<T> list) where T: class, new()
        {
            await Stream.WriteAsync(CSV.Serialize<T>(list, Settings)).ConfigureAwait(false);

            // Since many people use this shortcut function and expect to see data right away, we flush here
            await Stream.FlushAsync().ConfigureAwait(false);
        }
#else
        /// <summary>
        /// Write one line to the file
        /// </summary>
        /// <param name="line">The array of values for this line</param>
        public void WriteLine(IEnumerable<object> line)
        {
            Stream.WriteLine(line.ToCSVString(Settings));
        }

        /// <summary>
        /// Serialize a list of objects to CSV using this writer
        /// </summary>
        /// <typeparam name="IEnumerable">An IEnumerable that produces the list of objects to serialize.</typeparam>
        public void WriteArray<T>(IEnumerable<T> list) where T : class, new()
        {
            // Did the caller want the header row?
            if (Settings.HeaderRowIncluded)
            {
                Stream.Write(CSV.GetHeader(typeof(T), Settings));
                Stream.Write(Settings.LineSeparator);
            }

            // Let's go through the array of objects
            // Iterate through all the objects
            var values = new List<object>();
            var sb = new StringBuilder();
            foreach (T obj in list)
            {
                sb.Length = 0;
                sb.AppendAsCSV<T>(obj, Settings);
                sb.Append(Settings.LineSeparator);
                Stream.Write(sb.ToString());
            }

            // Since many people use this shortcut function and expect to see data right away, we flush here
            Stream.Flush();
        }
#endif

        /// <summary>
        /// Close our resources - specifically, the stream reader
        /// </summary>
        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}

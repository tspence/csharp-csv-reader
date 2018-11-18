/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CSVFile
{
    /// <summary>
    /// Extension methods for simplifying streams
    /// </summary>
    public static class CSVStream
    {
        #region FileStream related functions
        /// <summary>
        /// Serialize an object array to a stream in CSV format
        /// </summary>
        /// <param name="list">The object array to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="settings">The CSV settings to use (Default: CSV)</param>
        public static async Task WriteToStream<T>(this IEnumerable<T> list, StreamWriter sw, CSVSettings settings = null) where T: class, new()
        {
            using (CSVWriter cw = new CSVWriter(sw, settings))
            {
                await cw.WriteArray<T>(list).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Serialize an object array to a stream in CSV format
        /// </summary>
        /// <param name="list">The object array to write</param>
        /// <param name="filename">The stream where the CSV text will be written</param>
        /// <param name="settings">The CSV settings to use when writing the output (Default: CSV)</param>
        public static async Task WriteToStream<T>(this IEnumerable<T> list, string filename, CSVSettings settings = null) where T: class, new()
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                await WriteToStream<T>(list, sw, settings).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Read in a single CSV file as an array of objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize from this CSV.</typeparam>
        /// <param name="filename">The path of the file to read.</param>
        /// <param name="settings">The CSV settings to use when loading this array (Default: CSV)</param>
        /// <returns>An array of objects that were retrieved from the CSV file.</returns>
        public static async Task<List<T>> LoadArray<T>(string filename, CSVSettings settings = null) where T : class, new()
        {
            return await LoadArray<T>(new StreamReader(filename), settings).ConfigureAwait(false);
        }

        /// <summary>
        /// Read in a single CSV file as an array of objects
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize from this CSV.</typeparam>
        /// <param name="stream">The stream to read.</param>
        /// <param name="settings">The CSV settings to use when loading this array (Default: CSV)</param>
        /// <returns>An array of objects that were retrieved from the CSV file.</returns>
        public static async Task<List<T>> LoadArray<T>(StreamReader stream, CSVSettings settings) where T : class, new()
        {
            using (CSVReader cr = new CSVReader(stream, settings))
            {
                return await cr.Deserialize<T>().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Convert a CSV file (in string form) into a list of string arrays 
        /// </summary>
        /// <param name="source_string"></param>
        /// <param name="settings">The CSV settings to use when loading this array (Default: CSV)</param>
        /// <returns></returns>
        public static async Task<List<string[]>> LoadString(string source_string, CSVSettings settings = null)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(source_string);
            var results = new List<string[]>();
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                using (CSVReader cr = new CSVReader(new StreamReader(stream)))
                {
                    while (true)
                    {
                        var line = await cr.NextLine().ConfigureAwait(false);
                        if (line == null) break;
                        results.Add(line);
                    }
                }
            }
            return results;
        }
        #endregion
    }
}

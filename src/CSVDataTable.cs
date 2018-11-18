/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
#if HAS_DATATABLE
using System.Data;
#endif
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CSVFile
{
#if HAS_DATATABLE
    /// <summary>
    /// Extension class for simplifying data table operations
    /// </summary>
    public static class CSVDataTable
    {
#region Reading CSV into a DataTable
        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static async Task<DataTable> FromFile(string filename, CSVSettings settings = null)
        {
            using (var sr = new StreamReader(filename))
            {
                return await FromStream(sr, settings).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="stream">The stream source from which to load the datatable.</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static async Task<DataTable> FromStream(StreamReader stream, CSVSettings settings = null)
        {
            using (CSVReader cr = new CSVReader(stream, settings))
            {
                return await cr.ReadAsDataTable().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Convert a CSV file (in string form) into a data table
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns></returns>
        public static async Task<DataTable> FromString(string source, CSVSettings settings = null)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(source);
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                using (CSVReader cr = new CSVReader(new StreamReader(stream), settings))
                {
                    return await cr.ReadAsDataTable().ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Read the entire stream into a data table in memory
        /// </summary>
        public static async Task<DataTable> ReadAsDataTable(this CSVReader reader)
        {
            DataTable dt = new DataTable();
            string[] firstLine = null;

            // File contains column names - so name each column properly
            var h = await reader.Headers().ConfigureAwait(false);
            if (h == null)
            {
                firstLine = await reader.NextLine().ConfigureAwait(false);
                var list = new List<string>();
                for (int i = 0; i < firstLine.Length; i++)
                {
                    list.Add($"Column{i}");
                }
            }

            // Add headers
            int numColumns = h.Length;
            for (int i = 0; i < h.Length; i++)
            {
                dt.Columns.Add(new DataColumn(h[i], typeof(string)));
            }

            // If we had to read the first line to get dimensions, add it
            int row_num = 1;
            if (firstLine != null)
            {
                dt.Rows.Add(firstLine);
                row_num++;
            }

            // Start reading through the file
            while (true)
            {
                var line = await reader.NextLine().ConfigureAwait(false);
                if (line == null) break;

                // Does this line match the length of the first line?
                if (line.Length != numColumns)
                {
                    if (!reader.Settings.IgnoreDimensionErrors)
                    {
                        throw new Exception($"Line #{row_num} contains {line.Length} columns; expected {numColumns}");
                    }
                    else
                    {

                        // Add as best we can - construct a new line and make it fit
                        List<string> list = new List<string>();
                        list.AddRange(line);
                        while (list.Count < numColumns)
                        {
                            list.Add("");
                        }
                        dt.Rows.Add(list.GetRange(0, numColumns).ToArray());
                    }
                }
                else
                {
                    dt.Rows.Add(line);
                }

                // Keep track of where we are in the file
                row_num++;
            }

            // Here's your data table
            return dt;
        }
        #endregion

#if HAS_SMTPCLIENT
        #region CSV Attachment Email Shortcut
        /// <summary>
        /// Quick shortcut to send a datatable as an attachment via SMTP
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="from_address"></param>
        /// <param name="to_address"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public static void SendCsvAttachment(this DataTable dt, string from_address, string to_address, string subject, string body, string smtp_host, string attachment_filename)
        {
            // Save this CSV to a string
            string csv = WriteToString(dt);

            // Prepare the email message and attachment
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
            message.To.Add(to_address);
            message.Subject = subject;
            message.From = new System.Net.Mail.MailAddress(from_address);
            message.Body = body;
            System.Net.Mail.Attachment a = System.Net.Mail.Attachment.CreateAttachmentFromString(csv, "text/csv");
            a.Name = attachment_filename;
            message.Attachments.Add(a);

            // Send the email
#if NET20
            var smtp = new System.Net.Mail.SmtpClient(smtp_host);
            smtp.Send(message);
#else
            using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(smtp_host))
            {
                smtp.Send(message);
            }
#endif
        }
        #endregion
#endif

        #region Writing a DataTable to CSV
        /// <summary>
        /// Write a data table to disk at the designated file name in CSV format
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filename"></param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
        public static async Task WriteToFile(this DataTable dt, string filename, CSVSettings settings = null)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                await WriteToStream(dt, sw, settings).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
        public static async Task WriteToStream(this DataTable dt, StreamWriter sw, CSVSettings settings = null)
        {
            using (CSVWriter cw = new CSVWriter(sw, settings))
            {
                await cw.Write(dt).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Write a DataTable to a string in CSV format
        /// </summary>
        /// <param name="dt">The datatable to write</param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
        /// <returns>The CSV string representing the object array.</returns>
        public static async Task<string> WriteToString(this DataTable dt, CSVSettings settings = null)
        {
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);
                var cw = new CSVWriter(sw, settings);
                await cw.Write(dt).ConfigureAwait(false);
                sw.Flush();
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        public static async Task Write(this CSVWriter writer, DataTable dt)
        {
            // Write headers, if the caller requested we do so
            if (writer.Settings.HeaderRowIncluded)
            {
                var headers = new List<object>();
                foreach (DataColumn col in dt.Columns)
                {
                    headers.Add(col.ColumnName);
                }
                await writer.WriteLine(headers).ConfigureAwait(false);
            }

            // Now produce the rows
            foreach (DataRow dr in dt.Rows)
            {
                await writer.WriteLine(dr.ItemArray).ConfigureAwait(false);
            }

            // Flush the stream
            await writer.Stream.FlushAsync().ConfigureAwait(false);
        }
        #endregion
    }
#endif
}

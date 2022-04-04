/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System.Data;
using System.IO;
using System.Text;

namespace CSVFile
{
    /// <summary>
    /// Code to assist in working with DataTable objects and CSV sources
    /// </summary>
    public static class CSVDataTable
    {
        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable FromFile(string filename, CSVSettings settings = null)
        {
            using (var sr = new StreamReader(filename))
            {
                return FromStream(sr, settings);
            }
        }

        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="stream">The stream source from which to load the datatable.</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable FromStream(StreamReader stream, CSVSettings settings = null)
        {
            using (var cr = new CSVReader(stream, settings))
            {
                return cr.ReadAsDataTable();
            }
        }

        /// <summary>
        /// Convert a CSV file (in string form) into a data table
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns></returns>
        public static DataTable FromString(string source, CSVSettings settings = null)
        {
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }
            var byteArray = settings.Encoding.GetBytes(source);
            using (var stream = new MemoryStream(byteArray))
            {
                using (var cr = new CSVReader(stream, settings))
                {
                    return cr.ReadAsDataTable();
                }
            }
        }

        /// <summary>
        /// Quick shortcut to send a datatable as an attachment via SMTP
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="from_address"></param>
        /// <param name="to_address"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="smtp_host"></param>
        /// <param name="attachment_filename"></param>
#if NET2_0
        public static void SendCsvAttachment(DataTable dt, string from_address, string to_address, string subject, string body, string smtp_host, string attachment_filename)
#else
        public static void SendCsvAttachment(this DataTable dt, string from_address, string to_address, string subject, string body, string smtp_host, string attachment_filename)
#endif
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

#if NET2_0
            // In DotNet 2.0, SmtpClient isn't disposable
            var smtp = new System.Net.Mail.SmtpClient(smtp_host);
            smtp.Send(message);
#else
            using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(smtp_host))
            {
                smtp.Send(message);
            }
#endif
        }

        /// <summary>
        /// Write a data table to disk at the designated file name in CSV format
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filename"></param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
#if NET2_0
        public static void WriteToFile(DataTable dt, string filename, CSVSettings settings = null)
#else
        public static void WriteToFile(this DataTable dt, string filename, CSVSettings settings = null)
#endif
        {
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }

            using (var fs = new FileStream(filename, FileMode.CreateNew))
            {
                using (var sw = new StreamWriter(fs, settings.Encoding))
                {
                    WriteToStream(dt, sw, settings);
                }
            }
        }

        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
#if NET2_0
        public static void WriteToStream(DataTable dt, StreamWriter sw, CSVSettings settings = null)
#else
        public static void WriteToStream(this DataTable dt, StreamWriter sw, CSVSettings settings = null)
#endif
        {
            using (var cw = new CSVWriter(sw, settings))
            {
                cw.Write(dt);
            }
        }

        /// <summary>
        /// Write a DataTable to a string in CSV format
        /// </summary>
        /// <param name="dt">The datatable to write</param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
        /// <returns>The CSV string representing the object array.</returns>
#if NET2_0
        public static string WriteToString(DataTable dt, CSVSettings settings = null)
#else
        public static string WriteToString(this DataTable dt, CSVSettings settings = null)
#endif
        {
            if (settings == null)
            {
                settings = CSVSettings.CSV;
            }
            using (var ms = new MemoryStream())
            {
                var cw = new CSVWriter(ms, settings);
                cw.Write(dt);
                var rawString = settings.Encoding.GetString(ms.ToArray());
                return CSV.RemoveByteOrderMarker(rawString);
            }
        }
    }
}

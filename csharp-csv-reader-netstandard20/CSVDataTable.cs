using System;
using System.Data;
using System.IO;
using System.Text;

namespace CSVFile
{
    public static class CSVDataTable
    {
        #region Reading CSV into a DataTable
        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable LoadDataTable(string filename, CSVSettings settings = null)
        {
            return LoadDataTable(new StreamReader(filename), settings);
        }

        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="stream">The stream source from which to load the datatable.</param>
        /// <param name="settings">The CSV settings to use when exporting this array (Default: CSV)</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable LoadDataTable(StreamReader stream, CSVSettings settings = null)
        {
            using (CSVReader cr = new CSVReader(stream, settings))
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
        public static DataTable LoadDataTableFromString(string source, CSVSettings settings = null)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(source);
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                using (CSVReader cr = new CSVReader(new StreamReader(stream), settings))
                {
                    return cr.ReadAsDataTable();
                }
            }
        }
        #endregion

        #region CSV Attachment Email Shortcut
        /// <summary>
        /// Quick shortcut to send a datatable as an attachment via SMTP
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="from_address"></param>
        /// <param name="to_address"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
#if DOTNET20
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

            // Send the email
#if (DOTNET20)
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

        #region Writing a DataTable to CSV
        /// <summary>
        /// Write a data table to disk at the designated file name in CSV format
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filename"></param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
#if DOTNET20
        public static void SaveAsCSV(DataTable dt, string filename, CSVSettings settings = null)
#else
        public static void SaveAsCSV(this DataTable dt, string filename, CSVSettings settings = null)
#endif
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                WriteToStream(dt, sw, settings);
            }
        }

        /// <summary>
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="settings">The CSV settings to use when exporting this DataTable (Default: CSV)</param>
#if DOTNET20
        public static void WriteToStream(DataTable dt, StreamWriter sw, CSVSettings settings = null)
#else
        public static void WriteToStream(this DataTable dt, StreamWriter sw, CSVSettings settings = null)
#endif
        {
            using (CSVWriter cw = new CSVWriter(sw, settings))
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
#if DOTNET20
        public static string WriteToString(DataTable dt, CSVSettings settings = null)
#else
        public static string WriteToString(this DataTable dt, CSVSettings settings = null)
#endif
        {
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);
                var cw = new CSVWriter(sw, settings);
                cw.Write(dt);
                sw.Flush();
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        #endregion
    }
}

using System;
using System.Data;
using System.IO;
using System.Text;

namespace CSVFile
{
    public static class CSVDataTable
    {
        #region DataTable related functions (not available on dot-net-portable)
        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delim">The CSV field delimiter character.</param>
        /// <param name="qual">The CSV text qualifier character.</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable LoadDataTable(string filename, bool first_row_are_headers = true, bool ignore_dimension_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            return LoadDataTable(new StreamReader(filename), first_row_are_headers, ignore_dimension_errors, delim, qual);
        }

        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="stream">The stream source from which to load the datatable.</param>
        /// <param name="delim">The CSV field delimiter character.</param>
        /// <param name="qual">The CSV text qualifier character.</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable LoadDataTable(StreamReader stream, bool first_row_are_headers = true, bool ignore_dimension_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            using (CSVReader cr = new CSVReader(stream, delim, qual))
            {
                return cr.ReadAsDataTable(first_row_are_headers, ignore_dimension_errors, null);
            }
        }

        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delim">The CSV field delimiter character.</param>
        /// <param name="qual">The CSV text qualifier character.</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable LoadDataTable(string filename, string[] headers, bool ignore_dimension_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            return LoadDataTable(new StreamReader(filename), headers, ignore_dimension_errors, delim, qual);
        }

        /// <summary>
        /// Read in a single CSV file into a datatable in memory
        /// </summary>
        /// <param name="stream">The stream source from which to load the CSV.</param>
        /// <param name="delim">The CSV field delimiter character.</param>
        /// <param name="qual">The CSV text qualifier character.</param>
        /// <returns>An data table of strings that were retrieved from the CSV file.</returns>
        public static DataTable LoadDataTable(StreamReader stream, string[] headers, bool ignore_dimension_errors = true, char delim = CSV.DEFAULT_DELIMITER, char qual = CSV.DEFAULT_QUALIFIER)
        {
            using (CSVReader cr = new CSVReader(stream, delim, qual))
            {
                return cr.ReadAsDataTable(false, ignore_dimension_errors, headers);
            }
        }

        /// <summary>
        /// Convert a CSV file (in string form) into a data table
        /// </summary>
        /// <param name="source_string"></param>
        /// <param name="first_row_are_headers"></param>
        /// <param name="ignore_dimension_errors"></param>
        /// <returns></returns>
        public static DataTable LoadString(string source_string, bool first_row_are_headers, bool ignore_dimension_errors)
        {
            DataTable dt = null;
            byte[] byteArray = Encoding.ASCII.GetBytes(source_string);
            MemoryStream stream = new MemoryStream(byteArray);
            using (CSVReader cr = new CSVReader(new StreamReader(stream)))
            {
                dt = cr.ReadAsDataTable(first_row_are_headers, ignore_dimension_errors);
            }
            return dt;
        }

        /// <summary>
        /// Write a data table to disk at the designated file name in CSV format
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filename"></param>
        /// <param name="save_column_names"></param>
        /// <param name="delim"></param>
        /// <param name="qual"></param>
#if DOTNET20
        public static void SaveAsCSV(DataTable dt, string filename, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#else
        public static void SaveAsCSV(this DataTable dt, string filename, bool save_column_names, char delim = CSV.DEFAULT_CSV_DELIMITER, char qual = DEFAULT_CSV_QUALIFIER)
#endif
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                WriteToStream(dt, sw, save_column_names, delim, qual);
            }
        }

        /// <summary>
        /// Send this 
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
            string csv = WriteToString(dt, true);

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
#if (DOTNET20 || DOTNET35)
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
        /// Write the data table to a stream in CSV format
        /// </summary>
        /// <param name="dt">The data table to write</param>
        /// <param name="sw">The stream where the CSV text will be written</param>
        /// <param name="save_column_names">True if you wish the first line of the file to have column names</param>
        /// <param name="delim">The delimiter (comma, tab, pipe, etc) to separate fields</param>
        /// <param name="qual">The text qualifier (double-quote) that encapsulates fields that include delimiters</param>
#if DOTNET20
        public static void WriteToStream(DataTable dt, StreamWriter sw, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#else
        public static void WriteToStream(this DataTable dt, StreamWriter sw, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#endif
        {
            using (CSVWriter cw = new CSVWriter(sw, delim, qual))
            {
                cw.Write(dt, save_column_names);
            }
        }

        /// <summary>
        /// Write a DataTable to a string in CSV format
        /// </summary>
        /// <param name="dt">The datatable to write</param>
        /// <param name="save_column_names">True if you wish the first line of the file to have column names</param>
        /// <param name="delim">The delimiter (comma, tab, pipe, etc) to separate fields</param>
        /// <param name="qual">The text qualifier (double-quote) that encapsulates fields that include delimiters</param>
        /// <returns>The CSV string representing the object array.</returns>
#if DOTNET20
        public static string WriteToString(DataTable dt, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#else
        public static string WriteToString(this DataTable dt, bool save_column_names, char delim = DEFAULT_DELIMITER, char qual = DEFAULT_QUALIFIER)
#endif
        {
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);
                var cw = new CSVWriter(sw, delim, qual);
                cw.Write(dt, save_column_names);
                sw.Flush();
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }
#endif
        #endregion

    }
}

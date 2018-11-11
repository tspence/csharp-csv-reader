/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;

namespace CSVFile
{
    /// <summary>
    /// Settings to configure how a CSV file is parsed
    /// </summary>
    public class CSVSettings
    {
        /// <summary>
        /// Default constructor picks CSV as the default
        /// </summary>
        public CSVSettings()
        {
            FieldDelimiter = ',';
            TextQualifier = '"';
            ForceQualifiers = false;
            LineSeparator = Environment.NewLine;
            NullToken = null;
            AllowNull = false;
            IgnoreDimensionErrors = true;
            AssumedHeaders = null;
            HeaderRowIncluded = true;
        }

        /// <summary>
        /// The character used to delimit individual fields in the CSV.
        /// </summary>
        public char FieldDelimiter { get; set; }

        /// <summary>
        /// The character used to enclose fields that contain the delimiter character.
        /// </summary>
        public char TextQualifier { get; set; }

        /// <summary>
        /// The separator used to indicate the end of a line in the CSV file.
        /// </summary>
        public string LineSeparator { get; set; }

        /// <summary>
        /// Set this value to true to enclose all fields in the text qualifier character.
        /// </summary>
        public bool ForceQualifiers { get; set; }

        /// <summary>
        /// Set this value to true to allow nulls to be rendered. 
        /// CSV files by default do not permit null fields.  If this field is set to true, all non-null fields
        /// will be enclosed by the text qualifier
        /// </summary>
        public bool AllowNull { get; set; }

        /// <summary>
        /// If AllowNull is set to true, this token will be used to represent NULL values.
        /// </summary>
        public string NullToken { get; set; }

        /// <summary>
        /// The first line of the CSV file will include the names of each field.
        /// </summary>
        public bool HeaderRowIncluded { get; set; }

        /// <summary>
        /// If HeaderRowIncluded is false, use these values for the headers
        /// </summary>
        public List<string> AssumedHeaders { get; set; }

        /// <summary>
        /// Set this value to true to allow parsing for files where each row has a different number of fields
        /// </summary>
        public bool IgnoreDimensionErrors { get; set; }

        /// <summary>
        /// Set this value to true to ignore header errors when deserializing
        /// </summary>
        public bool IgnoreHeaderErrors { get; set; }

        /// <summary>
        /// Standard comma-separated value (CSV) file settings
        /// </summary>
        public static readonly CSVSettings CSV = new CSVSettings();

        /// <summary>
        /// Standard comma-separated value (CSV) file settings that permit rendering of NULL values
        /// </summary>
        public static readonly CSVSettings CSV_PERMIT_NULL = new CSVSettings()
        {
            AllowNull = true,
            NullToken = "NULL"
        };

        /// <summary>
        /// Standard tab-separated value (TSV) file settings
        /// </summary>
        public static readonly CSVSettings TSV = new CSVSettings() { 
            FieldDelimiter = '\t'
        };
    }
}

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
        /// The character used to delimit individual fields in the CSV.
        /// </summary>
        public char FieldDelimiter { get; set; } = ',';

        /// <summary>
        /// The character used to enclose fields that contain the delimiter character.
        /// </summary>
        public char TextQualifier { get; set; } = '"';

        /// <summary>
        /// The separator used to indicate the end of a line in the CSV file.
        /// </summary>
        public string LineSeparator { get; set; } = Environment.NewLine;

        /// <summary>
        /// Set this value to true to enclose all fields in the text qualifier character.
        /// </summary>
        public bool ForceQualifiers { get; set; }

        /// <summary>
        /// Set this value to true to allow nulls to be rendered in CSV files when serializing
        /// and deserializing.
        ///  
        /// CSV files by default do not have a mechanism for differentiating between null fields
        /// and empty fields.  If this field is set to false, both `null` and empty string will
        /// render as an empty string.
        ///
        /// If this field is set to true, all non-null fields will be enclosed by the text qualifier,
        /// and fields that are null will be represented as `NullToken`.
        /// </summary>
        public bool AllowNull { get; set; }

        /// <summary>
        /// If AllowNull is set to true, this token will be used to represent NULL values.
        /// </summary>
        public string NullToken { get; set; }

        /// <summary>
        /// The first line of the CSV file will include the names of each field.
        /// </summary>
        public bool HeaderRowIncluded { get; set; } = true;

        /// <summary>
        /// When reading a CSV file, if the first line contains the instruction `sep=`, use this
        /// to determine the separator for the file.
        ///
        /// The "sep" line is a feature exclusive to Microsoft Excel, which permits CSV files to
        /// more easily handle European files where the comma character is often a separator between
        /// numeric values rather than a field delimiter.
        ///
        /// If this flag is set to true, when you parse a CSV, the first line of the parsing can override
        /// the field separator logic for each individual instance of parsing, but it will not change
        /// the `FieldDelimiter` in your settings object.
        ///
        /// More information:
        /// * [Original Issue Report](https://github.com/tspence/csharp-csv-reader/issues/28)
        /// * [Superuser Article](https://superuser.com/questions/773644/what-is-the-sep-metadata-you-can-add-to-csvs)
        /// * [Tjitjing Blog](https://blog.tjitjing.com/index.php/2016/07/set-separator-delimiter-in-csv-file-to-open-correctly-in-excel.html)
        /// </summary>
        public bool AllowSepLine { get; set; } = true;

        /// <summary>
        /// If `HeaderRowIncluded` is false, use these values for the headers
        /// </summary>
        public List<string> AssumedHeaders { get; set; } = null;

        /// <summary>
        /// Set this value to true to allow parsing for files where each row has a different number of fields
        /// </summary>
        public bool IgnoreDimensionErrors { get; set; } = true;

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

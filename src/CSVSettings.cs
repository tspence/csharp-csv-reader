/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using System.Text;

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
        public string[] AssumedHeaders { get; set; }

        /// <summary>
        /// Set this value to true to allow parsing for files where each row has a different number of fields
        /// </summary>
        public bool IgnoreDimensionErrors { get; set; } = true;

        /// <summary>
        /// Set this value to true to ignore header errors when deserializing
        /// </summary>
        public bool IgnoreHeaderErrors { get; set; }

        /// <summary>
        /// Set this flag to true to ignore read-only properties during serialization
        /// </summary>
        public bool IgnoreReadOnlyProperties { get; set; }

        /// <summary>
        /// Expect headers to be case sensitive during deserialization
        /// </summary>
        public bool HeadersCaseSensitive { get; set; }

        /// <summary>
        /// Exclude these columns during serialization and deserialization
        /// </summary>
        public string[] ExcludedColumns { get; set; }

        /// <summary>
        /// A list of data types that require text qualifiers during serialization.
        /// </summary>
        public Type[] ForceQualifierTypes { get; set; }

        /// <summary>
        /// Some CSV files contain an empty line at the end. If you set this flag to true, deserialization will
        /// not throw an error for empty lines and will instead ignore it.
        /// </summary>
        public bool IgnoreEmptyLineForDeserialization { get; set; }

        /// <summary>
        /// When reading data from a stream, this is the block size to read at once.
        /// </summary>
        public int BufferSize { get; set; } = DEFAULT_BUFFER_SIZE;
        internal static readonly int DEFAULT_BUFFER_SIZE = 65536;

        /// <summary>
        /// The encoding for converting streams of bytes to strings
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// The format to use for serializing date time objects, by default, ISO 8601
        /// </summary>
        public string DateTimeFormat { get; set; } = "o";

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
        public static readonly CSVSettings TSV = new CSVSettings()
        {
            FieldDelimiter = '\t'
        };

        /// <summary>
        /// Clone the existing settings, but with a different field delimiter.
        /// 
        /// Used for parsing of "sep=" lines so that the original object is immutable.
        /// </summary>
        /// <param name="newDelimiter">The new delimiter for the cloned settings</param>
        /// <returns>The newly cloned settings with the updated delimiter</returns>
        public CSVSettings CloneWithNewDelimiter(char newDelimiter)
        {
            var newSettings = (CSVSettings)this.MemberwiseClone();
            newSettings.FieldDelimiter = newDelimiter;
            return newSettings;
        }

        /// <summary>
        /// Retrieve the list of risky characters according to this settings definition
        /// </summary>
        /// <returns></returns>
        public char[] GetRiskyChars()
        {
            var riskyChars = new List<char>();
            riskyChars.Add(FieldDelimiter);
            riskyChars.Add(TextQualifier);
            foreach (var c in LineSeparator)
            {
                riskyChars.Add(c);
            }

            // CRLF is always considered risky
            riskyChars.Add('\n');
            riskyChars.Add('\r');
            return riskyChars.ToArray();
        }

        /// <summary>
        /// Retrieve a hashset containing the list of types that require text qualifiers, or null if this
        /// feature is not used in this settings definition
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Dictionary<Type, int> GetForceQualifierTypes()
        {
            if (ForceQualifierTypes == null) return null;
            var hashSet = new Dictionary<Type, int>();
            foreach (var type in ForceQualifierTypes)
            {
                hashSet.Add(type, 1);
            }

            return hashSet;
        }
    }
}

/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using NUnit.Framework;
using CSVFile;

namespace CSVTestSuite
{
    [TestFixture]
    public class BasicParseTests
    {
        [Test]
        public void ParseBasicCSV()
        {
            // Simplest test
            var line = CSV.ParseLine("1,2,3,4,5");
            Assert.AreEqual(5, line.Length);
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("5", line[4]);

            // Test with a trailing blank item
            line = CSV.ParseLine("1,2,3,4,5,");
            Assert.AreEqual(6, line.Length);
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("", line[5]);

            // Parse oddly formatted CSV from this page: http://stackoverflow.com/questions/11974341/using-filehelper-library-to-parse-csv-strings-but-need-to-ignore-newlines
            line = CSV.ParseLine(@"30: ""NY"", 41: ""JOHN S."", 36: ""HAMPTON"", 42: ""123 Road Street, NY"", 68: ""Y""");
            Assert.AreEqual(6, line.Length);
            Assert.AreEqual(@"30: ""NY""", line[0]);
            Assert.AreEqual(@" 41: ""JOHN S.""", line[1]);
            Assert.AreEqual(@" 36: ""HAMPTON""", line[2]);
            Assert.AreEqual(@" 42: ""123 Road Street", line[3]);
            Assert.AreEqual(@" NY""", line[4]);
            Assert.AreEqual(@" 68: ""Y""", line[5]);

            line = CSV.ParseLine(@"123,""Sue said, """"Hi, this is a test!"""""",2012-08-15");
            Assert.AreEqual(3, line.Length);
            Assert.AreEqual(@"123", line[0]);
            Assert.AreEqual(@"Sue said, ""Hi, this is a test!""", line[1]);
            Assert.AreEqual(@"2012-08-15", line[2]);
        }

        [Test]
        public void ParseTSV()
        {
            // Basic TSV test
            var line = CSV.ParseLine("1\t2\t3\t4\t5", CSVSettings.TSV);
            Assert.AreEqual(5, line.Length);
            Assert.AreEqual("4", line[3]);
            Assert.AreEqual("2", line[1]);

            // Test trailing blank item
            line = CSV.ParseLine("1\t2\t3\t4\t5\t", CSVSettings.TSV);
            Assert.AreEqual(6, line.Length);
            Assert.AreEqual("", line[5]);
            Assert.AreEqual("5", line[4]);
        }

        [Test]
        public void ParseTextQualifiedCSV()
        {
            var line = CSV.ParseLine("1,\"two\",3,\"four\",five");
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("four", line[3]);
            Assert.AreEqual("five", line[4]);
            line = CSV.ParseLine("1,\"two\",3,\"four five\",six");
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("four five", line[3]);
            Assert.AreEqual("six", line[4]);
            line = CSV.ParseLine("1,\"two\",3,\"four five 6\",six");
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("four five 6", line[3]);
            Assert.AreEqual("six", line[4]);

            // Test with embedded delimiters
            line = CSV.ParseLine("1,\"two\",3,\"four, five, and 6\",six");
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("four, five, and 6", line[3]);
            Assert.AreEqual("six", line[4]);

            // Test with doubled up qualifiers
            line = CSV.ParseLine("1,\"two\",3,\"four, \"\"five\"\", and 6\",six");
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("four, \"five\", and 6", line[3]);
            Assert.AreEqual("six", line[4]);

            // Test with an embedded newline
            line = CSV.ParseLine("1,\"two\",3,\"four, \"\"five\n\"\", and 6\",six");
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("four, \"five\n\", and 6", line[3]);
            Assert.AreEqual("six", line[4]);
        }

        [Test]
        public void TestFailingCSVLine()
        {
            // Test a good line
            var line = CSV.ParseLine("1,\"two\",3,\"four, \"\"five\"");
            Assert.AreEqual(4, line.Length);
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1] );
            Assert.AreEqual("3", line[2]);
            Assert.AreEqual("four, \"five", line[3]);

            // Assert that a missing text qualifier  throws an exception
            var ex = Assert.Throws<Exception>(() =>
            {
                CSV.ParseLine("1,\"two\",3,\"four, \"\"five");
            });
            Assert.AreEqual("Malformed CSV structure: MissingTrailingQualifier", ex.Message);

            // Confirm that the more advanced parse reports failure to parse when there is no ending qualifier
            Assert.IsFalse(CSV.TryParseLine("1,\"two\",3,\"four, \"\"five", out line));
        }

        /// <summary>
        /// This test resolves a rare case that occurs with hand-written CSV files.  sometimes users hand typing the file use
        /// "english grammar" and include a space after the comma.  This can trip up algorithms that expect the text qualifier
        /// to be the first character after a delimiter.
        /// </summary>
        [Test]
        public void ParseTextQualifiedCSVWithSpacing()
        {
            var line = CSV.ParseLine("1, \"two\", 3, \"four\", five");
            Assert.AreEqual(5, line.Length);
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual(" 3", line[2]);
            Assert.AreEqual("four", line[3]);
            Assert.AreEqual(" five", line[4]);
            line = CSV.ParseLine("1, \"two\", 3, \"four five\", six");
            Assert.AreEqual(5, line.Length);
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual(" 3", line[2]);
            Assert.AreEqual("four five", line[3]);
            Assert.AreEqual(" six", line[4]);
            line = CSV.ParseLine("1, \"two\", 3, \"four five 6\", six");
            Assert.AreEqual(5, line.Length);
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual(" 3", line[2]);
            Assert.AreEqual("four five 6", line[3]);
            Assert.AreEqual(" six", line[4]);
            line = CSV.ParseLine("1, \"two\", 3, \"four, five, and 6\", six");
            Assert.AreEqual(5, line.Length);
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual(" 3", line[2]);
            Assert.AreEqual("four, five, and 6", line[3]);
            Assert.AreEqual(" six", line[4]);
            line = CSV.ParseLine("1, \"two\", 3, \"four, \"\"five\"\", \nand 6\", six");
            Assert.AreEqual(5, line.Length);
            Assert.AreEqual("1", line[0]);
            Assert.AreEqual("two", line[1]);
            Assert.AreEqual(" 3", line[2]);
            Assert.AreEqual("four, \"five\", \nand 6", line[3]);
            Assert.AreEqual(" six", line[4]);
        }

        [Test]
        public void ParseSepLineTest()
        {
            Assert.AreEqual('|', CSV.ParseSepLine("sep=|"));
            Assert.AreEqual('|', CSV.ParseSepLine("sep=   |"));
            Assert.AreEqual('|', CSV.ParseSepLine("sep=|     "));
            Assert.AreEqual('|', CSV.ParseSepLine("sep=    |     "));
            Assert.AreEqual('|', CSV.ParseSepLine("sep = |"));
            Assert.AreEqual(',', CSV.ParseSepLine("sep=,"));
            Assert.AreEqual(',', CSV.ParseSepLine("sep = ,"));
            Assert.AreEqual(null, CSV.ParseSepLine("sep="));
            Assert.Throws<Exception>(() =>
            {
                CSV.ParseSepLine("sep= this is a test since separators can't be more than a single character");
            });
        }
        
        [Test]
        public void TestIssue53()
        {
            // This use case was reported by wvdvegt as https://github.com/tspence/csharp-csv-reader/issues/53
            var line = CSV.ParseLine("\"test\",\"\n\",,,,\"Normal\",\"False\",,,\"Normal\",\"\"");
            Assert.AreEqual("test", line[0]);
            Assert.AreEqual("\n", line[1]);
            Assert.AreEqual("", line[2]);
            Assert.AreEqual("", line[3]);
            Assert.AreEqual("", line[4]);
            Assert.AreEqual("Normal", line[5]);
            Assert.AreEqual("False", line[6]);
            Assert.AreEqual("", line[7]);
            Assert.AreEqual("", line[8]);
            Assert.AreEqual("Normal", line[9]);
            Assert.AreEqual("", line[10]);
            
            // Try same thing with MS-DOS newlines - CRLF
            var line2 = CSV.ParseLine("\"test\",\"\r\n\",,,,\"Normal\",\"False\",,,\"Normal\",\"\"");
            Assert.AreEqual("test", line2[0]);
            Assert.AreEqual("\r\n", line2[1]);
            Assert.AreEqual("", line2[2]);
            Assert.AreEqual("", line2[3]);
            Assert.AreEqual("", line2[4]);
            Assert.AreEqual("Normal", line2[5]);
            Assert.AreEqual("False", line2[6]);
            Assert.AreEqual("", line2[7]);
            Assert.AreEqual("", line2[8]);
            Assert.AreEqual("Normal", line2[9]);
            Assert.AreEqual("", line2[10]);
            
            // Try same thing with just LF only
            var line3 = CSV.ParseLine("\"test\",\"\r\",,,,\"Normal\",\"False\",,,\"Normal\",\"\"");
            Assert.AreEqual("test", line3[0]);
            Assert.AreEqual("\r", line3[1]);
            Assert.AreEqual("", line3[2]);
            Assert.AreEqual("", line3[3]);
            Assert.AreEqual("", line3[4]);
            Assert.AreEqual("Normal", line3[5]);
            Assert.AreEqual("False", line3[6]);
            Assert.AreEqual("", line3[7]);
            Assert.AreEqual("", line3[8]);
            Assert.AreEqual("Normal", line3[9]);
            Assert.AreEqual("", line3[10]);
        }

        [Test]
        public void TestMultipleNewlines()
        {
            // Specific issue reported by domdere
            var line1 = CSV.ParseLine("\"test\",\"blah\r\n\r\n\r\nfoo\",\"Normal\"");
            Assert.AreEqual("test", line1[0]);
            Assert.AreEqual("blah\r\n\r\n\r\nfoo", line1[1]);
            Assert.AreEqual("Normal", line1[2]);

            // Test a few potential use cases here
            var line2 = CSV.ParseLine("\"test\",\"\n\n\",\"\r\n\r\n\r\n\",\"Normal\",\"\",\"\r\r\r\r\r\"");
            Assert.AreEqual("test", line2[0]);
            Assert.AreEqual("\n\n", line2[1]);
            Assert.AreEqual("\r\n\r\n\r\n", line2[2]);
            Assert.AreEqual("Normal", line2[3]);
            Assert.AreEqual("", line2[4]);
            Assert.AreEqual("\r\r\r\r\r", line2[5]);
        }
    }
}

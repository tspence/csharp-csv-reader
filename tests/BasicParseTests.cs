/*
 * 2006 - 2016 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
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
            string[] line = CSV.ParseLine("1,2,3,4,5");
            Assert.AreEqual(line.Length, 5);
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[4], "5");

            // Test with a trailing blank item
            line = CSV.ParseLine("1,2,3,4,5,");
            Assert.AreEqual(line.Length, 6);
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[5], "");

            // Parse oddly formatted CSV from this page: http://stackoverflow.com/questions/11974341/using-filehelper-library-to-parse-csv-strings-but-need-to-ignore-newlines
            line = CSV.ParseLine(@"30: ""NY"", 41: ""JOHN S."", 36: ""HAMPTON"", 42: ""123 Road Street, NY"", 68: ""Y""");
            Assert.AreEqual(line.Length, 6);
            Assert.AreEqual(line[0], @"30: ""NY""");
            Assert.AreEqual(line[1], @" 41: ""JOHN S.""");
            Assert.AreEqual(line[2], @" 36: ""HAMPTON""");
            Assert.AreEqual(line[3], @" 42: ""123 Road Street");
            Assert.AreEqual(line[4], @" NY""");
            Assert.AreEqual(line[5], @" 68: ""Y""");

            line = CSV.ParseLine(@"123,""Sue said, """"Hi, this is a test!"""""",2012-08-15");
            Assert.AreEqual(line.Length, 3);
            Assert.AreEqual(line[0], @"123");
            Assert.AreEqual(line[1], @"Sue said, ""Hi, this is a test!""");
            Assert.AreEqual(line[2], @"2012-08-15");
        }

        [Test]
        public void ParseTSV()
        {
            // Basic TSV test
            string[] line = CSV.ParseLine("1\t2\t3\t4\t5", '\t', '\"');
            Assert.AreEqual(line.Length, 5);
            Assert.AreEqual(line[3], "4");
            Assert.AreEqual(line[1], "2");

            // Test trailing blank item
            line = CSV.ParseLine("1\t2\t3\t4\t5\t", '\t', '\"');
            Assert.AreEqual(line.Length, 6);
            Assert.AreEqual(line[5], "");
            Assert.AreEqual(line[4], "5");
        }

        [Test]
        public void ParseTextQualifiedCSV()
        {
            string[] line = null;

            line = CSV.ParseLine("1,\"two\",3,\"four\",five");
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[3], "four");
            Assert.AreEqual(line[4], "five");
            line = CSV.ParseLine("1,\"two\",3,\"four five\",six");
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[3], "four five");
            Assert.AreEqual(line[4], "six");
            line = CSV.ParseLine("1,\"two\",3,\"four five 6\",six");
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[3], "four five 6");
            Assert.AreEqual(line[4], "six");

            // Test with embedded delimiters
            line = CSV.ParseLine("1,\"two\",3,\"four, five, and 6\",six");
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[3], "four, five, and 6");
            Assert.AreEqual(line[4], "six");

            // Test with doubled up qualifiers
            line = CSV.ParseLine("1,\"two\",3,\"four, \"\"five\"\", and 6\",six");
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[3], "four, \"five\", and 6");
            Assert.AreEqual(line[4], "six");

            // Test with an embedded newline
            line = CSV.ParseLine("1,\"two\",3,\"four, \"\"five\n\"\", and 6\",six");
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[3], "four, \"five\n\", and 6");
            Assert.AreEqual(line[4], "six");
        }

        [Test]
        public void TestFailingCSVLine()
        {
            string[] line = null;

            // Use a basic parse that will do its best but fail to recognize the problem
            line = CSV.ParseLine("1,\"two\",3,\"four, \"\"five");
            Assert.AreEqual(line.Length, 4);
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], "3");
            Assert.AreEqual(line[3], "four, \"five");

            // Confirm that the more advanced parse will fail
            Assert.IsFalse(CSV.TryParseLine("1,\"two\",3,\"four, \"\"five", CSV.DEFAULT_DELIMITER, CSV.DEFAULT_QUALIFIER, out line));
        }

        /// <summary>
        /// This test resolves a rare case that occurs with hand-written CSV files.  sometimes users hand typing the file use
        /// "english grammar" and include a space after the comma.  This can trip up algorithms that expect the text qualifier
        /// to be the first character after a delimiter.
        /// </summary>
        [Test]
        public void ParseTextQualifiedCSVWithSpacing()
        {
            string[] line = null;

            line = CSV.ParseLine("1, \"two\", 3, \"four\", five");
            Assert.AreEqual(line.Length, 5);
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], " 3");
            Assert.AreEqual(line[3], "four");
            Assert.AreEqual(line[4], " five");
            line = CSV.ParseLine("1, \"two\", 3, \"four five\", six");
            Assert.AreEqual(line.Length, 5);
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], " 3");
            Assert.AreEqual(line[3], "four five");
            Assert.AreEqual(line[4], " six"); 
            line = CSV.ParseLine("1, \"two\", 3, \"four five 6\", six");
            Assert.AreEqual(line.Length, 5);
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], " 3");
            Assert.AreEqual(line[3], "four five 6");
            Assert.AreEqual(line[4], " six");
            line = CSV.ParseLine("1, \"two\", 3, \"four, five, and 6\", six");
            Assert.AreEqual(line.Length, 5);
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], " 3");
            Assert.AreEqual(line[3], "four, five, and 6");
            Assert.AreEqual(line[4], " six");
            line = CSV.ParseLine("1, \"two\", 3, \"four, \"\"five\"\", \nand 6\", six");
            Assert.AreEqual(line.Length, 5);
            Assert.AreEqual(line[0], "1");
            Assert.AreEqual(line[1], "two");
            Assert.AreEqual(line[2], " 3");
            Assert.AreEqual(line[3], "four, \"five\", \nand 6");
            Assert.AreEqual(line[4], " six");
        }
    }
}

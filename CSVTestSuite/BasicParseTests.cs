using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSVFile;

namespace CSVTestSuite
{
    [TestClass]
    public class BasicParseTests
    {
        [TestMethod]
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
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
        [TestMethod]
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

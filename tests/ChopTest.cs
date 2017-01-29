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
using System.IO;
using CSVFile;
using System.Data;

namespace CSVTestSuite
{
    [TestFixture]
    public class ChopTest
    {
#if !PORTABLE
        [Test]
        public void TestChoppingFiles()
        {
            string source = @"timestamp,TestString,SetComment,PropertyString,IntField,IntProperty
2012-05-01,test1,""Hi there, I said!"",Bob,57,0
2011-04-01,test2,""What's up, buttercup?"",Ralph,1,-999
1975-06-03,test3,""Bye and bye, dragonfly!"",Jimmy's The Bomb,12,13";
            DataTable dt = CSV.LoadString(source, true, false);

            // Save this string to a test file
            string test_rootfn = Guid.NewGuid().ToString();
            string sourcefile = test_rootfn + ".csv";
            CSV.SaveAsCSV(dt, sourcefile, true);

            // Create an empty test folder
            string dirname = Guid.NewGuid().ToString();
            Directory.CreateDirectory(dirname);

            // Chop this file into one-line chunks
            CSV.ChopFile(sourcefile, dirname, true, 1);

            // Verify that we got three files
            string[] files = Directory.GetFiles(dirname);
            Assert.AreEqual(3, files.Length);

            // Read in each file and verify that each one has one line
            dt = CSV.LoadDataTable(files[0]);
            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual("2012-05-01", dt.Rows[0].ItemArray[0]);
            Assert.AreEqual("test1", dt.Rows[0].ItemArray[1]);

            dt = CSV.LoadDataTable(files[1]);
            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual("2011-04-01", dt.Rows[0].ItemArray[0]);
            Assert.AreEqual("test2", dt.Rows[0].ItemArray[1]);

            dt = CSV.LoadDataTable(files[2]);
            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual("1975-06-03", dt.Rows[0].ItemArray[0]);
            Assert.AreEqual("test3", dt.Rows[0].ItemArray[1]);

            // Clean up
            Directory.Delete(dirname, true);
            File.Delete(sourcefile);
        }

        [Test]
        public void LargeChopTest()
        {
            string[] array_first = new string[] { "first", "two", "three", "four, five" };
            string[] array_second = new string[] { "second", "two", "three", "four, five" };
            string[] array_third = new string[] { "third", "two", "three", "four, five" };
            DataTable dt = new DataTable();
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            dt.Columns.Add("col3");
            dt.Columns.Add("col4");
            dt.Columns.Add("col5");
            for (int i = 0; i < 5000; i++) {
                dt.Rows.Add(array_first);
            }
            for (int i = 0; i < 5000; i++) {
                dt.Rows.Add(array_second);
            }
            for (int i = 0; i < 5000; i++) {
                dt.Rows.Add(array_third);
            }

            // Save this string to a test file
            string test_rootfn = Guid.NewGuid().ToString();
            string sourcefile = test_rootfn + ".csv";
            CSV.SaveAsCSV(dt, sourcefile, true);

            // Create an empty test folder
            string dirname = Guid.NewGuid().ToString();
            Directory.CreateDirectory(dirname);

            // Chop this file into one-line chunks
            CSV.ChopFile(sourcefile, dirname, true, 5000);

            // Verify that we got three files
            string[] files = Directory.GetFiles(dirname);
            Assert.AreEqual(3, files.Length);

            // Read in each file and verify that each one has one line
            dt = CSV.LoadDataTable(files[0]);
            Assert.AreEqual(5000, dt.Rows.Count);
            Assert.AreEqual("first", dt.Rows[0].ItemArray[0]);

            dt = CSV.LoadDataTable(files[1]);
            Assert.AreEqual(5000, dt.Rows.Count);
            Assert.AreEqual("second", dt.Rows[0].ItemArray[0]);

            dt = CSV.LoadDataTable(files[2]);
            Assert.AreEqual(5000, dt.Rows.Count);
            Assert.AreEqual("third", dt.Rows[0].ItemArray[0]);

            // Clean up
            Directory.Delete(dirname, true);
            File.Delete(sourcefile);
        }
#endif
    }
}

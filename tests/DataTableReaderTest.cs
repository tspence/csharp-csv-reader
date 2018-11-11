/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using CSVFile;
using System.IO;
using System.Data;

namespace CSVTestSuite
{
#if HAS_DATATABLE
    [TestFixture]
    public class DataTableReaderTest
    {
        const string source = "Name,Title,Phone\n" +
            "JD,Doctor,x234\n" +
            "Janitor,Janitor,x235\n" +
            "\"Dr. Reed, Eliot\",Private Practice,x236\n" +
            "Dr. Kelso,Chief of Medicine,x100";

        const string source_embedded_newlines = "Name,Title,Phone\n" +
            "JD,Doctor,x234\n" +
            "Janitor,Janitor,x235\n" +
            "\"Dr. Reed, \nEliot\",\"Private \"\"Practice\"\"\",x236\n" +
            "Dr. Kelso,Chief of Medicine,x100";

        [Test]
        public void TestBasicDataTable()
        {
            DataTable dt = CSVDataTable.FromString(source);
            Assert.AreEqual(dt.Columns.Count, 3);
            Assert.AreEqual(dt.Rows.Count, 4);
            Assert.AreEqual(dt.Rows[0].ItemArray[0], "JD");
            Assert.AreEqual(dt.Rows[1].ItemArray[0], "Janitor");
            Assert.AreEqual(dt.Rows[2].ItemArray[0], "Dr. Reed, Eliot");
            Assert.AreEqual(dt.Rows[3].ItemArray[0], "Dr. Kelso");
            Assert.AreEqual(dt.Rows[0].ItemArray[1], "Doctor");
            Assert.AreEqual(dt.Rows[1].ItemArray[1], "Janitor");
            Assert.AreEqual(dt.Rows[2].ItemArray[1], "Private Practice");
            Assert.AreEqual(dt.Rows[3].ItemArray[1], "Chief of Medicine");
            Assert.AreEqual(dt.Rows[0].ItemArray[2], "x234");
            Assert.AreEqual(dt.Rows[1].ItemArray[2], "x235");
            Assert.AreEqual(dt.Rows[2].ItemArray[2], "x236");
            Assert.AreEqual(dt.Rows[3].ItemArray[2], "x100");
        }

        [Test]
        public void TestDataTableWithEmbeddedNewlines()
        {
            DataTable dt = CSVDataTable.FromString(source_embedded_newlines);
            Assert.AreEqual(dt.Columns.Count, 3);
            Assert.AreEqual(dt.Rows.Count, 4);
            Assert.AreEqual(dt.Rows[0].ItemArray[0], "JD");
            Assert.AreEqual(dt.Rows[1].ItemArray[0], "Janitor");
            Assert.AreEqual(dt.Rows[2].ItemArray[0], "Dr. Reed, " + Environment.NewLine + "Eliot");
            Assert.AreEqual(dt.Rows[3].ItemArray[0], "Dr. Kelso");
            Assert.AreEqual(dt.Rows[0].ItemArray[1], "Doctor");
            Assert.AreEqual(dt.Rows[1].ItemArray[1], "Janitor");
            Assert.AreEqual(dt.Rows[2].ItemArray[1], "Private \"Practice\"");
            Assert.AreEqual(dt.Rows[3].ItemArray[1], "Chief of Medicine");
            Assert.AreEqual(dt.Rows[0].ItemArray[2], "x234");
            Assert.AreEqual(dt.Rows[1].ItemArray[2], "x235");
            Assert.AreEqual(dt.Rows[2].ItemArray[2], "x236");
            Assert.AreEqual(dt.Rows[3].ItemArray[2], "x100");
        }

        [Test]
        public void DataTableChopTest()
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
            string sourcefile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
            CSVDataTable.WriteToFile(dt, sourcefile);

            // Create an empty test folder
            string dirname = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirname);

            // Chop this file into one-line chunks
            CSVReader.ChopFile(sourcefile, dirname, 5000);

            // Verify that we got three files
            string[] files = Directory.GetFiles(dirname);
            Assert.AreEqual(3, files.Length);

            // Read in each file and verify that each one has one line
            dt = CSVDataTable.FromFile(files[0]);
            Assert.AreEqual(5000, dt.Rows.Count);
            Assert.AreEqual("first", dt.Rows[0].ItemArray[0]);

            dt = CSVDataTable.FromFile(files[1]);
            Assert.AreEqual(5000, dt.Rows.Count);
            Assert.AreEqual("second", dt.Rows[0].ItemArray[0]);

            dt = CSVDataTable.FromFile(files[2]);
            Assert.AreEqual(5000, dt.Rows.Count);
            Assert.AreEqual("third", dt.Rows[0].ItemArray[0]);

            // Clean up
            Directory.Delete(dirname, true);
            File.Delete(sourcefile);
        }

        [Test]
        public void DataTableChoppingFiles()
        {
            string source = @"timestamp,TestString,SetComment,PropertyString,IntField,IntProperty
2012-05-01,test1,""Hi there, I said!"",Bob,57,0
2011-04-01,test2,""What's up, buttercup?"",Ralph,1,-999
1975-06-03,test3,""Bye and bye, dragonfly!"",Jimmy's The Bomb,12,13";
            DataTable dt = CSVDataTable.FromString(source);

            // Save this string to a test file
            string outfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
            CSVDataTable.WriteToFile(dt, outfile);

            // Create an empty test folder
            string dirname = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirname);

            // Chop this file into one-line chunks
            CSVReader.ChopFile(outfile, dirname, 1);

            // Verify that we got three files
            string[] files = Directory.GetFiles(dirname);
            Assert.AreEqual(3, files.Length);

            // Read in each file and verify that each one has one line
            dt = CSVDataTable.FromFile(files[0]);
            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual("2012-05-01", dt.Rows[0].ItemArray[0]);
            Assert.AreEqual("test1", dt.Rows[0].ItemArray[1]);

            dt = CSVDataTable.FromFile(files[1]);
            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual("2011-04-01", dt.Rows[0].ItemArray[0]);
            Assert.AreEqual("test2", dt.Rows[0].ItemArray[1]);

            dt = CSVDataTable.FromFile(files[2]);
            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual("1975-06-03", dt.Rows[0].ItemArray[0]);
            Assert.AreEqual("test3", dt.Rows[0].ItemArray[1]);

            // Clean up
            Directory.Delete(dirname, true);
            File.Delete(outfile);
        }
    }
#endif
}

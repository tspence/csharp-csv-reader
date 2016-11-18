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
using System.Data;
using CSVFile;
using System.IO;

namespace CSVTestSuite
{
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

#if !PORTABLE
        [Test]
        public void TestBasicDataTable()
        {
            DataTable dt = CSV.LoadString(source, true, false);
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
            DataTable dt = CSV.LoadString(source_embedded_newlines, true, false);
            Assert.AreEqual(dt.Columns.Count, 3);
            Assert.AreEqual(dt.Rows.Count, 4);
            Assert.AreEqual(dt.Rows[0].ItemArray[0], "JD");
            Assert.AreEqual(dt.Rows[1].ItemArray[0], "Janitor");
            Assert.AreEqual(dt.Rows[2].ItemArray[0], "Dr. Reed, \nEliot");
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
#endif

#if PORTABLE
        [Test]
        public void TestBasicDataTable()
        {
            var list = CSV.LoadString(source, true, false);

            // Equivalent of "columns"
            Assert.AreEqual(list[0].Length, 3);

            // Equivalent of "rows" - includes header row
            Assert.AreEqual(list.Count, 5);

            // Test values
            Assert.AreEqual(list[1][0], "JD");
            Assert.AreEqual(list[2][0], "Janitor");
            Assert.AreEqual(list[3][0], "Dr. Reed, Eliot");
            Assert.AreEqual(list[4][0], "Dr. Kelso");
            Assert.AreEqual(list[1][1], "Doctor");
            Assert.AreEqual(list[2][1], "Janitor");
            Assert.AreEqual(list[3][1], "Private Practice");
            Assert.AreEqual(list[4][1], "Chief of Medicine");
            Assert.AreEqual(list[1][2], "x234");
            Assert.AreEqual(list[2][2], "x235");
            Assert.AreEqual(list[3][2], "x236");
            Assert.AreEqual(list[4][2], "x100");
        }

        [Test]
        public void TestDataTableWithEmbeddedNewlines()
        {


            var list = CSV.LoadString(source_embedded_newlines, true, false);

            // Equivalent of "columns"
            Assert.AreEqual(list[0].Length, 3);

            // Equivalent of "rows"
            Assert.AreEqual(list.Count, 5);

            Assert.AreEqual(list[1][0], "JD");
            Assert.AreEqual(list[2][0], "Janitor");
            Assert.AreEqual(list[3][0], "Dr. Reed, \nEliot");
            Assert.AreEqual(list[4][0], "Dr. Kelso");
            Assert.AreEqual(list[1][1], "Doctor");
            Assert.AreEqual(list[2][1], "Janitor");
            Assert.AreEqual(list[3][1], "Private \"Practice\"");
            Assert.AreEqual(list[4][1], "Chief of Medicine");
            Assert.AreEqual(list[1][2], "x234");
            Assert.AreEqual(list[2][2], "x235");
            Assert.AreEqual(list[3][2], "x236");
            Assert.AreEqual(list[4][2], "x100");
        }
#endif


    }
}

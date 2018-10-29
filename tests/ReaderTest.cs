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

namespace CSVTestSuite
{
    [TestFixture]
    public class ReaderTest
    {
        [Test]
        public void TestBasicReader()
        {
            string source = "Name,Title,Phone\n" +
                "JD,Doctor,x234\n" +
                "Janitor,Janitor,x235\n" +
                "\"Dr. Reed, " + Environment.NewLine + "Eliot\",\"Private \"\"Practice\"\"\",x236\n" +
                "Dr. Kelso,Chief of Medicine,x100";

            // Skip header row
            CSVSettings settings = new CSVSettings()
            {
                HeaderRowIncluded = false
            };

            // Convert into stream
            byte[] byteArray = Encoding.UTF8.GetBytes(source);
            MemoryStream stream = new MemoryStream(byteArray);
            using (StreamReader sr = new StreamReader(stream)) {
                using (CSVReader cr = new CSVReader(sr, settings)) {
                    int i = 0;
                    foreach (string[] line in cr) {
                        if (i == 0) {
                            Assert.AreEqual(line[0], "Name");
                            Assert.AreEqual(line[1], "Title");
                            Assert.AreEqual(line[2], "Phone");
                        } else if (i == 1) {
                            Assert.AreEqual(line[0], "JD");
                            Assert.AreEqual(line[1], "Doctor");
                            Assert.AreEqual(line[2], "x234");
                        } else if (i == 2) {
                            Assert.AreEqual(line[0], "Janitor");
                            Assert.AreEqual(line[1], "Janitor");
                            Assert.AreEqual(line[2], "x235");
                        } else if (i == 3) {
                            Assert.AreEqual(line[0], "Dr. Reed, " + Environment.NewLine + "Eliot");
                            Assert.AreEqual(line[1], "Private \"Practice\"");
                            Assert.AreEqual(line[2], "x236");
                        } else if (i == 4) {
                            Assert.AreEqual(line[0], "Dr. Kelso");
                            Assert.AreEqual(line[1], "Chief of Medicine");
                            Assert.AreEqual(line[2], "x100");
                        }
                        i++;
                    }
                }
            }
        }

        [Test]
        public void TestAlternateDelimiterQualifiers()
        {
            string source = "Name\tTitle\tPhone\n" +
                "JD\tDoctor\tx234\n" +
                "Janitor\tJanitor\tx235\n" +
                "\"Dr. Reed, " + Environment.NewLine + "Eliot\"\t\"Private \"\"Practice\"\"\"\tx236\n" +
                "Dr. Kelso\tChief of Medicine\tx100";

            // Convert into stream
            byte[] byteArray = Encoding.UTF8.GetBytes(source);
            MemoryStream stream = new MemoryStream(byteArray);
            using (StreamReader sr = new StreamReader(stream)) {
                using (CSVReader cr = new CSVReader(sr, CSVSettings.TSV)) {
                    Assert.AreEqual(cr.Headers[0], "Name");
                    Assert.AreEqual(cr.Headers[1], "Title");
                    Assert.AreEqual(cr.Headers[2], "Phone");
                    int i = 1;
                    foreach (string[] line in cr) {
                        if (i == 1) {
                            Assert.AreEqual(line[0], "JD");
                            Assert.AreEqual(line[1], "Doctor");
                            Assert.AreEqual(line[2], "x234");
                        } else if (i == 2) {
                            Assert.AreEqual(line[0], "Janitor");
                            Assert.AreEqual(line[1], "Janitor");
                            Assert.AreEqual(line[2], "x235");
                        } else if (i == 3) {
                            Assert.AreEqual(line[0], "Dr. Reed, " + Environment.NewLine + "Eliot");
                            Assert.AreEqual(line[1], "Private \"Practice\"");
                            Assert.AreEqual(line[2], "x236");
                        } else if (i == 4) {
                            Assert.AreEqual(line[0], "Dr. Kelso");
                            Assert.AreEqual(line[1], "Chief of Medicine");
                            Assert.AreEqual(line[2], "x100");
                        }
                        i++;
                    }
                }
            }
        }
    }
}

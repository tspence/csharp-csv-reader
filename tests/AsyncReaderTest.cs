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
using System.Threading.Tasks;

namespace CSVTestSuite
{
    [TestFixture]
    public class AsyncReaderTest
    {
        [Test]
        public async Task TestBasicReader()
        {
            var source = "Name,Title,Phone\n" +
                "JD,Doctor,x234\n" +
                "Janitor,Janitor,x235\n" +
                "\"Dr. Reed, " + Environment.NewLine + "Eliot\",\"Private \"\"Practice\"\"\",x236\n" +
                "Dr. Kelso,Chief of Medicine,x100";

            // Skip header row
            var settings = new CSVSettings()
            {
                HeaderRowIncluded = false
            };

            // Convert into stream
            using (var cr = CSVReader.FromString(source, settings))
            {
                var i = 0;
                await foreach (var line in cr)
                {
                    Assert.AreEqual(3, line.Length);
                    switch (i)
                    {
                        case 0:
                            Assert.AreEqual(line[0], "Name");
                            Assert.AreEqual(line[1], "Title");
                            Assert.AreEqual(line[2], "Phone");
                            break;
                        case 1:
                            Assert.AreEqual(line[0], "JD");
                            Assert.AreEqual(line[1], "Doctor");
                            Assert.AreEqual(line[2], "x234");
                            break;
                        case 2:
                            Assert.AreEqual(line[0], "Janitor");
                            Assert.AreEqual(line[1], "Janitor");
                            Assert.AreEqual(line[2], "x235");
                            break;
                        case 3:
                            Assert.AreEqual(line[0], "Dr. Reed, " + Environment.NewLine + "Eliot");
                            Assert.AreEqual(line[1], "Private \"Practice\"");
                            Assert.AreEqual(line[2], "x236");
                            break;
                        case 4:
                            Assert.AreEqual(line[0], "Dr. Kelso");
                            Assert.AreEqual(line[1], "Chief of Medicine");
                            Assert.AreEqual(line[2], "x100");
                            break;
                        default:
                            Assert.IsTrue(false, "Should not get here");
                            break;
                    }

                    i++;
                }
            }
        }


        [Test]
        public async Task TestDanglingFields()
        {
            var source = "Name,Title,Phone,Dangle\n" +
                "JD,Doctor,x234,\n" +
                "Janitor,Janitor,x235,\n" +
                "\"Dr. Reed, " + Environment.NewLine + "Eliot\",\"Private \"\"Practice\"\"\",x236,\n" +
                "Dr. Kelso,Chief of Medicine,x100,\n" +
                ",,,";

            // Skip header row
            var settings = new CSVSettings()
            {
                HeaderRowIncluded = false
            };

            // Convert into stream
            using (var cr = CSVReader.FromString(source, settings))
            {
                var i = 0;
                await foreach (var line in cr)
                {
                    Assert.AreEqual(4, line.Length);
                    switch (i)
                    {
                        case 0:
                            Assert.AreEqual(line[0], "Name");
                            Assert.AreEqual(line[1], "Title");
                            Assert.AreEqual(line[2], "Phone");
                            Assert.AreEqual(line[3], "Dangle");
                            break;
                        case 1:
                            Assert.AreEqual(line[0], "JD");
                            Assert.AreEqual(line[1], "Doctor");
                            Assert.AreEqual(line[2], "x234");
                            Assert.AreEqual(line[3], "");
                            break;
                        case 2:
                            Assert.AreEqual(line[0], "Janitor");
                            Assert.AreEqual(line[1], "Janitor");
                            Assert.AreEqual(line[2], "x235");
                            Assert.AreEqual(line[3], "");
                            break;
                        case 3:
                            Assert.AreEqual(line[0], "Dr. Reed, " + Environment.NewLine + "Eliot");
                            Assert.AreEqual(line[1], "Private \"Practice\"");
                            Assert.AreEqual(line[2], "x236");
                            Assert.AreEqual(line[3], "");
                            break;
                        case 4:
                            Assert.AreEqual(line[0], "Dr. Kelso");
                            Assert.AreEqual(line[1], "Chief of Medicine");
                            Assert.AreEqual(line[2], "x100");
                            Assert.AreEqual(line[3], "");
                            break;
                        case 5:
                            Assert.AreEqual(line[0], "");
                            Assert.AreEqual(line[1], "");
                            Assert.AreEqual(line[2], "");
                            Assert.AreEqual(line[3], "");
                            break;
                        default:
                            Assert.IsTrue(false, "Should not get here");
                            break;
                    }

                    i++;
                }
            }
        }

        [Test]
        public async Task TestAlternateDelimiterQualifiers()
        {
            var source = "Name\tTitle\tPhone\n" +
                         "JD\tDoctor\tx234\n" +
                         "Janitor\tJanitor\tx235\n" +
                         "\"Dr. Reed, " + Environment.NewLine + "Eliot\"\t\"Private \"\"Practice\"\"\"\tx236\n" +
                         "Dr. Kelso\tChief of Medicine\tx100";

            // Convert into stream
            var settings = new CSVSettings() { HeaderRowIncluded = true, FieldDelimiter = '\t' };
            using (var cr = CSVReader.FromString(source, settings))
            {
                Assert.AreEqual(cr.Headers[0], "Name");
                Assert.AreEqual(cr.Headers[1], "Title");
                Assert.AreEqual(cr.Headers[2], "Phone");
                var i = 1;
                await foreach (var line in cr)
                {
                    switch (i)
                    {
                        case 1:
                            Assert.AreEqual(line[0], "JD");
                            Assert.AreEqual(line[1], "Doctor");
                            Assert.AreEqual(line[2], "x234");
                            break;
                        case 2:
                            Assert.AreEqual(line[0], "Janitor");
                            Assert.AreEqual(line[1], "Janitor");
                            Assert.AreEqual(line[2], "x235");
                            break;
                        case 3:
                            Assert.AreEqual(line[0], "Dr. Reed, " + Environment.NewLine + "Eliot");
                            Assert.AreEqual(line[1], "Private \"Practice\"");
                            Assert.AreEqual(line[2], "x236");
                            break;
                        case 4:
                            Assert.AreEqual(line[0], "Dr. Kelso");
                            Assert.AreEqual(line[1], "Chief of Medicine");
                            Assert.AreEqual(line[2], "x100");
                            break;
                    }

                    i++;
                }
            }
        }
    }
}

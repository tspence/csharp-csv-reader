/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using CSVFile;
using System.Linq;

namespace CSVTestSuite
{
    [TestFixture]
    public class ChopTest
    {
        public class SimpleChopClass
        {
            public int id { get; set; }
            public string name { get; set; }
            public string email { get; set; }
        }

        [Test]
        public void DataTableChopTest()
        {
            var list = new List<SimpleChopClass>();
            for (var i = 0; i < 5000; i++)
            {
                list.Add(new SimpleChopClass()
                {
                    id = i,
                    name = $"Bob Smith #{i}",
                    email = $"bob.smith.{i}@example.com"
                });
            }
            for (var i = 5000; i < 10000; i++)
            {
                list.Add(new SimpleChopClass()
                {
                    id = i,
                    name = $"Alice Jones #{i}",
                    email = $"alice.jones.{i}@example.com"
                });
            }
            for (var i = 10000; i < 15000; i++)
            {
                list.Add(new SimpleChopClass()
                {
                    id = i,
                    name = $"Charlie Jenkins #{i}",
                    email = $"charlie.jenkins.{i}@example.com"
                });
            }

            // Save this string to a test file
            var singleFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
            var dirname = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                var rawString = CSV.Serialize<SimpleChopClass>(list);
                File.WriteAllText(singleFile, rawString);

                // Create an empty test folder
                Directory.CreateDirectory(dirname);

                // Chop this file into one-line chunks
                CSVReader.ChopFile(singleFile, dirname, 5000);

                // Verify that we got three files
                var files = Directory.GetFiles(dirname).OrderBy(f => f).ToArray();
                Assert.AreEqual(3, files.Length);

                // Read in each file and verify that each one has one line
                var f1 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[0])).ToList();
                Assert.AreEqual(5000, f1.Count);

                var f2 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[1])).ToList();
                Assert.AreEqual(5000, f2.Count);

                var f3 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[2])).ToList();
                Assert.AreEqual(5000, f3.Count);

                // Merge and verify
                var results = new List<SimpleChopClass>();
                results.AddRange(f1);
                results.AddRange(f2);
                results.AddRange(f3);
                for (var i = 0; i < list.Count; i++)
                {
                    Assert.AreEqual(list[i].id, results[i].id);
                    Assert.AreEqual(list[i].name, results[i].name);
                    Assert.AreEqual(list[i].email, results[i].email);
                }
            }
            // Clean up
            finally
            {
                if (Directory.Exists(dirname))
                {
                    Directory.Delete(dirname, true);
                }
                if (File.Exists(singleFile))
                {
                    File.Delete(singleFile);
                }
            }
        }

        [Test]
        public void DataTableChoppingFiles()
        {
            const string source = "timestamp,TestString,SetComment,PropertyString,IntField,IntProperty\r\n" +
                "2012-05-01,test1,\"Hi there, I said!\",Bob,57,0\r\n" +
                "2011-04-01,test2,\"What's up, buttercup?\",Ralph,1,-999\r\n" +
                "1975-06-03,test3,\"Bye and bye, dragonfly!\",Jimmy's The Bomb,12,13";
            var dt = CSVDataTable.FromString(source);
            Assert.AreEqual("2012-05-01", dt.Rows[0].ItemArray[0]);
            Assert.AreEqual("2011-04-01", dt.Rows[1].ItemArray[0]);
            Assert.AreEqual("1975-06-03", dt.Rows[2].ItemArray[0]);
            Assert.AreEqual(3, dt.Rows.Count);

            // Save this string to a test file
            var fileGroupName = Guid.NewGuid().ToString();
            var outfile = fileGroupName + ".csv";
            var dirname = Guid.NewGuid().ToString();
            try
            {
                CSVDataTable.WriteToFile(dt, outfile);

                // Create an empty test folder
                Directory.CreateDirectory(dirname);

                // Chop this file into one-line chunks
                CSVReader.ChopFile(outfile, dirname, 1);

                // Verify that we got three files
                var files = Directory.GetFiles(dirname).ToList();
                files.Sort();
                Assert.IsTrue(files[0].EndsWith("1.csv"));
                Assert.IsTrue(files[1].EndsWith("2.csv"));
                Assert.IsTrue(files[2].EndsWith("3.csv"));
                Assert.AreEqual(3, files.Count);

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
            }
            finally
            {
                if (Directory.Exists(dirname))
                {
                    Directory.Delete(dirname, true);
                }
                if (File.Exists(outfile))
                {
                    File.Delete(outfile);
                }
            }
        }
    }
}

/*
 * 2006 - 2018 Ted Spence, http://tedspence.com
 * License: http://www.apache.org/licenses/LICENSE-2.0 
 * Home page: https://github.com/tspence/csharp-csv-reader
 */
using System;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using CSVFile;

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
            List<SimpleChopClass> list = new List<SimpleChopClass>();
            for (int i = 0; i < 5000; i++)
            {
                list.Add(new SimpleChopClass()
                {
                    id = i,
                    name = $"Bob Smith #{i}",
                    email = $"bob.smith.{i}@example.com"
                });
            }
            for (int i = 5000; i < 10000; i++)
            {
                list.Add(new SimpleChopClass()
                {
                    id = i,
                    name = $"Alice Jones #{i}",
                    email = $"alice.jones.{i}@example.com"
                });
            }
            for (int i = 10000; i < 15000; i++)
            {
                list.Add(new SimpleChopClass()
                {
                    id = i,
                    name = $"Charlie Jenkins #{i}",
                    email = $"charlie.jenkins.{i}@example.com"
                });
            }

            // Save this string to a test file
            string singlefile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
            File.WriteAllText(singlefile, CSV.Serialize<SimpleChopClass>(list));

            // Create an empty test folder
            string dirname = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirname);

            // Chop this file into one-line chunks
            CSVReader.ChopFile(singlefile, dirname, 5000);

            // Verify that we got three files
            string[] files = Directory.GetFiles(dirname);
            Assert.AreEqual(3, files.Length);

            // Read in each file and verify that each one has one line
            var f1 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[0]));
            Assert.AreEqual(5000, f1.Count);

            var f2 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[1]));
            Assert.AreEqual(5000, f2.Count);

            var f3 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[2]));
            Assert.AreEqual(5000, f3.Count);

            // Merge and verify
            var results = new List<SimpleChopClass>();
            results.AddRange(f1);
            results.AddRange(f2);
            results.AddRange(f3);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i].id, results[i].id);
                Assert.AreEqual(list[i].name, results[i].name);
                Assert.AreEqual(list[i].email, results[i].email);
            }

            // Clean up
            Directory.Delete(dirname, true);
            File.Delete(singlefile);
        }
        /*
        [Test]
        public void DataTableChoppingFiles()
        {
            string source = @"timestamp,TestString,SetComment,PropertyString,IntField,IntProperty
2012-05-01,test1,""Hi there, I said!"",Bob,57,0
2011-04-01,test2,""What's up, buttercup?"",Ralph,1,-999
1975-06-03,test3,""Bye and bye, dragonfly!"",Jimmy's The Bomb,12,13";
            DataTable dt = CSVDataTable.FromString(source);

            // Save this string to a test file
            string test_rootfn = Guid.NewGuid().ToString();
            string outfile = test_rootfn + ".csv";
            CSVDataTable.WriteToFile(dt, outfile);

            // Create an empty test folder
            string dirname = Guid.NewGuid().ToString();
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
        }*/
    }
}

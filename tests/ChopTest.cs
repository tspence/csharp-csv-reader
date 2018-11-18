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
#if HAS_ASYNC
using System.Threading.Tasks;
#endif

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
#if HAS_ASYNC
        public async Task DataTableChopTest()
#else
        public void DataTableChopTest()
#endif
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
#if HAS_ASYNC
            var num = await CSV.ChopFile(singlefile, dirname, 5000);
            string[] files = Directory.GetFiles(dirname);
            Assert.AreEqual(3, files.Length);
            Assert.AreEqual(3, num);
            var f1 = await CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[0]));
            var f2 = await CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[1]));
            var f3 = await CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[2]));
#else
            var num = CSV.ChopFile(singlefile, dirname, 5000);
            string[] files = Directory.GetFiles(dirname);
            Assert.AreEqual(3, files.Length);
            Assert.AreEqual(3, num);
            var f1 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[0]));
            var f2 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[1]));
            var f3 = CSV.Deserialize<SimpleChopClass>(File.ReadAllText(files[2]));
#endif

            // Verify that we got three files
            Assert.AreEqual(5000, f1.Count);
            Assert.AreEqual(5000, f2.Count);
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
    }
}

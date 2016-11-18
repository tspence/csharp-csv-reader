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
    public class WriterTest
    {
        [Test]
        public void TestForceQualifiers()
        {
            string[] array = new string[] { "one", "two", "three", "four, five" };
            string s = CSV.Output(array);
            Assert.AreEqual(s, "one,two,three,\"four, five\"");
            s = CSV.Output(array, '|', '\'', true);
            Assert.AreEqual(s, "'one'|'two'|'three'|'four, five'");
        }
    }
}

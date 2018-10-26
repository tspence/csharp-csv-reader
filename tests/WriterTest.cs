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

namespace CSVTestSuite
{
    [TestFixture]
    public class WriterTest
    {
        [Test]
        public void TestForceQualifiers()
        {
            string[] array = new string[] { "one", "two", "three", "four, five" };
            string s = array.ToCSVString();
            Assert.AreEqual(s, "one,two,three,\"four, five\"");

            // Now construct new settings
            var settings = new CSVSettings()
            {
                FieldDelimiter = '|',
                TextQualifier = '\'',
                ForceQualifiers = true
            };
            s = array.ToCSVString(settings);
            Assert.AreEqual(s, "'one'|'two'|'three'|'four, five'");
        }
    }
}

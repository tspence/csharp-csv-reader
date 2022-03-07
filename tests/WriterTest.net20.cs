﻿/*
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

        public class ExampleCsvType
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
        
        [Test]
        public void TestStringBuilder()
        {
            // Check if we can get string headers properly
            var sb = new StringBuilder();
            sb.AppendCSVHeader<ExampleCsvType>();
            Assert.AreEqual($"ID,Name,Latitude,Longitude{Environment.NewLine}", sb.ToString());
            
            // Check if we can serialize a class line by line
            sb.AppendCSVLine(new ExampleCsvType() { ID = 1, Name = "Smith, Alice", Latitude = 57.2, Longitude = 31.4 });
            Assert.AreEqual($"ID,Name,Latitude,Longitude{Environment.NewLine}1,\"Smith, Alice\",57.2,31.4{Environment.NewLine}", sb.ToString());
            sb.AppendCSVLine(new ExampleCsvType() { ID = 2, Name = "Palmer, Robert", Latitude = 57.3, Longitude = 31.5 });
            Assert.AreEqual($"ID,Name,Latitude,Longitude{Environment.NewLine}1,\"Smith, Alice\",57.2,31.4{Environment.NewLine}2,\"Palmer, Robert\",57.3,31.5{Environment.NewLine}", sb.ToString());
            sb.AppendCSVLine(new ExampleCsvType() { ID = 3, Name = "Jameson, Charlie", Latitude = 57.4, Longitude = 31.6 });
            Assert.AreEqual($"ID,Name,Latitude,Longitude{Environment.NewLine}1,\"Smith, Alice\",57.2,31.4{Environment.NewLine}2,\"Palmer, Robert\",57.3,31.5{Environment.NewLine}3,\"Jameson, Charlie\",57.4,31.6{Environment.NewLine}", sb.ToString());
        }
    }
}
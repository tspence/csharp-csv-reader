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
using System.IO;

namespace CSVTestSuite
{
    [TestFixture]
    public class SerializationTest
    {
#if !PORTABLE
        public class TestClassOne
        {
            public string TestString;
            public string PropertyString { get; set; }
            public string Comment;
            public void SetComment(string s) { Comment = s;  }
            public int IntField;
            public int IntProperty { get; set; }
            public DateTime timestamp;
        }

        public enum EnumTestType { First = 1, Second = 2, Third = 3 };

        public class TestClassTwo
        {
            public string FirstColumn;
            public int SecondColumn;
            public EnumTestType ThirdColumn;
        }

        [Test]
        public void TestObjectSerialization()
        {
            // Deserialize an array to a list of objects!
            List<TestClassOne> list = null;
            string source = @"timestamp,TestString,SetComment,PropertyString,IntField,IntProperty
2012-05-01,test1,""Hi there, I said!"",Bob,57,0
2011-04-01,test2,""What's up, buttercup?"",Ralph,1,-999
1975-06-03,test3,""Bye and bye, dragonfly!"",Jimmy's The Bomb,12,13";
            byte[] byteArray = Encoding.ASCII.GetBytes(source);
            MemoryStream stream = new MemoryStream(byteArray);
            using (CSVReader cr = new CSVReader(new StreamReader(stream))) {
                list = cr.Deserialize<TestClassOne>();
            }

            // Test the array
            Assert.AreEqual(list.Count, 3);
            Assert.AreEqual(list[0].TestString, "test1");
            Assert.AreEqual(list[0].Comment, "Hi there, I said!");
            Assert.AreEqual(list[0].PropertyString, "Bob");
            Assert.AreEqual(list[0].IntField, 57);
            Assert.AreEqual(list[0].IntProperty, 0);
            Assert.AreEqual(list[0].timestamp, new DateTime(2012,5,1));
            Assert.AreEqual(list[1].TestString, "test2");
            Assert.AreEqual(list[1].Comment, "What's up, buttercup?");
            Assert.AreEqual(list[1].PropertyString, "Ralph");
            Assert.AreEqual(list[1].IntField, 1);
            Assert.AreEqual(list[1].IntProperty, -999);
            Assert.AreEqual(list[1].timestamp, new DateTime(2011, 4, 1));
            Assert.AreEqual(list[2].TestString, "test3");
            Assert.AreEqual(list[2].Comment, "Bye and bye, dragonfly!");
            Assert.AreEqual(list[2].PropertyString, "Jimmy's The Bomb");
            Assert.AreEqual(list[2].IntField, 12);
            Assert.AreEqual(list[2].IntProperty, 13);
            Assert.AreEqual(list[2].timestamp, new DateTime(1975, 6, 3));
        }

        [Test]
        public void TestStructSerialization()
        {
            List<TestClassTwo> list = new List<TestClassTwo>();
            list.Add(new TestClassTwo() { FirstColumn = "hi1!", SecondColumn = 12, ThirdColumn = EnumTestType.First });
            list.Add(new TestClassTwo() { FirstColumn = "hi2, hi2, hi2!", SecondColumn = 34, ThirdColumn = EnumTestType.Second });
            list.Add(new TestClassTwo() { FirstColumn = @"hi3 says, ""Hi Three!""", SecondColumn = 56, ThirdColumn = EnumTestType.Third });

            // Serialize to a CSV string
            string csv = CSV.WriteToString(list, true);

            // Deserialize back from a CSV string - should not throw any errors!
            byte[] byteArray = Encoding.ASCII.GetBytes(csv);
            MemoryStream stream = new MemoryStream(byteArray);
            List<TestClassTwo> newlist = CSV.LoadArray<TestClassTwo>(new StreamReader(stream), false, false, false);

            // Compare original objects to new ones
            for (int i = 0; i < list.Count; i++) {
                Assert.AreEqual(list[i].FirstColumn, newlist[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newlist[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newlist[i].ThirdColumn);
            }
        }
#endif
    }
}

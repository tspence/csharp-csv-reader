using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSVFile;
using System.IO;

namespace CSVTestSuite
{
    [TestClass]
    public class SerializationTest
    {
        public class SerializeTest
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

        public class SerializeStruct
        {
            public string FirstColumn;
            public int SecondColumn;
            public EnumTestType ThirdColumn;
        }

        [TestMethod]
        public void TestObjectSerialization()
        {
            // Deserialize an array to a list of objects!
            List<SerializeTest> list = null;
            string source = @"timestamp,TestString,SetComment,PropertyString,IntField,IntProperty
2012-05-01,test1,""Hi there, I said!"",Bob,57,0
2011-04-01,test2,""What's up, buttercup?"",Ralph,1,-999
1975-06-03,test3,""Bye and bye, dragonfly!"",Jimmy's The Bomb,12,13";
            byte[] byteArray = Encoding.ASCII.GetBytes(source);
            MemoryStream stream = new MemoryStream(byteArray);
            using (CSVReader cr = new CSVReader(new StreamReader(stream))) {
                list = cr.Deserialize<SerializeTest>();
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

        [TestMethod]
        public void TestStructSerialization()
        {
            List<SerializeStruct> list = new List<SerializeStruct>();
            list.Add(new SerializeStruct() { FirstColumn = "hi1!", SecondColumn = 12, ThirdColumn = EnumTestType.First });
            list.Add(new SerializeStruct() { FirstColumn = "hi2, hi2, hi2!", SecondColumn = 34, ThirdColumn = EnumTestType.Second });
            list.Add(new SerializeStruct() { FirstColumn = @"hi3 says, ""Hi Three!""", SecondColumn = 56, ThirdColumn = EnumTestType.Third });

            // Serialize to a CSV string
            string csv = list.WriteToString(true);

            // Deserialize back from a CSV string - should not throw any errors!
            byte[] byteArray = Encoding.ASCII.GetBytes(csv);
            MemoryStream stream = new MemoryStream(byteArray);
            List<SerializeStruct> newlist = CSV.LoadArray<SerializeStruct>(new StreamReader(stream), false, false, false);

            // Compare original objects to new ones
            for (int i = 0; i < list.Count; i++) {
                Assert.AreEqual(list[i].FirstColumn, newlist[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newlist[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newlist[i].ThirdColumn);
            }
        }
    }
}

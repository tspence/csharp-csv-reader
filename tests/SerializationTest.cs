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
using System.Linq;
using System.Threading.Tasks;
// ReSharper disable ConvertToConstant.Local

namespace CSVTestSuite
{
    [TestFixture]
    public class SerializationTest
    {
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

        public enum EnumTestType { First = 1, Second = 2, Third = 3, Fourth = 4 };

        public class TestClassTwo
        {
            public string FirstColumn;
            public int SecondColumn;
            public EnumTestType? ThirdColumn;
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
            byte[] byteArray = Encoding.UTF8.GetBytes(source);
            MemoryStream stream = new MemoryStream(byteArray);
            using (CSVReader cr = new CSVReader(new StreamReader(stream))) {
                list = cr.Deserialize<TestClassOne>().ToList();
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
            string csv = CSV.Serialize<TestClassTwo>(list);

            // Deserialize back from a CSV string - should not throw any errors!
            List<TestClassTwo> newlist = CSV.Deserialize<TestClassTwo>(csv).ToList();

            // Compare original objects to new ones
            for (int i = 0; i < list.Count; i++) {
                Assert.AreEqual(list[i].FirstColumn, newlist[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newlist[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newlist[i].ThirdColumn);
            }
        }

        [Test]
        public void TestNullSerialization()
        {
            List<TestClassTwo> list = new List<TestClassTwo>();
            list.Add(new TestClassTwo() { FirstColumn = "hi1!", SecondColumn = 12, ThirdColumn = EnumTestType.First });
            list.Add(new TestClassTwo() { FirstColumn = "hi2, hi2, hi2!", SecondColumn = 34, ThirdColumn = EnumTestType.Second });
            list.Add(new TestClassTwo() { FirstColumn = @"hi3 says, ""Hi Three!""", SecondColumn = 56, ThirdColumn = EnumTestType.Third });
            list.Add(new TestClassTwo() { FirstColumn = null, SecondColumn = 7, ThirdColumn = EnumTestType.Fourth });

            // Serialize to a CSV string
            string csv = CSV.Serialize<TestClassTwo>(list, CSVSettings.CSV_PERMIT_NULL);

            // Deserialize back from a CSV string - should not throw any errors!
            List<TestClassTwo> newlist = CSV.Deserialize<TestClassTwo>(csv, CSVSettings.CSV_PERMIT_NULL).ToList();

            // Compare original objects to new ones
            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i].FirstColumn, newlist[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newlist[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newlist[i].ThirdColumn);
            }
        }

        [Test]
        public void TestCaseInsensitiveDeserializer()
        {
            List<TestClassTwo> list = new List<TestClassTwo>();
            list.Add(new TestClassTwo() { FirstColumn = "hi1!", SecondColumn = 12, ThirdColumn = EnumTestType.First });
            list.Add(new TestClassTwo() { FirstColumn = "hi2, hi2, hi2!", SecondColumn = 34, ThirdColumn = EnumTestType.Second });
            list.Add(new TestClassTwo() { FirstColumn = @"hi3 says, ""Hi Three!""", SecondColumn = 56, ThirdColumn = EnumTestType.Third });
            list.Add(new TestClassTwo() { FirstColumn = null, SecondColumn = 7, ThirdColumn = EnumTestType.Fourth });

            string csv = "FIRSTCOLUMN,SECONDCOLUMN,THIRDCOLUMN\n" +
                "hi1!,12,First\n" +
                "\"hi2, hi2, hi2!\",34,Second\n" +
                "\"hi3 says, \"\"Hi Three!\"\"\",56,Third\n" +
                "NULL,7,Fourth";

            // Deserialize back from a CSV string - should not throw any errors!
            List<TestClassTwo> newlist = CSV.Deserialize<TestClassTwo>(csv, CSVSettings.CSV_PERMIT_NULL).ToList();

            // Compare original objects to new ones
            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i].FirstColumn, newlist[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newlist[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newlist[i].ThirdColumn);
            }
        }
        
        [Test]
        public void TestNullDeserialization()
        {
            var csv = "FIRSTCOLUMN,SECONDCOLUMN,THIRDCOLUMN\n" +
                         ",,\n" +
                         "\"hi2, hi2, hi2!\",34,Second\n" +
                         "\"hi3 says, \"\"Hi Three!\"\"\",56,Third\n" +
                         "NULL,7,Fourth";
            var settings = new CSVSettings()
            {
                AllowNull = true,
                NullToken = string.Empty,
            };

            // Deserialize back from a CSV string - should not throw any errors!
            var list = CSV.Deserialize<TestClassTwo>(csv, settings).ToList();
            Assert.AreEqual(null, list[0].FirstColumn);
            Assert.AreEqual(0, list[0].SecondColumn);
            Assert.AreEqual(null, list[0].ThirdColumn);
        }
        
        [Test]
        public void TestNullDeserializationWithToken()
        {
            var csv = "FIRSTCOLUMN,SECONDCOLUMN,THIRDCOLUMN\n" +
                      "SPECIAL_NULL,SPECIAL_NULL,SPECIAL_NULL\n" +
                      "\"hi2, hi2, hi2!\",34,Second\n" +
                      "\"hi3 says, \"\"Hi Three!\"\"\",56,Third\n" +
                      "NULL,7,Fourth\n" +
                      ",8,Second";
            var settings = new CSVSettings()
            {
                AllowNull = true,
                NullToken = "SPECIAL_NULL",
            };

            // Deserialize back from a CSV string - should not throw any errors!
            var list = CSV.Deserialize<TestClassTwo>(csv, settings).ToList();
            Assert.AreEqual(null, list[0].FirstColumn);
            Assert.AreEqual(0, list[0].SecondColumn);
            Assert.AreEqual(null, list[0].ThirdColumn);
            
            // Did the regular text "NULL" still come through?
            Assert.AreEqual("NULL", list[3].FirstColumn);
            
            // Did an empty field get imported as an empty string?
            Assert.AreEqual("", list[4].FirstColumn);
        }

        public class TestClassLastColumnNullableSingle
        {
            public string TestString;
            public int IntProperty { get; set; }
            public float? NullableSingle { get; set; }
        }
        
        [Test]
        public void DeserializeLastColumnNullableSingle()
        {
            var csv = "TestString,IntProperty,NullableSingle\n" +
                      "Test String,57,12.35\n" +
                      "Test String,57,\n" +
                      "Test String,57,56.19\n" + 
                      "Test String,57,\n" +
                      "\n";
            
            // Let's specifically allow null
            var settings = new CSVSettings();
            settings.AllowNull = true;
            settings.NullToken = string.Empty;
            settings.IgnoreEmptyLineForDeserialization = true;

            // Deserialize back from a CSV string - should not throw any errors!
            var list = CSV.Deserialize<TestClassLastColumnNullableSingle>(csv, settings).ToList();
            Assert.AreEqual(4, list.Count);

            Assert.AreEqual("Test String", list[0].TestString);
            Assert.AreEqual(57, list[0].IntProperty);
            Assert.AreEqual(12.35f, list[0].NullableSingle);
            
            Assert.AreEqual("Test String", list[1].TestString);
            Assert.AreEqual(57, list[1].IntProperty);
            Assert.IsNull(list[1].NullableSingle);
            
            Assert.AreEqual("Test String", list[2].TestString);
            Assert.AreEqual(57, list[2].IntProperty);
            Assert.AreEqual(56.19f, list[2].NullableSingle);

            Assert.AreEqual("Test String", list[3].TestString);
            Assert.AreEqual(57, list[3].IntProperty);
            Assert.IsNull(list[3].NullableSingle);
        }
        
#if HAS_ASYNC_IENUM
        [Test]
        public async Task DeserializeLastColumnNullableSingleAsync()
        {
            var csv = "TestString,IntProperty,NullableSingle\n" +
                      "Test String,57,12.35\n" +
                      "Test String,57,\n" +
                      "Test String,57,56.19\n" + 
                      "Test String,57,\n" +
                      "\n";
            
            // Let's specifically allow null
            var settings = new CSVSettings();
            settings.AllowNull = true;
            settings.NullToken = string.Empty;
            settings.IgnoreEmptyLineForDeserialization = true;

            // Try deserializing using the async version
            var list = await CSV.DeserializeAsync<TestClassLastColumnNullableSingle>(csv, settings).ToListAsync();
            Assert.AreEqual(4, list.Count);

            Assert.AreEqual("Test String", list[0].TestString);
            Assert.AreEqual(57, list[0].IntProperty);
            Assert.AreEqual(12.35f, list[0].NullableSingle);
            
            Assert.AreEqual("Test String", list[1].TestString);
            Assert.AreEqual(57, list[1].IntProperty);
            Assert.IsNull(list[1].NullableSingle);
            
            Assert.AreEqual("Test String", list[2].TestString);
            Assert.AreEqual(57, list[2].IntProperty);
            Assert.AreEqual(56.19f, list[2].NullableSingle);

            Assert.AreEqual("Test String", list[3].TestString);
            Assert.AreEqual(57, list[3].IntProperty);
            Assert.IsNull(list[3].NullableSingle);
        }
#endif
        
        public class TestClassHasReadOnlyProperty
        {
            public string TestString;
            public DateTime DateProperty { get; set; }
            public float? ReadOnlySingle { get; }
        }

        [Test]
        public void DeserializeReadOnlyProperty()
        {
            var csv = "TestString,DateProperty,ReadOnlySingle\n" +
                      "Test String,2020-01-01,12.35\n" +
                      "Test String,2001-03-18,\n" +
                      "Test String,1997-05-04,56.19\n" +
                      "Test String,1993-06-03,\n" +
                      "\n";

            // Let's specifically allow null
            var settings = new CSVSettings
            {
                AllowNull = true,
                NullToken = string.Empty,
                IgnoreEmptyLineForDeserialization = true
            };

            // Deserialize back from a CSV string - should throw an error because "ReadOnlySingle" is read only
            var ex = Assert.Throws<Exception>(() =>
            {
                var testClassHasReadOnlyProperties = CSV.Deserialize<TestClassHasReadOnlyProperty>(csv, settings).ToList();
            });
            Assert.AreEqual("The column header 'ReadOnlySingle' matches a read-only property. To ignore this exception, enable IgnoreReadOnlyProperties and IgnoreHeaderErrors.", ex.Message);
            Assert.AreEqual(typeof(Exception), ex.GetType());
            
            // Now do the same thing while ignoring read-only properties
            settings.IgnoreReadOnlyProperties = true;
            settings.IgnoreHeaderErrors = true;
            var list = CSV.Deserialize<TestClassHasReadOnlyProperty>(csv, settings).ToList();
            Assert.AreEqual(4, list.Count);

            Assert.AreEqual("Test String", list[0].TestString);
            Assert.AreEqual(new DateTime(2020, 1, 1), list[0].DateProperty);
            Assert.IsNull(list[0].ReadOnlySingle);

            Assert.AreEqual("Test String", list[1].TestString);
            Assert.AreEqual(new DateTime(2001, 3, 18), list[1].DateProperty);
            Assert.IsNull(list[1].ReadOnlySingle);

            Assert.AreEqual("Test String", list[2].TestString);
            Assert.AreEqual(new DateTime(1997, 5, 4), list[2].DateProperty);
            Assert.IsNull(list[2].ReadOnlySingle);

            Assert.AreEqual("Test String", list[3].TestString);
            Assert.AreEqual(new DateTime(1993, 6, 3), list[3].DateProperty);
            Assert.IsNull(list[3].ReadOnlySingle);
                        
            // Return this array back to a string
            settings.LineSeparator = "\n";
            var backToCsv = CSV.Serialize(list, settings);
            Assert.AreEqual("TestString,DateProperty,ReadOnlySingle\n" +
                            "Test String,2020-01-01T00:00:00.0000000,\n" +
                            "Test String,2001-03-18T00:00:00.0000000,\n" +
                            "Test String,1997-05-04T00:00:00.0000000,\n" +
                            "Test String,1993-06-03T00:00:00.0000000,\n", backToCsv);
                        
            // Try forcing all dates to be text-qualified
            settings.LineSeparator = "\n";
            settings.DateTimeFormat = "yyyy-MM-dd";
            settings.ForceQualifierTypes = new []{ typeof(DateTime) };
            backToCsv = CSV.Serialize(list, settings);
            Assert.AreEqual("TestString,DateProperty,ReadOnlySingle\n" +
                            "Test String,\"2020-01-01\",\n" +
                            "Test String,\"2001-03-18\",\n" +
                            "Test String,\"1997-05-04\",\n" +
                            "Test String,\"1993-06-03\",\n", backToCsv);
        }

        [Test]
        public void DeserializeExcludeColumns()
        {
            var csv = "TestString,IntProperty,NullableSingle\n" +
                      "Test String,57,12.35\n" +
                      "Test String,57,\n" +
                      "Test String,57,56.19\n" + 
                      "Test String,57,\n" +
                      "\n";
            
            // Let's specifically allow null
            var settings = new CSVSettings
            {
                AllowNull = true,
                NullToken = string.Empty,
                IgnoreEmptyLineForDeserialization = true,
                ExcludedColumns = new []{ "TestString" }
            };

            // Try deserializing - we should see nulls in the TestString column
            var list = CSV.Deserialize<TestClassLastColumnNullableSingle>(csv, settings).ToList();
            Assert.AreEqual(4, list.Count);

            Assert.IsNull(list[0].TestString);
            Assert.AreEqual(57, list[0].IntProperty);
            Assert.AreEqual(12.35f, list[0].NullableSingle);
            
            Assert.IsNull(list[1].TestString);
            Assert.AreEqual(57, list[1].IntProperty);
            Assert.IsNull(list[1].NullableSingle);
            
            Assert.IsNull(list[2].TestString);
            Assert.AreEqual(57, list[2].IntProperty);
            Assert.AreEqual(56.19f, list[2].NullableSingle);

            Assert.IsNull(list[3].TestString);
            Assert.AreEqual(57, list[3].IntProperty);
            Assert.IsNull(list[3].NullableSingle);
            
            // Try serializing - we should no longer see a TestString column
            settings.LineSeparator = "\n";
            var backToCsv = CSV.Serialize(list, settings);
            Assert.AreEqual("IntProperty,NullableSingle\n" +
                            "57,12.35\n" +
                            "57,\n" +
                            "57,56.19\n" + 
                            "57,\n", backToCsv);
        }
    }
}

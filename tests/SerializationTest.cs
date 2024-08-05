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
#if HAS_ASYNC
using System.Threading.Tasks;
#endif

// ReSharper disable ConvertToConstant.Local
// ReSharper disable StringLiteralTypo
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global

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

        public class TestClassThree
        {
            public string Name { get; set; }
            public string[] StringArray { get; set; }
            public List<int> IntList { get; set; }
            public IEnumerable<bool> BoolEnumerable { get; set; }
            public List<Guid> GuidList { get; set; }
            public List<Guid> NullableList { get; set; }
        }

        [Test]
        public void TestObjectSerialization()
        {
            // Deserialize an array to a list of objects!
            var source = @"timestamp,TestString,SetComment,PropertyString,IntField,IntProperty
2012-05-01,test1,""Hi there, I said!"",Bob,57,0
2011-04-01,test2,""What's up, buttercup?"",Ralph,1,-999
1975-06-03,test3,""Bye and bye, dragonfly!"",Jimmy's The Bomb,12,13";
            var byteArray = Encoding.UTF8.GetBytes(source);
            var stream = new MemoryStream(byteArray);
            using (var cr = new CSVReader(new StreamReader(stream))) {
                var list = cr.Deserialize<TestClassOne>().ToList();
                
                Assert.AreEqual(3, list.Count);
                Assert.AreEqual("test1", list[0].TestString);
                Assert.AreEqual("Hi there, I said!", list[0].Comment);
                Assert.AreEqual("Bob", list[0].PropertyString);
                Assert.AreEqual(57, list[0].IntField);
                Assert.AreEqual(0, list[0].IntProperty);
                Assert.AreEqual(new DateTime(2012,5,1), list[0].timestamp);
                Assert.AreEqual("test2", list[1].TestString);
                Assert.AreEqual("What's up, buttercup?", list[1].Comment);
                Assert.AreEqual("Ralph", list[1].PropertyString);
                Assert.AreEqual(1, list[1].IntField);
                Assert.AreEqual(-999, list[1].IntProperty);
                Assert.AreEqual(new DateTime(2011, 4, 1), list[1].timestamp);
                Assert.AreEqual("test3", list[2].TestString);
                Assert.AreEqual("Bye and bye, dragonfly!", list[2].Comment);
                Assert.AreEqual("Jimmy's The Bomb", list[2].PropertyString);
                Assert.AreEqual(12, list[2].IntField);
                Assert.AreEqual(13, list[2].IntProperty);
                Assert.AreEqual(new DateTime(1975, 6, 3), list[2].timestamp);
            }
        }

        [Test]
        public void TestStructSerialization()
        {
            var list = new List<TestClassTwo>();
            list.Add(new TestClassTwo() { FirstColumn = "hi1!", SecondColumn = 12, ThirdColumn = EnumTestType.First });
            list.Add(new TestClassTwo() { FirstColumn = "hi2, hi2, hi2!", SecondColumn = 34, ThirdColumn = EnumTestType.Second });
            list.Add(new TestClassTwo() { FirstColumn = @"hi3 says, ""Hi Three!""", SecondColumn = 56, ThirdColumn = EnumTestType.Third });

            // Serialize to a CSV string
            var csv = CSV.Serialize(list);

            // Deserialize back from a CSV string - should not throw any errors!
            var newList = CSV.Deserialize<TestClassTwo>(csv).ToList();

            // Compare original objects to new ones
            for (var i = 0; i < list.Count; i++) {
                Assert.AreEqual(list[i].FirstColumn, newList[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newList[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newList[i].ThirdColumn);
            }
        }

        [Test]
        public void TestNullSerialization()
        {
            var list = new List<TestClassTwo>();
            list.Add(new TestClassTwo() { FirstColumn = "hi1!", SecondColumn = 12, ThirdColumn = EnumTestType.First });
            list.Add(new TestClassTwo() { FirstColumn = "hi2, hi2, hi2!", SecondColumn = 34, ThirdColumn = EnumTestType.Second });
            list.Add(new TestClassTwo() { FirstColumn = @"hi3 says, ""Hi Three!""", SecondColumn = 56, ThirdColumn = EnumTestType.Third });
            list.Add(new TestClassTwo() { FirstColumn = null, SecondColumn = 7, ThirdColumn = EnumTestType.Fourth });

            // Serialize to a CSV string
            var csv = CSV.Serialize(list, CSVSettings.CSV_PERMIT_NULL);

            // Deserialize back from a CSV string - should not throw any errors!
            var newList = CSV.Deserialize<TestClassTwo>(csv, CSVSettings.CSV_PERMIT_NULL).ToList();

            // Compare original objects to new ones
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i].FirstColumn, newList[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newList[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newList[i].ThirdColumn);
            }
        }

        /// <summary>
        /// Arrays and child objects aren't well suited for complex serialization within a CSV file.
        /// However, we have options:
        /// * ToString just converts it to "MyClass[]"
        /// * CountItems just produces the number of elements in the array
        /// </summary>
        [Test]
        public void TestArraySerialization()
        {
            var list = new List<TestClassThree>();
            list.Add(new TestClassThree()
            {
                Name = "Test",
                StringArray = new [] { "a", "b", "c"},
                IntList = new List<int> { 1, 2, 3 },
                BoolEnumerable = new [] { true, false, true, false },
                GuidList = new List<Guid>(),
            });

            // Serialize to a CSV string using ToString
            // This was the default behavior in CSVFile 3.1.2 and earlier - it's pretty ugly!
            var options = new CSVSettings()
            {
                HeaderRowIncluded = true,
                NestedArrayBehavior = ArrayOptions.ToString,
                NullToken = "NULL",
                AllowNull = true,
            };
            var toStringCsv = CSV.Serialize(list, options);
            Assert.AreEqual($"Name,StringArray,IntList,BoolEnumerable,GuidList,NullableList{Environment.NewLine}"
                + $"Test,System.String[],System.Collections.Generic.List`1[System.Int32],System.Boolean[],System.Collections.Generic.List`1[System.Guid],NULL{Environment.NewLine}", toStringCsv);

            // Serialize to a CSV string using counts
            options.NestedArrayBehavior = ArrayOptions.CountItems;
            var countItemsCsv = CSV.Serialize(list, options);
            Assert.AreEqual($"Name,StringArray,IntList,BoolEnumerable,GuidList,NullableList{Environment.NewLine}"
                + $"Test,3,3,4,0,NULL{Environment.NewLine}", countItemsCsv);

            // Serialize to a CSV string using counts
            options.NestedArrayBehavior = ArrayOptions.TreatAsNull;
            var ignoreArraysCsv = CSV.Serialize(list, options);
            Assert.AreEqual($"Name,StringArray,IntList,BoolEnumerable,GuidList,NullableList{Environment.NewLine}"
                + $"Test,NULL,NULL,NULL,NULL,NULL{Environment.NewLine}", ignoreArraysCsv);
        }
        
        [Test]
        public void TestCaseInsensitiveDeserializer()
        {
            var list = new List<TestClassTwo>();
            list.Add(new TestClassTwo() { FirstColumn = "hi1!", SecondColumn = 12, ThirdColumn = EnumTestType.First });
            list.Add(new TestClassTwo() { FirstColumn = "hi2, hi2, hi2!", SecondColumn = 34, ThirdColumn = EnumTestType.Second });
            list.Add(new TestClassTwo() { FirstColumn = @"hi3 says, ""Hi Three!""", SecondColumn = 56, ThirdColumn = EnumTestType.Third });
            list.Add(new TestClassTwo() { FirstColumn = null, SecondColumn = 7, ThirdColumn = EnumTestType.Fourth });

            var csv = "FIRSTCOLUMN,SECONDCOLUMN,THIRDCOLUMN\n" +
                "hi1!,12,First\n" +
                "\"hi2, hi2, hi2!\",34,Second\n" +
                "\"hi3 says, \"\"Hi Three!\"\"\",56,Third\n" +
                "NULL,7,Fourth";

            // Deserialize back from a CSV string - should not throw any errors!
            var permitNull = new CSVSettings() { AllowNull = true, LineSeparator = "\n", NullToken = "NULL" };
            var newList = CSV.Deserialize<TestClassTwo>(csv, permitNull).ToList();

            // Compare original objects to new ones
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i].FirstColumn, newList[i].FirstColumn);
                Assert.AreEqual(list[i].SecondColumn, newList[i].SecondColumn);
                Assert.AreEqual(list[i].ThirdColumn, newList[i].ThirdColumn);
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
                LineSeparator = "\n",
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
                LineSeparator = "\n",
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
            settings.LineSeparator = "\n";

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
            settings.LineSeparator = "\n";

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
                IgnoreEmptyLineForDeserialization = true,
                LineSeparator = "\n",
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
                ExcludedColumns = new []{ "TestString" },
                LineSeparator = "\n",
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

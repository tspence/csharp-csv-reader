[![NuGet](https://img.shields.io/nuget/v/CSVFile.svg?style=plastic)](https://www.nuget.org/packages/CSVFile/)
[![Travis-CI](https://api.travis-ci.org/tspence/csharp-csv-reader.svg?style=plastic&branch=master)](https://travis-ci.org/tspence/csharp-csv-reader/branches)

# csharp-csv-reader
This library is a series of unit tested, thoroughly commented CSV parsing functions which I have developed over the past eight or nine years. Extremely small and easy to implement; includes unit tests for the majority of odd CSV edge cases. Library supports different delimiters, qualifiers, and embedded newlines. Can read and write from data tables.

## Why use CSharp CSV Reader?
A few reasons:
* Full compatibility all the way back to C# 2.0 - easy to integrate into legacy projects.
* Between 16-32 kilobytes in size, depending on framework
* No dependencies
* Handles all the horrible edge cases from poorly written CSV generating software: custom delimiters, embedded newlines, and doubled-up text qualifiers
* Reads via streams; which means if you have a 16GB .csv.gz file it can be streamed into memory

# Tutorial
Want to get started? Here are a few walkthroughs.

## Custom CSV Settings
Do you have files that use the pipe symbol as a delimiter, or does your application need double quotes around all fields? No problem!

```
var settings = new CSVSettings()
{
    FieldDelimiter = '|',
    TextQualifier = '\'',
    ForceQualifiers = true
};
s = array.ToCSVString(settings);
```

## Iterate Through A Massive File
When you receive a gigantic 20GB file that is formatted CSV, you obviously can't parse it all into memory at once. Maybe you want to deserialize a ZIP file and the CSV within it at the same time - here's how you do that:

```
using (CSVReader cr = new CSVReader(myfilename)) {
    foreach (string[] line in cr) {
        // Do whatever you want with this one line - the buffer will
        // only hold a small amount of memory at once, so you can 
        // iterate at your own pace!
    }
}
```

## Serialize and Deserialize
You can serialize and deserialize between List<T> and CSV arrays.  Serialization supports all basic value types, and it can even optionally support storing null values in CSV cells.

```
var list = new List<MyClass>();

// Serialize an array of objects to a CSV string
string csv = CSV.Serialize<MyClass>(list);

// Deserialize a CSV back into an array of objects
var newlist = CSV.Deserialize<MyClass>(csv);
```

## Data Table Support (for older DotNet frameworks)
For those of you who work in older frameworks that still use DataTables, this feature is still available:

```
// This code assumes the file is on disk, and the first row of the file
// has the names of the columns on it
DataTable dt = CSV.LoadDataTable(myfilename);

// Save a datatable to a file
dt.SaveAsCSV(myfilename, true);
```

# Hand Roll Your Own
The class CSV contains a lot of useful functions for hand rolling your own CSV related code. You can use any of the functions in the `CSV` class directly.

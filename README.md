[![NuGet](https://img.shields.io/nuget/v/CSVFile.svg?style=plastic)](https://www.nuget.org/packages/CSVFile/)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/tspence/csharp-csv-reader/dotnet.yml?branch=main)](https://github.com/tspence/csharp-csv-reader/actions/workflows/dotnet.yml)
[![SonarCloud Coverage](https://sonarcloud.io/api/project_badges/measure?project=tspence_csharp-csv-reader&metric=coverage)](https://sonarcloud.io/summary/overall?id=tspence_csharp-csv-reader)
[![SonarCloud Bugs](https://sonarcloud.io/api/project_badges/measure?project=tspence_csharp-csv-reader&metric=bugs)](https://sonarcloud.io/summary/overall?id=tspence_csharp-csv-reader)
[![SonarCloud Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=tspence_csharp-csv-reader&metric=vulnerabilities)](https://sonarcloud.io/summary/overall?id=tspence_csharp-csv-reader)

# CSVFile
This library is a series of unit tested, thoroughly commented CSV parsing functions which I have developed off and on since 2006. Extremely small and easy to implement; includes unit tests for the majority of odd CSV edge cases. Library supports different delimiters, qualifiers, and embedded newlines. Can read and write from data tables.

## Why use CSVFile?
A few reasons:
* Compatible with DotNet Framework / C# 2.0 and later.  Makes it easy to integrate this library into extremely old legacy projects.
* Between 16-32 kilobytes in size, depending on framework.
* No dependencies.
* Handles all the horrible edge cases from poorly written CSV generating software: custom delimiters, embedded newlines, and doubled-up text qualifiers.
* Reads via streams, optionally using asynchronous I/O.  You can parse CSV files larger than you can hold in memory without thrashing.
* Ability to pipe data tables directly into SQL Server using [Table Parameter Inserts](https://www.gamedeveloper.com/programming/in-depth-sql-server---high-performance-inserts)
* Fastest with direct string parsing or async I/O, but [good enough performance when reading from MemoryStreams](https://www.joelverhagen.com/blog/2020/12/fastest-net-csv-parsers)

## CSV edge cases
This library was designed to handle edge cases I experienced when working with partner files.

| Case | Example |
|------|---------|
| CSV files larger than can fit into memory streamed off disk | 10TB files |
| Pipe delimited files | `field1\|field2` |
| Hand written CSV with spaces after deliminters | `"field1", "field2", "field3"` |
| Embedded newlines within a text qualifier | `"field1\r\nanother line","field2"` |
| Text qualifiers as regular characters within a field | `"field1",field2 "field2" field2,"field3"` |
| Doubled up text qualifiers within a qualified field | `"field1","field2 ""field2"" field2","field3"` |
| Different line separators | CR, LF, something else |
| Different text encoding | UTF-8, UTF-16, ASCII |
| [SEP= lines for European CSV files](https://superuser.com/questions/773644/what-is-the-sep-metadata-you-can-add-to-csvs) | `sep=;\\r\\n` |

# Tutorial
Want to get started? Here are a few walkthroughs.

## Custom CSV Settings
Do you have files that use the pipe symbol as a delimiter, or does your application need double quotes around all fields? No problem!

```csharp
var settings = new CSVSettings()
{
    FieldDelimiter = '|',
    TextQualifier = '\'',
    ForceQualifiers = true
};
s = array.ToCSVString(settings);
```

## Streaming asynchronous CSV 
The latest asynchronous I/O frameworks allow you to stream CSV data off disk without blocking.  Here's how to use the asynchronous I/O features of Dot Net 5.0:

```csharp
using (var cr = CSVReader.FromFile(filename, settings)) {
    await foreach (string[] line in cr) {
        // Do whatever you want with this one line - the buffer will
        // only hold a small amount of memory at once, so you can 
        // iterate at your own pace!
    }
}
```

## Streaming CSV without async

Don't worry if your project isn't yet able to use asynchronous foreach loops.  You can still use the existing reader logic:

```csharp
using (CSVReader cr = new CSVReader(sr, settings)) {
    foreach (string[] line in cr) {
        // Process this one line
    }
}
```

## Serialize and Deserialize
You can serialize and deserialize between List<T> and CSV arrays.  Serialization supports all basic value types, and it can even optionally support storing null values in CSV cells.

```csharp
var list = new List<MyClass>();

// Serialize an array of objects to a CSV string
string csv = CSV.Serialize<MyClass>(list);

// Deserialize a CSV back into an array of objects
foreach (var myObject in CSV.Deserialize<MyClass>(csv)) {
    // Use the objects
}
```

## Data Table Support (for older DotNet frameworks)
For those of you who work in older frameworks that still use DataTables, this feature is still available:

```csharp
// This code assumes the file is on disk, and the first row of the file
// has the names of the columns on it
DataTable dt = CSV.LoadDataTable(myfilename);

// Save a datatable to a file
dt.SaveAsCSV(myfilename, true);
```

# Hand Roll Your Own
The class CSV contains a lot of useful functions for hand rolling your own CSV related code. You can use any of the functions in the `CSV` class directly.

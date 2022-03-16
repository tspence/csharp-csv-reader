[![NuGet](https://img.shields.io/nuget/v/CSVFile.svg?style=plastic)](https://www.nuget.org/packages/CSVFile/)
![Travis (.com)](https://img.shields.io/travis/com/tspence/csharp-csv-reader)

# csharp-csv-reader
This library is a series of unit tested, thoroughly commented CSV parsing functions which I have developed off and on since 2006. Extremely small and easy to implement; includes unit tests for the majority of odd CSV edge cases. Library supports different delimiters, qualifiers, and embedded newlines. Can read and write from data tables.

## Why use CSharp CSV Reader?
A few reasons:
* Compatible with DotNet Framework / C# 2.0 and later.  Makes it easy to integrate this library into extremely old legacy projects.
* Between 16-32 kilobytes in size, depending on framework.
* No dependencies.
* Handles all the horrible edge cases from poorly written CSV generating software: custom delimiters, embedded newlines, and doubled-up text qualifiers.
* Reads via streams, optionally using asynchronous I/O.  You can parse CSV files larger than you can hold in memory without thrashing.

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
var newlist = await CSV.Deserialize<MyClass>(csv);
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

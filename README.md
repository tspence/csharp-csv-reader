# csharp-csv-reader
This library is a series of unit tested, thoroughly commented CSV parsing functions which I have developed over the past eight or nine years. Extremely small and easy to implement; includes unit tests for the majority of odd CSV edge cases. Library supports different delimiters, qualifiers, and embedded newlines. Can read and write from data tables.

# Tutorial
Want to get started? Here are a few walkthroughs.

# Read Into Data Table
The simplest way to get started is to read a file on disk into memory as a DataTable. Here's how you do it:

```
// This code assumes the file is on disk, and the first row of the file
// has the names of the columns on it
DataTable dt = CSV.LoadDataTable(myfilename);
```

# Iterate Through A Massive File
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

# Pipe Delimited Files
Have a funny delimiter? Are you getting weird tabs or other symbols? No problem!

```
CSVReader cr = new CSVReader(myfilename, 
    '|',  // This is the field delimiter in the file
    '`'); // This is the text qualifier - the symbol that wraps around fields that contain
          // delimiters or embedded newlines
```

# Output a Data Table
If you've got a datatable and want to put it on disk in a particular location, you can use the extension method to write it

```
DataTable dt;
dt.SaveAsCSV(myfilename, true);
```

# Serialize Objects
You can serialize and deserialize objects to gigantic CSV arrays, if you like:

```
List<TestClassTwo> list = new List<TestClassTwo>();
// Populate the list with values
string csv = CSV.SaveArray<TestClassTwo>(true);
```

When you want to retrieve data from the CSV file, you can reverse the process:

```
List<TestClassTwo> newlist = CSV.LoadArray<TestClassTwo>(new StreamReader(stream), false, false, false);
```

# Hand Roll Your Own
The class CSV contains a lot of useful functions for hand rolling your own CSV related code. Pay special attention to TryParseLine() - it's the core of the project.

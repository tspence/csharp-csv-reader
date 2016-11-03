@ECHO OFF
ECHO ********** Building all "older .net" variants
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net20.csproj /property:Config=Debug+Release
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net35.csproj /property:Config=Debug+Release
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net40.csproj /property:Config=Debug+Release

ECHO ********** Building "dotnetcore" variants
pushd .
cd src
dotnet.exe build csharp-csv-reader.portable.csproj -c Debug
dotnet.exe build csharp-csv-reader.portable.csproj -c Release
popd

ECHO ********** Done

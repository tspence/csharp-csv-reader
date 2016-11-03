@ECHO OFF
ECHO ********** Building all "older .net" variants
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net20.csproj /property:Configuration=Debug
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net20.csproj /property:Configuration=Release
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net35.csproj /property:Configuration=Debug
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net35.csproj /property:Configuration=Release
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net40.csproj /property:Configuration=Debug
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\csharp-csv-reader.net40.csproj /property:Configuration=Release

ECHO ********** Building "dotnetcore" variants
pushd .
cd src
dotnet restore
dotnet.exe build -c debug
dotnet.exe build -c release
popd

ECHO ********** Done

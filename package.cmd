@echo off
msbuild csharp-csv-reader.sln /p:configuration=release
..\nuget pack csvfile.nuspec
move *.nupkg ..\nuget-temp-folder

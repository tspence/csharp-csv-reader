@echo off
pushd .
cd src
..\..\nuget pack csvfile.nuspec
popd

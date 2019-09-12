@echo off
pushd .
cd %~dp0%

dotnet run --project %~dp0src\build\ElastiBuild.csproj -c Debug -- %*

popd

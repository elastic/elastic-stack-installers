@echo off
dotnet run --project %~dp0src\build\ElastiBuild.csproj -c Release -- %*
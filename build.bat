@echo off
set DOTNET_CLI_TELEMETRY_OPTOUT=1

pushd .
cd %~dp0%
dotnet run --project %~dp0src\build\ElastiBuild.csproj -c Release -- %*
popd

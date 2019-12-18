@echo off
set DOTNET_CLI_TELEMETRY_OPTOUT=1

rem When built with Release Manager we put 
rem Net Core SDK in the parent dir
set PATH=%PATH%;%~dp0\..\dotnet-sdk;

pushd .
cd %~dp0%
dotnet run --project %~dp0src\build\ElastiBuild.csproj -c Release -- %*
popd

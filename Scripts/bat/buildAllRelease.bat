@echo off
cd ../../

call git submodule update --init --recursive -- remote
call dotnet build -c Release

pause

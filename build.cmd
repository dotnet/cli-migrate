@echo off
powershell -ExecutionPolicy ByPass -command "& """%~eng\common\build.ps1""" -restore -build %*"
exit /b %ErrorLevel%

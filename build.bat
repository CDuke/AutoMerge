@echo off
cls
rem "tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"

SET VSVERSION="2013"
IF NOT [%1%] == [] (
	SET VSVERSION="%1"
)

"packages/FAKE.2.16.1/tools/Fake.exe" "build.fsx" "vsversion=%VSVERSION%"
pause
exit /b %errorlevel%
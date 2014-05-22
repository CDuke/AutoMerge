@echo off
cls
rem "tools/nuget/nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"

SET VSVERSION="2013"
IF NOT [%1%] == [] (
	SET VSVERSION="%1"
)

"tools/FAKE/tools/Fake.exe" "build.fsx" "vsversion=%VSVERSION%"
pause
exit /b %errorlevel%
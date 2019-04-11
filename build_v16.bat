@echo off
cls
IF NOT EXIST "tools/FAKE" (
	"tools/nuget/nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
)

"tools/FAKE/tools/Fake.exe" "build/build.fsx" "VisualStudioVersion=16.0"
pause
exit /b %errorlevel%
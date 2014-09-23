@echo off
cls
IF NOT EXIST "tools/FAKE" (
	"tools/nuget/nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
)

"tools/FAKE/tools/Fake.exe" "build/build.fsx" "VisualStudioVersion=14.0"
pause
exit /b %errorlevel%
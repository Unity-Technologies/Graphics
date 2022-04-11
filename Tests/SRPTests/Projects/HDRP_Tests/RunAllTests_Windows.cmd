echo off

rem example of command line : .\Unity.exe -runTests -projectPath "L:\Gits\Graphics\TestProjects\HDRP_Tests" -testPlatform StandaloneWindows64 -testResults "L:\Gits\Graphics\TestProjects\HDRP_Tests\TestResults.xml" -batchmode -executeMethod SetupProject.ApplySettings deferred

set pipelines=deferred

rem set pipelines=%pipelines%;deferred-depth-prepass
rem set pipelines=%pipelines%;deferred-depth-prepass-alpha-only
rem set pipelines=%pipelines%;forward

rem set platforms=StandaloneWindows64
set platforms=playmode

set  unity=%1
set project=%~dp0

echo Unity:
echo %unity%
echo/

echo Project:
echo %project%
echo/

pause
echo/

for %%a in (%platforms%) do (

	for %%b in (%pipelines%) do (
		echo start /WAIT %unity% -projectPath "%project%" -forgetProjectPath -automated -testResults "%project%TestResults-%%a-%%b.xml" -logFile "%project%Log-%%a-%%b.log" -runTests -testPlatform %%a
		echo/
		
		start /WAIT %unity% -projectPath "%project%" -forgetProjectPath -automated -testResults "%project%TestResults-%%a-%%b.xml" -logFile "%project%Log-%%a-%%b.log" -runTests -testPlatform %%a
	)
)

pause
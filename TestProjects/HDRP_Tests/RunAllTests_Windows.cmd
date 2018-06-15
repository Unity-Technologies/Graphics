echo off

rem example of command line : .\Unity.exe -runTests -projectPath "L:\Gits\Graphics\TestProjects\HDRP_Tests" -testPlatform StandaloneWindows64 -testResults "L:\Gits\Graphics\TestProjects\HDRP_Tests\TestResults.xml" -batchmode -executeMethod SetupProject.ApplySettings deferred

set pipelines=deferred
set pipelines=%pipelines%;deferred-depth-prepass
set pipelines=%pipelines%;deferred-depth-prepass-alpha-only
set pipelines=%pipelines%;forward

set platforms=StandaloneWindows64

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
	
		echo start /WAIT %unity% -runTests -projectPath "%project%" -testPlatform %%a -testResults "%project%TestResults-%%a-%%b.xml" -logFile "%project%Log%%a-%%b.log" -batchmode -executeMethod SetupProject.ApplySettings %%b
		echo/
		
		start /WAIT %unity% -runTests -projectPath "%project%" -testPlatform %%a -testResults "%project%TestResults-%%a-%%b.xml" -logFile "%project%Log%%a-%%b.log" -batchmode -executeMethod SetupProject.ApplySettings %%b
	)
)

pause
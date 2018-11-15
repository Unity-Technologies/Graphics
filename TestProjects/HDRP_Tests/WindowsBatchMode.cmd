set  unity=%1
set project=%~dp0

start /WAIT %unity% -batchmode -projectPath "%project%" -forgetProjectPath -automated -testResults "%project%TestResults.xml" -logFile "%project%Log.log" -runTests -testPlatform playmode


REM C:\buildslave\com.unity.render-pipelines\editor\Unity.exe
REM -batchmode
REM -projectPath
REM C:\buildslave\com.unity.render-pipelines\package\artifacts\TestProjects\HDRP_Tests/
REM -forgetProjectPath
REM -automated
REM -testResults
REM C:\buildslave\com.unity.render-pipelines\package\build\logs\TestResults_playmode_HDRP_Tests.xml
REM -logFile
REM C:\buildslave\com.unity.render-pipelines\package\build\logs\editor_playmode_HDRP_Tests.log
REM -runTests
REM -testPlatform
REM playmode
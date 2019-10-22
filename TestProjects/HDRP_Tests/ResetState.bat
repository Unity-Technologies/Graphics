@echo off

:: Delete iterative test run temp data
del /f "%AppData%\LocalLow\DefaultCompany\UnityTestFramework\tempSceneStorage.txt"
del /f "CurTestState.txt"
del /f "TestsDone.txt"
:: Clear result images from tests
del "upm-ci~\test-results\Assets\ActualImages\Linear\WindowsPlayer\*"
git checkout -- ProjectSettings/EditorBuildSettings.asset
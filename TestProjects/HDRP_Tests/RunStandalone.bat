@echo off

:: Redundant w/ .yml, used for local testing
IF NOT exist utr ( git clone git@github.cds.internal.unity3d.com:unity/utr.git )
IF NOT exist .Editor ( pip install unity-downloader-cli --extra-index-url https://artifactory.eu-cph-1.unityops.net/api/pypi/common-python/simple && unity-downloader-cli -b trunk -c editor --wait --published ) ELSE ( echo .Editor exists, using existing... )

:: --suite=editor --platform=editmode
:: Loop the utr test run, relying on the script to set GRAPHICS_TESTS_DONE when iterated through tests
if NOT "%i"=="" ( set args="--suite=playmode --platform=Standalone" )
else ( set args="%i" )
SET GRAPHICS_TESTS_DONE=False
:while
if %GRAPHICS_TESTS_DONE% EQU False (
   :: utr/utr %args%Windows64 --testproject=. --editor-location=.Editor --artifacts_path=upm-ci~/test-results --timeout=1200
   utr/utr --suite=playmode --platform=StandaloneWindows64 --testproject=. --editor-location=.Editor --artifacts_path=upm-ci~/test-results --timeout=1200
    goto :while
)


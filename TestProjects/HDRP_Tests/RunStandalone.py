import os.path
import os
import subprocess

if not os.path.exists("utr"):
    subprocess.call(["git", "clone", "git@github.cds.internal.unity3d.com:unity/utr.git"])

if not os.path.exists(".Editor"):
    subprocess.call(["pip", "install", "unity-downloader-cli", "--extra-index-url", "https://artifactory.eu-cph-1.unityops.net/api/pypi/common-python/simple"])
    subprocess.call(["unity-downloader-cli", "-b trunk", "-c editor", "--wait", "--published"]) 
else:
    print(".Editor exists, using existing...")

# os.environ["GRAPHICS_TESTS_DONE"] = "False"
# os.environ["GRAPHICS_TEST_ITERATOR"] = "0"

# while os.environ["GRAPHICS_TESTS_DONE"] == "False":
while not os.path.exists("TestsDone.txt"):
    iterative_split_res = subprocess.call([".Editor\Unity.exe", "-batchMode", "-quit", "-projectPath", "./", "-executeMethod", "CustomBuildSceneIterator.SelectIterativeScenesToBuild"])
    if iterative_split_res != 0:
        break
    test_run_res = subprocess.call(["utr\utr", "--suite=playmode", "--platform=StandaloneWindows64", "--extra-editor-arg=\"-executemethod\"", "--extra-editor-arg=\"CustomBuild.BuildWindowsDX11Linear\"", "--testproject=.", "--editor-location=.Editor", "--artifacts_path=upm-ci~/test-results", "--timeout=2400"], shell=True)
    if test_run_res != 0:
        break


using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

class ShaderTests
{
    private static readonly string s_ShaderAPIFolder = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Test", "Editor", "ShaderAPITests");
    private struct GraphicsPackageInfo
    {
        // This variable retrieved from the JSON file
        #pragma warning disable 649
        public string version;
        #pragma warning restore 649

        public int majorVersion;
        public int minorVersion;
        public int patchVersion;
        //public string name;
        //public string description;
        //public string unity;
        //public string unityRelease;
        //public string displayName;
        //public string dependencies;
        //public string keywords;
    }

    //[Test]
    public static void CreateShaderAPIFile()
    {
        if (!GetCurrentPackageInfo(out var curPackageInfo))
        {
            Assert.Fail("Unable to find current package version");
            return;
        }

        // Get the Shader API for the project
        PackageFunctionsSaveData results = RetrieveShaderAPI(curPackageInfo);

        // Save to disk
        ShaderParser.SavePackageFileToDisk(results, GetGraphicsDirectory(), GetSaveFilePath(curPackageInfo.version));
    }

    // When creating a new render pipeline asset it should not log any errors or throw exceptions.
    [Test]
    public void TestShaderAPI()
    {
        if (!GetCurrentPackageInfo(out var curPackageInfo))
        {
            Assert.Fail("Unable to find current package version");
            return;
        }

        // Read File from the previous Package
        bool wasAbleToFindPrevVersion = GetPreviousPackageInfo(curPackageInfo, out string prevPackageFilePath, out string prevPackageVersion);
        if (!wasAbleToFindPrevVersion)
        {
            //GetSaveFilePath(curPackageInfo.version);
            string currentPackageFilePath = GetSaveFilePath(curPackageInfo.version);
            if (!ShaderParser.LoadShaderAPIList(currentPackageFilePath, out PackageFunctionsSaveData asdf))
            {
                Assert.Fail("Error: Unable to find previous version to compare with!");
                return;
            }

            prevPackageVersion = curPackageInfo.version;
            prevPackageFilePath = currentPackageFilePath;
        }

        // Fetch all functions in the previous version
        if (!ShaderParser.LoadShaderAPIList(prevPackageFilePath, out PackageFunctionsSaveData prevShaderAPI))
            Assert.Fail("Unable to load \"" + prevPackageFilePath + "\"");

        // Fetch all functions in the current version
        PackageFunctionsSaveData currentShaderAPI = RetrieveShaderAPI(curPackageInfo);

        // Determine what kind of package change this is...
        ShaderParser.SplitVersionInfo(prevShaderAPI.version, out int prevMajorVersion, out int prevMinorVersion, out int prevPatchVersion);
        ShaderParser.SplitVersionInfo(currentShaderAPI.version, out int curMajorVersion, out int curMinorVersion, out int curPatchVersion);
        bool isAMajorVersionChange = curMajorVersion > prevMajorVersion;
        bool isAMinorVersionChange = curMinorVersion > prevMinorVersion;
        string changeType = ((isAMajorVersionChange) ? "Major" : (isAMinorVersionChange) ? "Minor" : "Patch") + " version change.";

        // Compare the two packages
        bool testPassed = ShaderParser.ComparePackageFunctions(isAMajorVersionChange, prevShaderAPI, currentShaderAPI, out string log);

        // Logging...
        if (testPassed)
        {
            Debug.Log(log);
            Assert.Pass("Comparing URP versions \"" + curPackageInfo.version + "\" and \"" + prevPackageVersion + "\" succeeded!\n" + changeType);
        }
        else
        {
            Debug.Log(log);
            Assert.Fail("Comparing URP versions \"" + curPackageInfo.version + "\" and \"" + prevPackageVersion + "\" failed!\n" + changeType);
        }
    }

    private static int GetPackageVal(int major, int minor, int patch)
    {
        return major * 1000000 + minor * 1000 + patch;
    }

    private static string GetGraphicsDirectory()
    {
        return Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
    }

    private static string GetSaveDirectoryPath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Test", "Editor", "ShaderAPITests");
    }

    private static string GetSaveFilePath(string version)
    {
        return Path.Combine(GetSaveDirectoryPath(), version + ".txt");
    }

    private static bool GetCurrentPackageInfo(out GraphicsPackageInfo packageInfo)
    {
        string urpPackageJsonFile = Path.Combine(GetGraphicsDirectory(), "com.unity.render-pipelines.universal", "package.json");
        packageInfo = JsonUtility.FromJson<GraphicsPackageInfo>(File.ReadAllText(urpPackageJsonFile));
        return ShaderParser.SplitVersionInfo(packageInfo.version, out packageInfo.majorVersion, out packageInfo.minorVersion, out packageInfo.patchVersion);
    }

    private static PackageFunctionsSaveData RetrieveShaderAPI(GraphicsPackageInfo graphicsPackageInfo)
    {
        // Graphics Directory path...
        string graphicsDirectory = GetGraphicsDirectory();

        // URP Shader Library path...
        string urpShaderLibDirectory = Path.Combine(graphicsDirectory, "com.unity.render-pipelines.universal", "ShaderLibrary");

        // What folders to search in...
        string[] dirs =
        {
            urpShaderLibDirectory
        };

        // What search patterns to use...
        string[] searchPatterns = {"*.hlsl"};

        // Create the Save data
        return ShaderParser.CreateShaderAPIList(graphicsPackageInfo.version, graphicsDirectory, dirs, searchPatterns);
    }

    private static bool GetPreviousPackageInfo(GraphicsPackageInfo curPackageInfo, out string filePath, out string version)
    {
        version = "";//curPackageInfo.majorVersion + "." + curPackageInfo.minorVersion + "." + curPackageInfo.patchVersion;
        filePath = "";//Path.Combine(s_ShaderAPIFolder, version);
        int curPackageVal = GetPackageVal(curPackageInfo.majorVersion,curPackageInfo.minorVersion, curPackageInfo.patchVersion);

        string[] shaderAPIFiles = Directory.GetFiles(s_ShaderAPIFolder, "*.txt");

        int closestFileVal = Int32.MaxValue;
        string closestFile = null;
        for (int i = 0; i < shaderAPIFiles.Length; i++)
        {
            string shaderAPIPath = shaderAPIFiles[i];
            string fileName = Path.GetFileName(shaderAPIPath);
            ShaderParser.SplitVersionInfo(fileName, out int majorVersion, out int minorVersion, out int patchVersion);

            int thisPackageVal = GetPackageVal(majorVersion, minorVersion, patchVersion);
            if (thisPackageVal > curPackageVal)
                continue;

            int diff = curPackageVal - thisPackageVal;
            if (diff < closestFileVal)
            {
                closestFileVal = diff;
                closestFile = fileName;
            }
        }

        if (closestFile == null)
        {
            return false;
        }

        ShaderParser.SplitVersionInfo(closestFile, out int closestMajor, out int closestMinor, out int closestPatch);
        filePath = Path.Combine(s_ShaderAPIFolder, closestFile);
        version = closestMajor + "." + closestMinor + "." + closestPatch;
        return true;
    }
}

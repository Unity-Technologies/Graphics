// TODO:
// 1. What type changes are considered a breakage? float => int? float => Half?
// 2. Major version change, functions in deprecated.hlsl are allowed to disappear!
// 3. Should be a breakage ==> Prev: a(float one) & a(float one, float two) => a(float one) & a(float one, float two = 1.0)

using System.IO;
using NUnit.Framework;
using UnityEngine;

class ShaderTests
{
    [Test]
    public static void CreateShaderAPIFile()
    {
        // Get the Shader API for the project
        PackageFunctionsSaveData results = RetrieveCurrentShaderAPI();

        if (!GetCurrentPackageInfo(out var curPackageInfo))
        {
            Debug.Log("Unable to find current package version");
            return;
        }

        // Save to disk
        ShaderParser.SavePackageFileToDisk(results, GetGraphicsDirectory(), GetSaveFilePath(curPackageInfo.version));
    }

    // When creating a new render pipeline asset it should not log any errors or throw exceptions.
    [Test]
    public void TestShaderAPI()
    {
        if (!GetCurrentPackageInfo(out var curPackageInfo))
        {
            Debug.Log("Unable to find current package version");
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
        PackageFunctionsSaveData currentShaderAPI = RetrieveCurrentShaderAPI();

        // Compare the two packages
        bool testPassed = ShaderParser.ComparePackageFunctions(prevShaderAPI, currentShaderAPI, out string log);


        // Logging...
        if (testPassed)
        {
            Debug.Log(log);
            Assert.Pass("Comparing URP versions \"" + curPackageInfo.version + "\" and \"" + prevPackageVersion + "\" succeeded!");
        }
        else
        {
            Debug.Log(log);
            Assert.Fail("Comparing URP versions \"" + curPackageInfo.version + "\" and \"" + prevPackageVersion + "\" failed!\n");
        }
    }

    private struct GraphicsPackageInfo
    {
        public string version;
        public int majorVersion;
        public int minorVersion;
        public int patchVersion;
        public string name;
        public string description;
        public string unity;
        public string unityRelease;
        public string displayName;
        public string dependencies;
        public string keywords;
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

    private static bool SplitVersionToInts(string versionString, out int majorVersion, out int minorVersion, out int patchVersion)
    {
        majorVersion = 0;
        minorVersion = 0;
        patchVersion = 0;

        string[] versions = versionString.Split('.');
        if (!int.TryParse(versions[0], out majorVersion)) return false;
        if (!int.TryParse(versions[1], out minorVersion)) return false;
        if (!int.TryParse(versions[2], out patchVersion)) return false;

        return true;
    }

    private static bool GetCurrentPackageInfo(out GraphicsPackageInfo packageInfo)
    {
        string urpPackageJsonFile = Path.Combine(GetGraphicsDirectory(), "com.unity.render-pipelines.universal", "package.json");
        packageInfo = JsonUtility.FromJson<GraphicsPackageInfo>(File.ReadAllText(urpPackageJsonFile));
        return SplitVersionToInts(packageInfo.version, out packageInfo.majorVersion, out packageInfo.minorVersion, out packageInfo.patchVersion);
    }

    private static PackageFunctionsSaveData RetrieveCurrentShaderAPI()
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
        return ShaderParser.CreateShaderAPIList(graphicsDirectory, dirs, searchPatterns);
    }

    static readonly string s_ShaderAPIFolder = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Test", "Editor", "ShaderAPITests");
    private static bool GetPreviousPackageInfo(GraphicsPackageInfo curPackageInfo, out string filePath, out string version)
    {
        string[] shaderAPIFiles = Directory.GetFiles(s_ShaderAPIFolder, "*.txt");

        string shaderAPIToCompareWith = null;
        //int closestMajor = 9;
        int closestMinor = 9;
        int closestPatch = 9;
        string closestFileName = "";
        for (int i = 0; i < shaderAPIFiles.Length; i++)
        {
            string shaderAPIPath = shaderAPIFiles[i];
            string fileName = Path.GetFileName(shaderAPIPath);
            SplitVersionToInts(fileName, out int majorVersion, out int minorVersion, out int patchVersion);

            if (curPackageInfo.majorVersion != majorVersion) continue;
            if (curPackageInfo.minorVersion == minorVersion && curPackageInfo.patchVersion == patchVersion) continue;

            //int diffMajor = majorVersion - curPackageInfo.majorVersion;
            int diffMinor = curPackageInfo.minorVersion - minorVersion;
            int diffPatch = curPackageInfo.patchVersion - patchVersion;

            //if (closestMajor > diffMajor)
            //{
            //    closestMinor = diffMinor;
            //    closestPatch = diffPatch;
            //    closestFileName = fileName;
            //    shaderAPIToCompareWith = fileName;
            //}
            if (closestMinor > diffMinor)
            {
                closestMinor = diffMinor;
                closestPatch = diffPatch;
                closestFileName = fileName;
                shaderAPIToCompareWith = fileName;
            }
            else if (closestPatch > diffPatch)
            {
                closestMinor = diffMinor;
                closestPatch = diffPatch;
                closestFileName = fileName;
                shaderAPIToCompareWith = fileName;
            }
        }

        if (shaderAPIToCompareWith == null)
        {
            filePath = "";
            version = "";
            return false;
        }

        filePath = Path.Combine(s_ShaderAPIFolder, shaderAPIToCompareWith);
        version = closestFileName;

        return true;
    }
}

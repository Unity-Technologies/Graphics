using System.IO;
using Unity.Build;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Build.Common;
using System.Collections.Generic;

public class SetupGraphicsTestCases : IPrebuildSetup
{
    private static BuildTarget target;

    public void Setup()
    {
        TriggerPreparePlayerTest();

        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(GraphicsTests.path);
    }

    private static void Log(string t)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, t);
    }

    private static BuildConfiguration FindConfig(BuildTarget t)
    {
        string configPath = "";
        switch (t)
        {
            case BuildTarget.StandaloneWindows: configPath = "Assets/Tests/Editor/GraphicsBuildconfig_Win.buildconfiguration"; break;
            case BuildTarget.StandaloneWindows64: configPath = "Assets/Tests/Editor/GraphicsBuildconfig_Win.buildconfiguration"; break;
            case BuildTarget.StandaloneOSX: configPath = "Assets/Tests/Editor/GraphicsBuildconfig_Mac.buildconfiguration"; break;
        }
        return (BuildConfiguration)AssetDatabase.LoadAssetAtPath(configPath, typeof(BuildConfiguration));
    }

    public static void TriggerPreparePlayerTest()
    {
        var args = System.Environment.GetCommandLineArgs();
        string testType = "playmode test";
        for(int i=0; i<args.Length; i++)
        {
            //Debug
            Log("*************** SetupGraphicsTestCases - Args "+i+" = "+args[i]);

            //Tell whether yamato is running player test or playmode test
            if( args[i].Contains("Standalone") )
            {
                testType = "standalone test";
                PreparePlayerTest();
                break;
            }
        }
        Log("*************** SetupGraphicsTestCases - This is "+testType);
    }

    [MenuItem("GraphicsTest/PreparePlayerTest")]
    public static void PreparePlayerTest()
    {
        Log("*************** SetupGraphicsTestCases - PreparePlayerTest - trigger BuildConfig.Build()");

        //Get the correct config file
        target = EditorUserBuildSettings.activeBuildTarget;
        BuildConfiguration config = FindConfig(target);

        //Sync the scenelist from BuildSettings to the Config file
        List<SceneList.SceneInfo> scenelist = new List<SceneList.SceneInfo>();
        EditorBuildSettingsScene[] buildSettingScenes = EditorBuildSettings.scenes;
        for(int i=0;i<buildSettingScenes.Length;i++)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildSettingScenes[i].path);
            scenelist.Add(new SceneList.SceneInfo() { AutoLoad = false, Scene = GlobalObjectId.GetGlobalObjectIdSlow(sceneAsset) });
        }
        var sceneListComponent = config.GetComponent<SceneList>();
        sceneListComponent.SceneInfos = scenelist;
        config.SetComponent<SceneList>(sceneListComponent);
        config.SaveAsset();
        //AssetDatabase.Refresh();

        while(EditorApplication.isCompiling)
        {
            //Wait for a bit because Editor will be compiling the config asset update
        }

        Log("*************** SetupGraphicsTestCases - PreparePlayerTest - Synced "+buildSettingScenes.Length+ " scenes to scenelist");

        //Make the build
        config.Build();

        Log("*************** SetupGraphicsTestCases - PreparePlayerTest - Move subscene cache");
        CreateFolder();
        CopyFiles();
    }

    [MenuItem("GraphicsTest/Debug/CreateFolder")]
    public static void CreateFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets\\StreamingAssets"))
        {
            AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            AssetDatabase.Refresh();
        }

        if (!AssetDatabase.IsValidFolder("Assets\\StreamingAssets\\SubScenes"))
        {
            AssetDatabase.CreateFolder("Assets\\StreamingAssets", "SubScenes");
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("GraphicsTest/Debug/CopyFiles")]
    public static void CopyFiles()
    {
        string projPath = Application.dataPath.Replace("/Assets", "");
        string srcPath = "";
        string dstPath = projPath + "/Assets/StreamingAssets/SubScenes";

        //decide path
        switch (target)
        {
            case BuildTarget.StandaloneWindows: srcPath = projPath + "/Builds/GraphicsTest/GraphicsTest_Data/StreamingAssets/SubScenes"; break;
            case BuildTarget.StandaloneWindows64: srcPath = projPath + "/Builds/GraphicsTest/GraphicsTest_Data/StreamingAssets/SubScenes"; break;
            case BuildTarget.StandaloneOSX: srcPath = projPath + "/Builds/GraphicsTest/GraphicsTest.app/Contents/Resources/Data/StreamingAssets/SubScenes"; break;
        }

        //Windows
        //srcPath = @"D:\UnityProject\dots\HybridURPSamples\Builds\HybridURPSamplesBuildSettings\HybridURPSamples_Data\StreamingAssets\SubScenes";
        //dstPath = @"D:\UnityProject\dots\HybridURPSamples\Assets\StreamingAssets\SubScenes";

        Log("*************** SetupGraphicsTestCases - srcPath = " + srcPath);
        Log("*************** SetupGraphicsTestCases - dstPath = " + dstPath);

        string[] fileList = Directory.GetFiles(srcPath, "*.*", SearchOption.AllDirectories);
        for (int i = 0; i < fileList.Length; i++)
        {
            File.Copy(fileList[i], fileList[i].Replace(srcPath, dstPath), true);
        }

        AssetDatabase.Refresh();
        Log("*************** SetupGraphicsTestCases - CopyFile Done. You can now do Testrunner > Run All in player");
    }
}
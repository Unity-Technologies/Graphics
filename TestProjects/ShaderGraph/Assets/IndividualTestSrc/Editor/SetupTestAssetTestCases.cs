using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEditor.TestTools.Graphics;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.XR;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

public class SetupTestAssetTestCases : IPrebuildSetup
{

        public static RuntimePlatform BuildTargetToRuntimePlatform(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return RuntimePlatform.Android;
            case BuildTarget.iOS:
                return RuntimePlatform.IPhonePlayer;
#if !UNITY_2019_2_OR_NEWER
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinuxUniversal:
#endif
            case BuildTarget.StandaloneLinux64:
                return RuntimePlatform.LinuxPlayer;
            case BuildTarget.StandaloneOSX:
                return RuntimePlatform.OSXPlayer;
            case BuildTarget.PS4:
                return RuntimePlatform.PS4;
#if !UNITY_2018_3_OR_NEWER
                case BuildTarget.PSP2:
                    return RuntimePlatform.PSP2;
#endif
            case BuildTarget.Switch:
                return RuntimePlatform.Switch;
            case BuildTarget.WebGL:
                return RuntimePlatform.WebGLPlayer;
            case BuildTarget.WSAPlayer:
                return RuntimePlatform.WSAPlayerX64;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return RuntimePlatform.WindowsPlayer;
            case BuildTarget.XboxOne:
                return RuntimePlatform.XboxOne;
            case BuildTarget.tvOS:
                return RuntimePlatform.tvOS;
#if UNITY_2019_3_OR_NEWER
            case BuildTarget.Stadia:
                return RuntimePlatform.Stadia;
#endif
        }

        throw new ArgumentOutOfRangeException("target", target, "Unknown BuildTarget");
    }


    private static bool IsBuildingForEditorPlaymode
    {
        get
        {
#if TEST_FRAMEWORK_1_2_0_OR_NEWER
                    return TestRunnerApi.GetActiveRunGuids().Any(guid =>
                    {
                        var settings = TestRunnerApi.GetExecutionSettings(guid);
                        return settings.filters[0].targetPlatform != null;
                    });
#else
            var playmodeLauncher =
                    typeof(RequirePlatformSupportAttribute).Assembly.GetType(
                        "UnityEditor.TestTools.TestRunner.PlaymodeLauncher");
            var isRunningField = playmodeLauncher.GetField("IsRunning");

            return (bool)isRunningField.GetValue(null);
#endif
        }
    }


    public static IEnumerable<ShaderGraphTestAsset> ShaderGraphTests
    {
        get
        {
            var strings = UnityEditor.AssetDatabase.FindAssets("t:ShaderGraphTestAsset");
            foreach(string path in strings)
            {
                yield return UnityEditor.AssetDatabase.LoadAssetAtPath<ShaderGraphTestAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(path));
            }
        }
    }


    public void Setup()
    {
        ColorSpace colorSpace;
        BuildTarget buildPlatform;
        RuntimePlatform runtimePlatform;
        GraphicsDeviceType[] graphicsDevices;

        string xrsdk = "None";

        UnityEditor.EditorPrefs.SetBool("AsynchronousShaderCompilation", false);

        // Figure out if we're preparing to run in Editor playmode, or if we're building to run outside the Editor
        if (IsBuildingForEditorPlaymode)
        {
            colorSpace = QualitySettings.activeColorSpace;
            buildPlatform = BuildTarget.NoTarget;
            runtimePlatform = Application.platform;
            graphicsDevices = new[] { SystemInfo.graphicsDeviceType };

            SetGameViewSize(ImageAssert.kBackBufferWidth, ImageAssert.kBackBufferHeight);
        }
        else
        {
            buildPlatform = EditorUserBuildSettings.activeBuildTarget;
            runtimePlatform = BuildTargetToRuntimePlatform(buildPlatform);
            colorSpace = PlayerSettings.colorSpace;
            graphicsDevices = PlayerSettings.GetGraphicsAPIs(buildPlatform);
        }

#pragma warning disable 0618
#if !UNITY_2020_2_OR_NEWER
            if (PlayerSettings.virtualRealitySupported == true)
            {
                string[] VrSDKs;

                // The NoTarget build target used here when we're in editor mode won't return any xr sdks
                // So just using the Standalone one since that should be what the editor is using.
                if(IsBuildingForEditorPlaymode)
                {
                    VrSDKs = PlayerSettings.GetVirtualRealitySDKs(BuildTargetGroup.Standalone);
                }
                else
                {
                    VrSDKs = PlayerSettings.GetVirtualRealitySDKs(BuildPipeline.GetBuildTargetGroup(buildPlatform));
                }

                // VR can be enabled and no VR platforms listed in the UI.  In that case it will use the non-xr rendering.
                xrsdk = VrSDKs.Length == 0 ? "None" : VrSDKs.First();
            }
#endif

        var xrsettings = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildPipeline.GetBuildTargetGroup(buildPlatform));

        // Since the settings are null when using NoTarget for the BuildTargetGroup which editor playmode seems to do
        // just use Standalone settings instead.
        if (IsBuildingForEditorPlaymode)
            xrsettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

        if (xrsettings != null && xrsettings.InitManagerOnStart)
        {
            if (xrsettings.AssignedSettings.loaders.Count > 0)
            {
                // since we don't really know which runtime loader will actually be used at runtime,
                // just take the first one assuming it will work and if it isn't loaded the
                // tests should fail since the reference images bundle will be named
                // with a loader that isn't active at runtime.
                var firstLoader = xrsettings.AssignedSettings.loaders.First();

                if (firstLoader != null)
                {
                    xrsdk = firstLoader.name;
                }
            }
        }

        EnsureExpectedTestResultsInitialized(colorSpace, runtimePlatform, graphicsDevices, xrsdk);

        #region SetupForPlayer
        var bundleBuilds = new List<AssetBundleBuild>();

        if (!IsBuildingForEditorPlaymode)
        {
            foreach (var api in graphicsDevices)
            {
                //--------------do we need to setup anthing here since we are storing the images as byte arrays in a scriptable object?
                ////UnityEditor.TestTools.Graphics.Utils.SetupReferenceImageImportSettings(images.Values);

                var expected = CollectExpectedTestResults(colorSpace, runtimePlatform, api, xrsdk);

                bundleBuilds.Add(new AssetBundleBuild
                {
                    assetBundleName = $"referenceimages--individual--{colorSpace}-{runtimePlatform}-{api}-{xrsdk}",
                    addressableNames = expected.Keys.ToArray(),
                    assetNames = expected.Values.ToArray()
                });
            }
        }

        if (bundleBuilds.Count > 0)
        {
            if (!Directory.Exists("Assets/StreamingAssets"))
                Directory.CreateDirectory("Assets/StreamingAssets");

            foreach (var bundle in bundleBuilds)
            {
                BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", new[] { bundle }, BuildAssetBundleOptions.None,
                    buildPlatform);
            }
        }
        #endregion

#pragma warning restore 0618

        // if (!IsBuildingForEditorPlaymode)
        // new CreateSceneListFileFromBuildSettings().Setup();
    }


    private void EnsureExpectedTestResultsInitialized(ColorSpace colorSpace, RuntimePlatform runtimePlatform, GraphicsDeviceType[] graphicsDevices, string xrsdk)
    {
        foreach (var api in graphicsDevices)
        {
            var fullPathPrefix = $"Assets/ReferenceImages/{colorSpace}/{runtimePlatform}/{api}/{xrsdk}/";

            foreach (var testAsset in ShaderGraphTests)
            {
                var directoryPath = Path.Combine(fullPathPrefix, $"{testAsset.name}/");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var individualMaterialTest in testAsset.testMaterial)
                    {
                        if(testAsset.name == null || individualMaterialTest.material == null || individualMaterialTest.material.name == null)
                        {
                            continue;
                        }
                        
                        TestAssetTestData.EnsureTestData(directoryPath,
                                                         testAsset.name,
                                                         individualMaterialTest.material,
                                                         individualMaterialTest.hash,
                                                         testAsset.isCameraPerspective,
                                                         testAsset.settings,
                                                         testAsset.customMesh,
                                                         out string filePath);
                        AssetDatabase.ImportAsset(filePath);

                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
    }

    public static Dictionary<string, string> CollectExpectedTestResults(ColorSpace colorSpace, RuntimePlatform runtimePlatform, GraphicsDeviceType api, string xrsdk)
    {
        var output = new Dictionary<string, string>();

        var fullPathPrefix = $"Assets/ReferenceImages/{colorSpace}/{runtimePlatform}/{api}/{xrsdk}/";

        foreach(var testAsset in ShaderGraphTests)
        {
            var testResultsPath = Path.Combine(fullPathPrefix, $"{testAsset.name}/");
            if(testAsset.customMesh != null)
            {
                output[testAsset.customMesh.name] = AssetDatabase.GetAssetPath(testAsset.customMesh);
            }
            foreach (var individualTest in testAsset.testMaterial)
            {
                if(individualTest.material == null || individualTest.enabled == false)
                {
                    continue;
                }

                File.AppendAllLines("Logs/Test.log", new string[] { "Trying to get test data..." });
                if(!TestAssetTestData.TryGetTestData(testResultsPath, testAsset.name, individualTest.material, out TestAssetTestData data))
                {
                    continue;
                }
//                AssetDatabase.ImportAsset(data.FilePath);

                string path = data.GetReferenceImagePath();
                Debug.Log(path);
                File.AppendAllLines("Logs/Test.log", new string[] { $"Found test data: {data} with path {path}" });
                if(File.Exists(path) && AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                {
                    output[data.ReferenceImageLookup] = path;
                }

                output[$"{testAsset.name}_{individualTest.material.name}_hash"] = data.FilePath;
                File.AppendAllLines("Logs/Test.log", new string[] { $"Returning IndividualTest obj with material {individualTest.material}" });
                output[data.TestMaterialLookup] = AssetDatabase.GetAssetPath(individualTest.material);
            }
        }

        return output;
    }

    public static void SetGameViewSize(int width, int height)
    {
        object size = GameViewSize.SetCustomSize(width, height);
        GameViewSize.SelectSize(size);
    }

}

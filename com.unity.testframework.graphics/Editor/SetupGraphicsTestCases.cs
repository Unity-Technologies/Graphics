using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Reflection;
using UnityEditor.XR.Management;
using UnityEngine.TestTools.Graphics;
using EditorSceneManagement = UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Test framework prebuild step to collect reference images for the current test run and prepare them for use in the
    /// player.
    /// Will also build Lightmaps for specially labelled scenes.
    /// </summary>
    public static class SetupGraphicsTestCases
    {
        static readonly string bakeLabel = "TestRunnerBake";

        private static bool IsBuildingForEditorPlaymode
        {
            get
            {
#if TEST_FRAMEWORK_1_2_0_OR_NEWER
                    return TestRunnerApi.GetActiveRunGuids().Any(guid =>
                    {
                        var settings = TestRunnerApi.GetExecutionSettings(guid);
                        return settings.targetPlatform == null;
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

        public static void Setup(string rootImageTemplatePath = EditorGraphicsTestCaseProvider.ReferenceImagesRoot, string imageResultsPath = "")
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
                runtimePlatform = EditorUtils.BuildTargetToRuntimePlatform(buildPlatform);
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
                if (IsBuildingForEditorPlaymode)
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

            var xrsettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildPipeline.GetBuildTargetGroup(buildPlatform));

            // Since the settings are null when using NoTarget for the BuildTargetGroup which editor playmode seems to do
            // just use Standalone settings instead.
            if (IsBuildingForEditorPlaymode)
                xrsettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

            if (xrsettings != null && xrsettings.InitManagerOnStart && !RuntimeSettings.reuseTestsForXR)
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

            ImageHandler.instance.ImageResultsPath = imageResultsPath;

            var bundleBuilds = new List<AssetBundleBuild>();

            if (!IsBuildingForEditorPlaymode)
            {
                foreach (var api in graphicsDevices)
                {
                    var images = EditorGraphicsTestCaseProvider.CollectReferenceImagePathsFor(rootImageTemplatePath, colorSpace, runtimePlatform, api, xrsdk);

                    EditorUtils.SetupReferenceImageImportSettings(images.Values);

                    bundleBuilds.Add(new AssetBundleBuild
                    {
                        assetBundleName = string.Format("referenceimages-{0}-{1}-{2}-{3}", colorSpace, runtimePlatform.ToUniqueString(), api, xrsdk),
                        addressableNames = images.Keys.ToArray(),
                        assetNames = images.Values.ToArray()
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

            // For each scene in the build settings, force build of the lightmaps if it has "DoLightmap" label.
            // Note that in the PreBuildSetup stage, TestRunner has already created a new scene with its testing monobehaviours

            Scene trScene = EditorSceneManagement.EditorSceneManager.GetSceneAt(0);

            string[] selectedScenes = GetSelectedScenes();

            var sceneIndex = 0;
            var buildSettingsScenes = EditorBuildSettings.scenes;
            var totalScenes = EditorBuildSettings.scenes.Length;

            var filterGuid = AssetDatabase.FindAssets("t: TestFilters");

            var filterConfigs = AssetDatabase.LoadAssetAtPath<TestFilters>(
                AssetDatabase.GUIDToAssetPath(filterGuid.FirstOrDefault()));

            var filterTest = Resources.Load<TestFilters>("TestCaseFilters");

            foreach (var scene in buildSettingsScenes)
            {
                if (!scene.enabled) continue;

                if (filterConfigs != null)
                {
                    var filtersForScene = new List<TestFilterConfig>();

                    // Right now only single TestFilter.asset file will be processed
                    foreach (var testFilter in filterConfigs.filters)
                    {
                        // legacy support for when test filters only supported one scene.
                        if (testFilter.FilteredScene != null && (testFilter.FilteredScenes == null || !testFilter.FilteredScenes.Any(
                            s => AssetDatabase.GetAssetPath(s) == AssetDatabase.GetAssetPath(testFilter.FilteredScene))))
                        {
                            if (testFilter.FilteredScenes == null)
                            {
                                testFilter.FilteredScenes = new SceneAsset[] { testFilter.FilteredScene };
                            }
                            else
                            {
                                testFilter.FilteredScenes.Concat(new[] { testFilter.FilteredScene }).ToArray();
                            }

                        }

                        foreach (var filteredScene in testFilter.FilteredScenes)
                        {
                            if (AssetDatabase.GetAssetPath(filteredScene) == scene.path)
                            {
                                filtersForScene.Add(testFilter);

                                // If duplicates scenes match we don't need to duplicate the filter in the list.
                                break;
                            }
                        }
                    }

                    // In case more than one filter match the scene, display all the reasons in the output.
                    string filterReasons = string.Empty;

                    foreach (var filter in filtersForScene)
                    {
                        StereoRenderingModeFlags stereoModeFlag = 0;

                        switch (PlayerSettings.stereoRenderingPath)
                        {
                            case StereoRenderingPath.MultiPass:
                                stereoModeFlag |= StereoRenderingModeFlags.MultiPass;
                                break;
                            case StereoRenderingPath.SinglePass:
                                stereoModeFlag |= StereoRenderingModeFlags.SinglePass;
                                break;
                            case StereoRenderingPath.Instancing:
                                stereoModeFlag |= StereoRenderingModeFlags.Instancing;
                                break;
                        }

                        if ((filter.BuildPlatform == buildPlatform || filter.BuildPlatform == BuildTarget.NoTarget) &&
                            (filter.GraphicsDevice == graphicsDevices.First() || filter.GraphicsDevice == GraphicsDeviceType.Null) &&
                            (filter.ColorSpace == colorSpace || filter.ColorSpace == ColorSpace.Uninitialized))
                        {
                            // Non vr filter matched if none of the VR settings are present.
                            if ((!PlayerSettings.virtualRealitySupported || !(xrsettings != null && xrsettings.InitManagerOnStart)) &&
                                (string.IsNullOrEmpty(filter.XrSdk) || string.Compare(filter.XrSdk, "None", true) == 0) &&
                                filter.StereoModes == StereoRenderingModeFlags.None)
                            {
                                scene.enabled = false;
                                filterReasons += filter.Reason + "\n";
                            }
                            // If VR is enabled then the VR specific filters need to match the filter too.
                            else if ((PlayerSettings.virtualRealitySupported || (xrsettings != null && xrsettings.InitManagerOnStart)) &&
                                (filter.StereoModes == StereoRenderingModeFlags.None || (filter.StereoModes & stereoModeFlag) == stereoModeFlag) &&
                                (filter.XrSdk == xrsdk || string.IsNullOrEmpty(filter.XrSdk)))
                            {
                                scene.enabled = false;
                                filterReasons += filter.Reason + "\n";
                            }
                        }
                    }

                    if (!scene.enabled)
                    {
                        Debug.Log(string.Format("Removed scene {0} from build settings because:\n{1}", Path.GetFileNameWithoutExtension(scene.path), filterReasons));
                    }
                }
#pragma warning restore 0618

                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                var labels = new System.Collections.Generic.List<string>(AssetDatabase.GetLabels(sceneAsset));

                // if we successfully retrieved the names of the selected scenes, we filter using this list
                if (selectedScenes.Length > 0 && !selectedScenes.Contains(sceneAsset.name))
                    continue;

                if (labels.Contains(bakeLabel))
                {
                    EditorSceneManagement.EditorSceneManager.OpenScene(scene.path, EditorSceneManagement.OpenSceneMode.Additive);

                    Scene currentScene = EditorSceneManagement.EditorSceneManager.GetSceneAt(1);

                    EditorSceneManagement.EditorSceneManager.SetActiveScene(currentScene);
#pragma warning disable 618
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
#pragma warning restore 618
                    //EditorUtility.DisplayProgressBar($"Baking Test Scenes {(sceneIndex + 1).ToString()}/{totalScenes.ToString()}", $"Baking {sceneAsset.name}", ((float)sceneIndex / totalScenes));

                    Lightmapping.Bake();

                    EditorSceneManagement.EditorSceneManager.SaveScene(currentScene);

                    EditorSceneManagement.EditorSceneManager.SetActiveScene(trScene);

                    EditorSceneManagement.EditorSceneManager.CloseScene(currentScene, true);
                }
                sceneIndex++;
            }

            // set the scene list in the build settings window.  Only updating the array will do this.
            EditorBuildSettings.scenes = buildSettingsScenes.Where(s => s.enabled).ToArray();

            //EditorUtility.ClearProgressBar();

            if (!IsBuildingForEditorPlaymode)
                new CreateSceneListFileFromBuildSettings().Setup();
        }

        static string[] GetSelectedScenes()
        {
            try
            {
                var testRunnerWindowType = Type.GetType("UnityEditor.TestTools.TestRunner.TestRunnerWindow, UnityEditor.TestRunner, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"); // type: TestRunnerWindow
                var testRunnerWindow = EditorWindow.GetWindow(testRunnerWindowType);
                var playModeListGUI = testRunnerWindowType.GetField("m_PlayModeTestListGUI", BindingFlags.NonPublic | BindingFlags.Instance); // type: PlayModeTestListGUI
                var testListTree = playModeListGUI.FieldType.BaseType.GetField("m_TestListTree", BindingFlags.NonPublic | BindingFlags.Instance); // type: TreeViewController

                // internal treeview GetSelection:
                var getSelectionMethod = testListTree.FieldType.GetMethod("GetSelection", BindingFlags.Public | BindingFlags.Instance); // int[] GetSelection();
                var playModeListGUIValue = playModeListGUI.GetValue(testRunnerWindow);
                var testListTreeValue = testListTree.GetValue(playModeListGUIValue);

                var selectedItems = getSelectionMethod.Invoke(testListTreeValue, null);

                var getSelectedTestsAsFilterMethod = playModeListGUI.FieldType.BaseType.GetMethod(
                    "GetSelectedTestsAsFilter",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                dynamic testRunnerFilterArray = getSelectedTestsAsFilterMethod.Invoke(playModeListGUIValue, new object[] { selectedItems });

                var testNamesField = testRunnerFilterArray[0].GetType().GetField("testNames", BindingFlags.Instance | BindingFlags.Public);

                List<string> testNames = new List<string>();
                foreach (dynamic testRunnerFilter in testRunnerFilterArray)
                    testNames.AddRange(testNamesField.GetValue(testRunnerFilter));

                return testNames.Select(name => name.Substring(name.LastIndexOf('.') + 1)).ToArray();
            }
            catch (Exception)
            {
                return new string[] { }; // Ignore error and return an empty array
            }
        }

        /// <summary>
        /// Set the Game View size to match the desired width and height at runtime
        /// </summary>
        public static void SetGameViewSize(int width, int height)
        {
            object size = GameViewSize.SetCustomSize(width, height);
            GameViewSize.SelectSize(size);
        }

        static string lightmapDataGitIgnore = @"Lightmap-*_comp*
LightingData.*
ReflectionProbe-*";

        [MenuItem("Assets/Tests/Toggle Scene for Bake")]
        public static void LabelSceneForBake()
        {
            UnityEngine.Object[] sceneAssets = Selection.GetFiltered(typeof(SceneAsset), SelectionMode.DeepAssets);

            EditorSceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManagement.SceneSetup[] previousSceneSetup = EditorSceneManagement.EditorSceneManager.GetSceneManagerSetup();

            foreach (UnityEngine.Object sceneAsset in sceneAssets)
            {
                List<string> labels = new System.Collections.Generic.List<string>(AssetDatabase.GetLabels(sceneAsset));

                string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                string gitIgnorePath = Path.Combine(Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), scenePath.Substring(0, scenePath.Length - 6)), ".gitignore");

                if (labels.Contains(bakeLabel))
                {
                    labels.Remove(bakeLabel);
                    File.Delete(gitIgnorePath);
                }
                else
                {
                    labels.Add(bakeLabel);

                    string sceneLightingDataFolder = Path.Combine(Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath));
                    if (!AssetDatabase.IsValidFolder(sceneLightingDataFolder))
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath));

                    File.WriteAllText(gitIgnorePath, lightmapDataGitIgnore);

                    EditorSceneManagement.EditorSceneManager.OpenScene(scenePath, EditorSceneManagement.OpenSceneMode.Single);
                    EditorSceneManagement.EditorSceneManager.SetActiveScene(EditorSceneManagement.EditorSceneManager.GetSceneAt(0));
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                    EditorSceneManagement.EditorSceneManager.SaveScene(EditorSceneManagement.EditorSceneManager.GetSceneAt(0));
                }

                AssetDatabase.SetLabels(sceneAsset, labels.ToArray());
            }
            AssetDatabase.Refresh();

            if (previousSceneSetup.Length == 0)
                EditorSceneManagement.EditorSceneManager.NewScene(EditorSceneManagement.NewSceneSetup.DefaultGameObjects, EditorSceneManagement.NewSceneMode.Single);
            else
                EditorSceneManagement.EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
        }

        [MenuItem("Assets/Tests/Toggle Scene for Bake", true)]
        public static bool LabelSceneForBake_Test()
        {
            return IsSceneAssetSelected();
        }

        public static bool IsSceneAssetSelected()
        {
            UnityEngine.Object[] sceneAssets = Selection.GetFiltered(typeof(SceneAsset), SelectionMode.DeepAssets);

            return sceneAssets.Length != 0;
        }
    }
}

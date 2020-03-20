using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Reflection;

using UnityEditor;
using EditorSceneManagement = UnityEditor.SceneManagement;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Test framework prebuild step to collect reference images for the current test run and prepare them for use in the
    /// player.
    /// Will also build Lightmaps for specially labelled scenes.
    /// </summary>
    public class SetupGraphicsTestCases
    {
        static string bakeLabel = "TestRunnerBake";

        private static bool IsBuildingForEditorPlaymode
        {
            get
            {
                var playmodeLauncher =
                    typeof(RequirePlatformSupportAttribute).Assembly.GetType(
                        "UnityEditor.TestTools.TestRunner.PlaymodeLauncher");
                var isRunningField = playmodeLauncher.GetField("IsRunning");

                return (bool)isRunningField.GetValue(null);
            }
        }

        public void Setup()
        {
            Setup(EditorGraphicsTestCaseProvider.ReferenceImagesRoot);
        }

        public void Setup(string rootImageTemplatePath)
        {
            ColorSpace colorSpace;
            BuildTarget buildPlatform;
            RuntimePlatform runtimePlatform;
            GraphicsDeviceType[] graphicsDevices;

            UnityEditor.EditorPrefs.SetBool("AsynchronousShaderCompilation", false);

            // Figure out if we're preparing to run in Editor playmode, or if we're building to run outside the Editor
            if (IsBuildingForEditorPlaymode)
            {
                colorSpace = QualitySettings.activeColorSpace;
                buildPlatform = BuildTarget.NoTarget;
                runtimePlatform = Application.platform;
                graphicsDevices = new[] {SystemInfo.graphicsDeviceType};
            }
            else
            {
                buildPlatform = EditorUserBuildSettings.activeBuildTarget;
                runtimePlatform = Utils.BuildTargetToRuntimePlatform(buildPlatform);
                colorSpace = PlayerSettings.colorSpace;
                graphicsDevices = PlayerSettings.GetGraphicsAPIs(buildPlatform);
            }

            var bundleBuilds = new List<AssetBundleBuild>();

            foreach (var api in graphicsDevices)
            {
                var images = EditorGraphicsTestCaseProvider.CollectReferenceImagePathsFor(rootImageTemplatePath, colorSpace, runtimePlatform, api);

                Utils.SetupReferenceImageImportSettings(images.Values);

                if (buildPlatform == BuildTarget.NoTarget)
                    continue;

                bundleBuilds.Add(new AssetBundleBuild
                {
                    assetBundleName = string.Format("referenceimages-{0}-{1}-{2}", colorSpace, runtimePlatform, api),
                    addressableNames = images.Keys.ToArray(),
                    assetNames = images.Values.ToArray()
                });
            }

            if (bundleBuilds.Count > 0)
            {
                if (!Directory.Exists("Assets/StreamingAssets"))
                    Directory.CreateDirectory("Assets/StreamingAssets");

                foreach (var bundle in bundleBuilds)
                {
                    BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", new [] { bundle }, BuildAssetBundleOptions.None,
                        buildPlatform);
                }
            }


            // For each scene in the build settings, force build of the lightmaps if it has "DoLightmap" label.
            // Note that in the PreBuildSetup stage, TestRunner has already created a new scene with its testing monobehaviours

            Scene trScene = EditorSceneManagement.EditorSceneManager.GetSceneAt(0);

            string[] selectedScenes = GetSelectedScenes();

            var sceneIndex = 0;
            var totalScenes = EditorBuildSettings.scenes.Length;

            string[] filterGUIDs = AssetDatabase.FindAssets("t:TestFilters");

            List<TestFilters> filters = new List<TestFilters>();
            foreach (var filterGUID in filterGUIDs)
            {
                string filterPath = AssetDatabase.GUIDToAssetPath(filterGUID);
                filters.Add(AssetDatabase.LoadAssetAtPath(filterPath, typeof(TestFilters)) as TestFilters);
            }
            // Disabling scenes directly in EditorBuildSettings.scenes does not work
            // As a solution - disabling scenes in temporary variable and then assigning it back to EditorBuildSettings.scenes
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            foreach ( EditorBuildSettingsScene scene in scenes)
            {
                if (!scene.enabled) continue;

                if (filters.Count > 0)
                {
                    // Right now leaving only single filter available per project.
                    var filtersForScene = filters.First().filters.Where(f => AssetDatabase.GetAssetPath(f.FilteredScene) == scene.path);
                    bool enableScene = true;
                    string filterReasons = "";

                    foreach (var filter in filtersForScene)
                    {
                        if ((filter.BuildPlatform == buildPlatform || filter.BuildPlatform == BuildTarget.NoTarget) &&
                            (filter.GraphicsDevice == graphicsDevices.First() || filter.GraphicsDevice == GraphicsDeviceType.Null) &&
                            (filter.ColorSpace == colorSpace || filter.ColorSpace == ColorSpace.Uninitialized))
                        {
                            // Adding reasons in case when same test is ignored several times
                            filterReasons += filter.Reason + "\n";
                            enableScene = false;
                        }
                    }
                    scene.enabled = enableScene;
                    if (!enableScene)
                    {
                        Debug.Log(string.Format("Removed scene {0} from build settings because {1}", Path.GetFileNameWithoutExtension(scene.path), filterReasons));
                        continue;
                    }
                }


                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                var labels = new System.Collections.Generic.List<string>(AssetDatabase.GetLabels(sceneAsset));
                
                // if we successfully retrieved the names of the selected scenes, we filter using this list
                if (selectedScenes.Length > 0 && !selectedScenes.Contains(sceneAsset.name))
                    continue;

                if ( labels.Contains(bakeLabel) )
                {
                    EditorSceneManagement.EditorSceneManager.OpenScene(scene.path, EditorSceneManagement.OpenSceneMode.Additive);

                    Scene currentScene = EditorSceneManagement.EditorSceneManager.GetSceneAt(1);

                    EditorSceneManagement.EditorSceneManager.SetActiveScene(currentScene);
#pragma warning disable 618
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
#pragma warning restore 618
                    EditorUtility.DisplayProgressBar($"Baking Test Scenes {(sceneIndex + 1).ToString()}/{totalScenes.ToString()}", $"Baking {sceneAsset.name}", ((float)sceneIndex / totalScenes));

                    Lightmapping.Bake();

                    EditorSceneManagement.EditorSceneManager.SaveScene( currentScene );

                    EditorSceneManagement.EditorSceneManager.SetActiveScene(trScene);

                    EditorSceneManagement.EditorSceneManager.CloseScene(currentScene, true);
                }

                sceneIndex++;
            }
            
            EditorUtility.ClearProgressBar();
            EditorBuildSettings.scenes = scenes;

            if (!IsBuildingForEditorPlaymode)
                new CreateSceneListFileFromBuildSettings().Setup();
        }

        string[] GetSelectedScenes()
        {
            try {
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

                List< string > testNames = new List<string>();
                foreach (dynamic testRunnerFilter in testRunnerFilterArray)
                    testNames.AddRange(testNamesField.GetValue(testRunnerFilter));

                return testNames.Select(name => name.Substring(name.LastIndexOf('.') + 1)).ToArray();
            } catch (Exception) {
                return new string[] {}; // Ignore error and return an empty array
            }
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
                string gitIgnorePath = Path.Combine( Path.Combine( Application.dataPath.Substring(0, Application.dataPath.Length-6), scenePath.Substring(0, scenePath.Length-6) ) , ".gitignore" );

                if (labels.Contains(bakeLabel))
                {
                    labels.Remove(bakeLabel);
                    File.Delete(gitIgnorePath);
                }
                else
                {
                    labels.Add(bakeLabel);

                    string sceneLightingDataFolder = Path.Combine( Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath) );
                    if ( !AssetDatabase.IsValidFolder(sceneLightingDataFolder) )
                        AssetDatabase.CreateFolder( Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath) );

                    File.WriteAllText(gitIgnorePath, lightmapDataGitIgnore);

                    EditorSceneManagement.EditorSceneManager.OpenScene(scenePath, EditorSceneManagement.OpenSceneMode.Single);
                    EditorSceneManagement.EditorSceneManager.SetActiveScene( EditorSceneManagement.EditorSceneManager.GetSceneAt(0) );
#pragma warning disable 618
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
#pragma warning restore 618
                    EditorSceneManagement.EditorSceneManager.SaveScene( EditorSceneManagement.EditorSceneManager.GetSceneAt(0) );
                }

                AssetDatabase.SetLabels( sceneAsset, labels.ToArray() );
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

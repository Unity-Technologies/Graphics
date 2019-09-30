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
using System.Runtime.Serialization.Formatters.Binary;

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
        public const int DEFAULT_SCENES_PER_BUILD = 10;
        public const string ITER_ENV_VAR_NAME = "GRAPHICS_TEST_ITERATOR";
        public const string SCENES_PER_BUILD_ENV_VAR_NAME = "SCENES_PER_BUILD";
        public const string TEMP_SCENE_LOCATION = "tempSceneStorage";

        public void Setup()
        {
            Setup(EditorGraphicsTestCaseProvider.ReferenceImagesRoot);
        }

        private void SelectIterativeScenesToBuild() {
            int curIter = GetCurrentIteration();
            string scenesPerBuildEnvVal = Environment.GetEnvironmentVariable(SCENES_PER_BUILD_ENV_VAR_NAME);
            int scenesPerBuild = scenesPerBuildEnvVal != null ? int.Parse(scenesPerBuildEnvVal) : DEFAULT_SCENES_PER_BUILD;

            string dataPath = Application.persistentDataPath;

            List<EditorBuildSettingsScene> scenesToRun = (from scene in EditorBuildSettings.scenes where scene.enabled select scene).ToList();
            // Write scene list to temp save location, so it can be restored at Cleanup()
            System.IO.File.WriteAllLines(dataPath + "/" + TEMP_SCENE_LOCATION, from scene in scenesToRun select scene.guid.ToString());

            // Handles the case of hitting the end of the scene list, and not having enough scenes do run the whole SCENES_PER_BUILD quantity
            int scenesInBuild = scenesPerBuild;
            if ((curIter + 1) * scenesPerBuild >= scenesToRun.Count) {
                Environment.SetEnvironmentVariable("GRAPHICS_TESTS_DONE", "True");
                scenesInBuild = scenesToRun.Count - (curIter + 1) * scenesPerBuild;
            }
            List<GUID> runSceneGuids = (from scene in scenesToRun.GetRange(curIter * scenesPerBuild, scenesInBuild) select scene.guid).ToList();
            // Split the scene list
            foreach (var scene in EditorBuildSettings.scenes) {
                if (runSceneGuids.IndexOf(scene.guid) != -1 ) {
                    scene.enabled = true;
                    Debug.Log(scene.path);
                } else {
                    scene.enabled = false;
                }
            }
        }

        private int GetCurrentIteration() {
            string curIterStr = Environment.GetEnvironmentVariable(ITER_ENV_VAR_NAME);
            return curIterStr != null ? int.Parse(curIterStr) : 0;
        }

        public void Cleanup() {

            string desktopPath = "C:/Users/jessica.thomson/Desktop/Cleanup.txt";
            StreamWriter writer = new StreamWriter(desktopPath, true);
            writer.WriteLine("Cleanup was called");
            writer.Close();

            List<string> oldActiveScenes = File.ReadAllLines(Application.persistentDataPath + "/" + TEMP_SCENE_LOCATION).ToList();

            // Enable all scenes which were disabled for the build
            Dictionary<string, EditorBuildSettingsScene> editorBuildScenes = new Dictionary<string, EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
                if (oldActiveScenes.IndexOf( scene.guid.ToString() ) != -1) {
                    scene.enabled = true;
                }
            }

            int newIterationVal = GetCurrentIteration() + 1;
            Environment.SetEnvironmentVariable(ITER_ENV_VAR_NAME, newIterationVal.ToString());
        }

        public void Setup(string rootImageTemplatePath)
        {
            ColorSpace colorSpace;
            BuildTarget buildPlatform;
            RuntimePlatform runtimePlatform;
            GraphicsDeviceType[] graphicsDevices;

            string desktopPath = "C:/Users/jessica.thomson/Desktop/Setup.txt";
            StreamWriter writer = new StreamWriter(desktopPath, true);
            writer.WriteLine("Setup was called");
            writer.Close();

            UnityEditor.EditorPrefs.SetBool("AsynchronousShaderCompilation", false);

            // Figure out if we're preparing to run in Editor playmode, or if we're building to run outside the Editor
            if (IsBuildingForEditorPlaymode)
            {
                SelectIterativeScenesToBuild();
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
            
            foreach( EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;

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

                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                    
                    EditorUtility.DisplayProgressBar($"Baking Test Scenes {(sceneIndex + 1).ToString()}/{totalScenes.ToString()}", $"Baking {sceneAsset.name}", ((float)sceneIndex / totalScenes));

                    Lightmapping.Bake();

                    EditorSceneManagement.EditorSceneManager.SaveScene( currentScene );

                    EditorSceneManagement.EditorSceneManager.SetActiveScene(trScene);

                    EditorSceneManagement.EditorSceneManager.CloseScene(currentScene, true);
                }

                sceneIndex++;
            }
            
            EditorUtility.ClearProgressBar();

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
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
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

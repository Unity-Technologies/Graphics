using UnityEngine;
using UnityEditor.SceneManagement;

namespace UnityEditor.Experimental.Rendering
{
    public class TestFrameworkCustomBuild
    {
        private static readonly string s_TestSceneFolder = "/GraphicsTests/RenderPipeline/LightweightPipeline/Scenes";
        private static readonly string s_BuildFolder = "/TestScenesBuild";

        [MenuItem("RenderPipeline/TestFramework/Build-iOS")]
        public static void BuildiOS()
        {
            TestFrameworkCustomBuild builder = new TestFrameworkCustomBuild();
            builder.Build(BuildTarget.iOS, BuildOptions.AcceptExternalModificationsToPlayer);
        }

        [MenuItem("RenderPipeline/TestFramework/Build-iOS", true)]

        public static bool ValidateBuildiOS()
        {
#if UNITY_STANDALONE_OSX
            return true;
#else
            return false;
#endif
        }

        public void Build(BuildTarget target, BuildOptions options)
        {
            var absoluteScenesPath = Application.dataPath + s_TestSceneFolder;
            string[] levels = System.IO.Directory.GetFiles(absoluteScenesPath, "*.unity", System.IO.SearchOption.AllDirectories);
            CheckAndAddGotoNextSceneBehavior(levels);

            string savePath = EditorUtility.SaveFolderPanel("Select folder to save project", "", "");
            BuildPipeline.BuildPlayer(levels, savePath + s_BuildFolder, target, options);
        }

        private void CheckAndAddGotoNextSceneBehavior(string[] levels)
        {
            for (int i = 0; i < levels.Length; ++i)
            {
                string levelPath = levels[i];
                var scene = EditorSceneManager.OpenScene(levelPath);
                GameObject[] objects = scene.GetRootGameObjects();
                bool componentFound = false;

                foreach (GameObject go in objects)
                {
                    GotoNextScene component = go.GetComponent<GotoNextScene>();
                    if (component != null)
                    {
                        component.m_NextSceneIndex = (i + 1) % levels.Length;
                        componentFound = true;
                        break;
                    }
                }

                if (!componentFound)
                {
                    GameObject gotoNextScene = new GameObject("GotoNextScene");
                    GotoNextScene component = gotoNextScene.AddComponent<GotoNextScene>();
                    component.m_NextSceneIndex = (i + 1) % levels.Length;
                }

                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}

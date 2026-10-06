using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ShaderStrippingAndPrefiltering
{
    class Light2DPrefilteringAnalysisTests
    {
        const string k_MissingScenePath = "Assets/Light2DPrefilteringAnalysisTests_Missing.unity";
        const string k_ExistingScenePath = "Assets/Light2DPrefilteringAnalysisTests_Existing.unity";
        const string k_NotASceneFolderName = "Light2DPrefilteringAnalysisTests_NotAScene.unity";
        const string k_NotASceneFolderPath = "Assets/" + k_NotASceneFolderName;

        EditorBuildSettingsScene[] m_SavedScenes;
        SceneSetup[] m_SavedSceneSetup;

        [SetUp]
        public void SetUp()
        {
            m_SavedScenes = EditorBuildSettings.scenes;
            m_SavedSceneSetup = EditorSceneManager.GetSceneManagerSetup();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                // GetSceneManagerSetup leaves out untitled scenes, so the saved setup can be empty.
                if (m_SavedSceneSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(m_SavedSceneSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            finally
            {
                // Restore project state even if the scene restore throws.
                EditorBuildSettings.scenes = m_SavedScenes;
                AssetDatabase.DeleteAsset(k_ExistingScenePath);
                AssetDatabase.DeleteAsset(k_NotASceneFolderPath);
            }
        }

        static void CreateSceneWithLight2D(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            EditorSceneManager.SaveScene(scene, path);

            // Replace it with an untitled scene so AnalyzeBuildScenes has to open the saved one.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void AnalyzeBuildScenes_SkipsEnabledSceneWhoseFileIsMissing()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(k_MissingScenePath, true)
            };

            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeBuildScenes());
            Assert.IsNotNull(combos);
            Assert.IsEmpty(combos);
        }

        [Test]
        public void AnalyzeBuildScenes_SkipsEnabledEntryWithEmptyPath()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(string.Empty, true)
            };

            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeBuildScenes());
            Assert.IsNotNull(combos);
            Assert.IsEmpty(combos);
        }

        [Test]
        public void AnalyzeBuildScenes_SkipsEnabledEntryThatIsNotASceneAsset()
        {
            // A folder whose name ends in .unity exists in the AssetDatabase but is not a scene.
            AssetDatabase.CreateFolder("Assets", k_NotASceneFolderName);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(k_NotASceneFolderPath, true)
            };

            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeBuildScenes());
            Assert.IsNotNull(combos);
            Assert.IsEmpty(combos);
        }

        [Test]
        public void AnalyzeBuildScenes_StillAnalyzesExistingSceneListedAfterMissingOne()
        {
            CreateSceneWithLight2D(k_ExistingScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(k_MissingScenePath, true),
                new EditorBuildSettingsScene(k_ExistingScenePath, true)
            };

            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeBuildScenes());
            Assert.IsNotNull(combos);
            Assert.IsNotEmpty(combos);
        }
    }
}

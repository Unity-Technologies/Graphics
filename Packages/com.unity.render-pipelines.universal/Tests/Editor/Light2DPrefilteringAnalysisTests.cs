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
        const string k_SecondScenePath = "Assets/Light2DPrefilteringAnalysisTests_Second.unity";
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
                AssetDatabase.DeleteAsset(k_SecondScenePath);
                AssetDatabase.DeleteAsset(k_NotASceneFolderPath);
            }
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
            CreateSceneWithPointLights(k_ExistingScenePath, new[] { 0 });
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

        static void CreatePointLight(int blendStyleIndex, Light2D.OverlapOperation overlapOperation)
        {
            var light = new GameObject("Style " + blendStyleIndex).AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = blendStyleIndex;
            light.overlapOperation = overlapOperation;
        }

        // Saves a new scene with one point light per blend style. Lights use alpha blending unless
        // their style is listed in additiveBlendStyles.
        static void CreateSceneWithPointLights(string path, int[] blendStyleIndices, params int[] additiveBlendStyles)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var blendStyleIndex in blendStyleIndices)
            {
                bool additive = System.Array.IndexOf(additiveBlendStyles, blendStyleIndex) >= 0;
                CreatePointLight(blendStyleIndex, additive ? Light2D.OverlapOperation.Additive : Light2D.OverlapOperation.AlphaBlend);
            }
            EditorSceneManager.SaveScene(scene, path);

            // Replace it with an untitled scene so AnalyzeBuildScenes has to open the saved one.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static string[] AnalyzeBuildScenes(params string[] scenePaths)
        {
            EditorBuildSettings.scenes = System.Array.ConvertAll(scenePaths, path => new EditorBuildSettingsScene(path, true));
            return Light2DPrefilteringAnalysis.AnalyzeBuildScenes();
        }

        static string KeptCombosMessage(string[] combos) => "Kept combos: " + string.Join(" | ", combos);

        // UUM-154779: the runtime ranks blend styles over the visible lights of a layer batch only.
        // If the style 1 light is culled, the style 2 light is drawn with USE_SHAPE_LIGHT_TYPE_1.
        [Test]
        public void AnalyzeBuildScenes_KeepsLowerShapeLightSlot_WhenLowerBlendStyleLightCanBeCulled()
        {
            CreateSceneWithPointLights(k_ExistingScenePath, new[] { 0, 2, 1 }, 0, 1);

            string[] combos = AnalyzeBuildScenes(k_ExistingScenePath);

            // The style 2 light (alpha blend) draws with USE_SHAPE_LIGHT_TYPE_1 when the style 1 light is culled.
            CollectionAssert.Contains(combos, "USE_SHAPE_LIGHT_TYPE_1", KeptCombosMessage(combos));
        }

        // Only the highest style uses alpha blending, so every plain slot must come from that one light.
        [Test]
        public void AnalyzeBuildScenes_KeepsEveryShapeLightSlotUpToHighestRank()
        {
            CreateSceneWithPointLights(k_ExistingScenePath, new[] { 0, 1, 2, 3 }, 0, 1, 2);

            string[] combos = AnalyzeBuildScenes(k_ExistingScenePath);
            string message = KeptCombosMessage(combos);

            for (int slot = 0; slot < 4; slot++)
                CollectionAssert.Contains(combos, "USE_SHAPE_LIGHT_TYPE_" + slot, message);
        }

        // A light kept with DontDestroyOnLoad, or from a scene loaded additively, can share a layer
        // batch with lights from another scene. That raises their rank above the rank in their own scene.
        [Test]
        public void AnalyzeBuildScenes_RanksBlendStylesAcrossAllBuildScenes()
        {
            CreateSceneWithPointLights(k_ExistingScenePath, new[] { 0 }, 0);
            CreateSceneWithPointLights(k_SecondScenePath, new[] { 1 });

            string[] combos = AnalyzeBuildScenes(k_ExistingScenePath, k_SecondScenePath);

            // The style 1 light is alone in its scene, but draws with USE_SHAPE_LIGHT_TYPE_1 next to the style 0 light.
            CollectionAssert.Contains(combos, "USE_SHAPE_LIGHT_TYPE_1", KeptCombosMessage(combos));
        }

        // Slots follow the rank among used blend styles, not the raw index, so unused styles add no slots.
        [Test]
        public void AnalyzeBuildScenes_KeepsSlotsUpToRank_NotUpToRawBlendStyleIndex()
        {
            CreateSceneWithPointLights(k_ExistingScenePath, new[] { 0, 3 });

            string[] combos = AnalyzeBuildScenes(k_ExistingScenePath);
            string message = KeptCombosMessage(combos);

            CollectionAssert.Contains(combos, "USE_SHAPE_LIGHT_TYPE_0", message);
            CollectionAssert.Contains(combos, "USE_SHAPE_LIGHT_TYPE_1", message);
            CollectionAssert.DoesNotContain(combos, "USE_SHAPE_LIGHT_TYPE_2", message);
            CollectionAssert.DoesNotContain(combos, "USE_SHAPE_LIGHT_TYPE_3", message);
        }

        // The runtime never draws a light whose blend style is outside 0-3, so it must not raise other lights' slots.
        [Test]
        public void AnalyzeBuildScenes_IgnoresOutOfRangeBlendStyleWhenRanking()
        {
            CreateSceneWithPointLights(k_ExistingScenePath, new[] { -1, 1 });

            string[] combos = AnalyzeBuildScenes(k_ExistingScenePath);
            string message = KeptCombosMessage(combos);

            CollectionAssert.Contains(combos, "USE_SHAPE_LIGHT_TYPE_0", message);
            CollectionAssert.DoesNotContain(combos, "USE_SHAPE_LIGHT_TYPE_1", message);
        }
    }
}

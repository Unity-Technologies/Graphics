using System.Collections.Generic;
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
        const string k_NoLightScenePath = "Assets/Light2DPrefilteringAnalysisTests_NoLight.unity";
        const string k_PrefabPath = "Assets/Light2DPrefilteringAnalysisTests_Light.prefab";
        const string k_NotASceneFolderName = "Light2DPrefilteringAnalysisTests_NotAScene.unity";
        const string k_NotASceneFolderPath = "Assets/" + k_NotASceneFolderName;
        // A scene shipped in the URP package, so its path is under Packages/.
        const string k_PackageScenePath = "Packages/com.unity.render-pipelines.universal/Editor/SceneTemplates/Basic.unity";
        const string k_ShadowCombo = "USE_SHADOW_MAP,USE_ADDITIVE_BLENDING,USE_SHAPE_LIGHT_TYPE_0";

        EditorBuildSettingsScene[] m_SavedScenes;
        SceneSetup[] m_SavedSceneSetup;
        SerializationMode m_SavedSerializationMode;

        [SetUp]
        public void SetUp()
        {
            m_SavedScenes = EditorBuildSettings.scenes;
            m_SavedSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            m_SavedSerializationMode = EditorSettings.serializationMode;
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
                AssetDatabase.DeleteAsset(k_NoLightScenePath);
                AssetDatabase.DeleteAsset(k_PrefabPath);
                AssetDatabase.DeleteAsset(k_NotASceneFolderPath);

                // Restore after the test assets are gone. Leaving Mixed mode re-serializes only the
                // assets whose format doesn't match, so the rest of the project is untouched.
                if (EditorSettings.serializationMode != m_SavedSerializationMode)
                    EditorSettings.serializationMode = m_SavedSerializationMode;
            }
        }

        [Test]
        public void AnalyzeScenes_SkipsSceneWhoseFileIsMissing()
        {
            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { k_MissingScenePath }));
            Assert.IsNotNull(combos);
            Assert.IsEmpty(combos);
        }

        [Test]
        public void AnalyzeScenes_SkipsEntryWithEmptyPath()
        {
            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { string.Empty, null }));
            Assert.IsNotNull(combos);
            Assert.IsEmpty(combos);
        }

        [Test]
        public void AnalyzeScenes_SkipsEntryThatIsNotASceneAsset()
        {
            // A folder whose name ends in .unity exists in the AssetDatabase but is not a scene.
            AssetDatabase.CreateFolder("Assets", k_NotASceneFolderName);

            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { k_NotASceneFolderPath }));
            Assert.IsNotNull(combos);
            Assert.IsEmpty(combos);
        }

        [Test]
        public void AnalyzeScenes_StillAnalyzesExistingSceneListedAfterMissingOne()
        {
            CreateSceneWithPointLights(k_ExistingScenePath, new[] { 0 });

            string[] combos = null;
            Assert.DoesNotThrow(() => combos = Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { k_MissingScenePath, k_ExistingScenePath }));
            Assert.IsNotNull(combos);
            Assert.IsNotEmpty(combos);
        }

        // The parameterless call reads the Build Profiles list, whose entries can be stale.
        [Test]
        public void AnalyzeBuildScenes_DoesNotThrow_WhenBuildProfilesListHasMissingEmptyAndNonSceneEntries()
        {
            AssetDatabase.CreateFolder("Assets", k_NotASceneFolderName);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(k_MissingScenePath, true),
                new EditorBuildSettingsScene(string.Empty, true),
                new EditorBuildSettingsScene(k_NotASceneFolderPath, true)
            };

            Assert.DoesNotThrow(() => Light2DPrefilteringAnalysis.AnalyzeBuildScenes());
        }

        static Light2D CreatePointLight(int blendStyleIndex, Light2D.OverlapOperation overlapOperation)
        {
            var light = new GameObject("Style " + blendStyleIndex).AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = blendStyleIndex;
            light.overlapOperation = overlapOperation;
            return light;
        }

        // Creates an additive point Light2D on blend style 0 that casts shadows.
        static Light2D CreateShadowLight()
        {
            var light = CreatePointLight(0, Light2D.OverlapOperation.Additive);
            light.shadowsEnabled = true;
            return light;
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

            // Replace it with an untitled scene so the analysis has to open the saved one.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // Saves a scene with one additive point Light2D that casts shadows.
        static void SaveShadowLightScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateShadowLight();
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static void SaveSceneWithoutLight2D(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Not a light");
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // Analyzes only the given scenes. The parameterless AnalyzeBuildScenes also scans every
        // scene under Assets/, so other scenes in the test project would change the result.
        static string[] AnalyzeBuildScenes(params string[] scenePaths)
        {
            return Light2DPrefilteringAnalysis.AnalyzeScenes(scenePaths);
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

        // UUM-160655: BuildPipeline.BuildPlayer can build a scene that is not in the Build Profiles
        // scene list. Its shadowed Light2D must still keep USE_SHADOW_MAP, or the stripper removes
        // the shadow variant and the player draws the light without shadows.
        [Test]
        public void AnalyzeBuildScenes_KeepsShadowCombo_ForSceneBuiltByScriptOutsideBuildProfilesList()
        {
            SaveShadowLightScene(k_ExistingScenePath);

            // The Build Profiles list is empty; the scene is only passed to BuildPlayer.
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];

            // Same scan as AnalyzeBuildScenes, but record the opened scenes. Other scenes in the test
            // project may also keep the shadow combo, so check that this scene was the one scanned.
            var opened = new List<string>();
            string[] combos = Light2DPrefilteringAnalysis.AnalyzeScenes(Light2DPrefilteringAnalysis.GetCandidateScenePaths(), opened);

            CollectionAssert.Contains(opened, k_ExistingScenePath);
            CollectionAssert.Contains(combos, k_ShadowCombo, KeptCombosMessage(combos));
        }

        // Build Profiles scenes outside Assets/ (for example in a package) must still be scanned.
        [Test]
        public void GetCandidateScenePaths_IncludesBuildProfilesSceneUnderPackages()
        {
            Assume.That(AssetDatabase.GetMainAssetTypeAtPath(k_PackageScenePath), Is.EqualTo(typeof(SceneAsset)));
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(k_PackageScenePath, true) };

            CollectionAssert.Contains(new List<string>(Light2DPrefilteringAnalysis.GetCandidateScenePaths()), k_PackageScenePath);
        }

        [Test]
        public void GetCandidateScenePaths_IncludesAssetsSceneNotInBuildProfiles_AndSkipsDisabledEntries()
        {
            SaveSceneWithoutLight2D(k_ExistingScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(k_PackageScenePath, false) };

            var candidates = new List<string>(Light2DPrefilteringAnalysis.GetCandidateScenePaths());

            CollectionAssert.Contains(candidates, k_ExistingScenePath);
            CollectionAssert.DoesNotContain(candidates, k_PackageScenePath);
        }

        // Saves the test assets as text or binary. Binary uses Mixed mode, which saves new assets as
        // binary without re-serializing the project (ForceBinary would rewrite every asset).
        // Switching to ForceText would rewrite every binary asset in the project as text, and
        // restoring Mixed afterwards would not undo it, so the text case only runs in text projects.
        static void UseSceneFormat(bool binary)
        {
            var current = EditorSettings.serializationMode;
            if (binary)
            {
                if (current == SerializationMode.ForceText)
                    EditorSettings.serializationMode = SerializationMode.Mixed;
            }
            else if (current != SerializationMode.ForceText)
            {
                Assert.Ignore("The text case needs a project in Force Text serialization mode.");
            }
        }

        static void AssertSavedFormat(string path, bool binary)
        {
            byte[] header = new byte[5];
            using (var stream = System.IO.File.OpenRead(path))
                stream.Read(header, 0, header.Length);
            bool isText = System.Text.Encoding.ASCII.GetString(header) == "%YAML";
            Assert.AreEqual(!binary, isText, path + " was not saved as " + (binary ? "binary" : "text"));
        }

        // Opening a scene is costly, so a scene that doesn't reference Light2D is skipped. The check
        // must work for text and binary scenes alike.
        [TestCase(false)]
        [TestCase(true)]
        public void AnalyzeScenes_OpensOnlyScenesThatDependOnLight2D(bool binary)
        {
            UseSceneFormat(binary);
            SaveShadowLightScene(k_ExistingScenePath);
            SaveSceneWithoutLight2D(k_NoLightScenePath);
            AssertSavedFormat(k_ExistingScenePath, binary);
            AssertSavedFormat(k_NoLightScenePath, binary);

            var opened = new List<string>();
            string[] combos = Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { k_NoLightScenePath, k_ExistingScenePath }, opened);

            CollectionAssert.AreEqual(new[] { k_ExistingScenePath }, opened);
            CollectionAssert.Contains(combos, k_ShadowCombo, KeptCombosMessage(combos));
        }

        // The recursive dependency check must find a Light2D that only lives in a prefab instance.
        [TestCase(false)]
        [TestCase(true)]
        public void AnalyzeScenes_OpensScene_WhenLight2DIsOnlyInAPrefabInstance(bool binary)
        {
            UseSceneFormat(binary);

            var prefabSource = CreateShadowLight().gameObject;
            var prefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, k_PrefabPath);
            Object.DestroyImmediate(prefabSource);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, k_ExistingScenePath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssertSavedFormat(k_ExistingScenePath, binary);

            var opened = new List<string>();
            string[] combos = Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { k_ExistingScenePath }, opened);

            CollectionAssert.AreEqual(new[] { k_ExistingScenePath }, opened);
            CollectionAssert.Contains(combos, k_ShadowCombo, KeptCombosMessage(combos));
        }

        // A loaded scene can gain its first Light2D after its last save. Its saved file doesn't
        // reference Light2D yet, but the build includes the light once the user saves the scene.
        [Test]
        public void AnalyzeScenes_KeepsCombo_ForUnsavedLight2DInLoadedScene()
        {
            SaveSceneWithoutLight2D(k_NoLightScenePath);
            var scene = EditorSceneManager.OpenScene(k_NoLightScenePath, OpenSceneMode.Single);
            CreateShadowLight();
            // Adding a component from script doesn't dirty the scene; the editor's Undo path does.
            EditorSceneManager.MarkSceneDirty(scene);

            var opened = new List<string>();
            string[] combos = Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { k_NoLightScenePath }, opened);

            // The loaded scene is scanned in place, not opened again.
            CollectionAssert.IsEmpty(opened);
            CollectionAssert.Contains(combos, k_ShadowCombo, KeptCombosMessage(combos));
        }

        // A scene listed twice (in Build Profiles and under Assets/) is opened once.
        [Test]
        public void AnalyzeScenes_OpensSceneOnce_WhenListedTwice()
        {
            SaveShadowLightScene(k_ExistingScenePath);

            var opened = new List<string>();
            Light2DPrefilteringAnalysis.AnalyzeScenes(new[] { k_ExistingScenePath, k_ExistingScenePath }, opened);

            Assert.AreEqual(1, opened.Count, string.Join(" | ", opened));
        }
    }
}

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.DummyPipeline;
using UnityEditor.SceneManagement;

namespace UnityEditor.Rendering.Tests
{
    public class BuildTargetExtensionsTests
    {
        const string k_ScenePathFormat = "Assets/Tests/Editor/ResourcesForBuildTargetTests/{0}.unity";
        const string k_AssetPathFormat = "Assets/Tests/Editor/ResourcesForBuildTargetTests/PipelineAssets/{0}.asset";
        const string k_Label = "TakeMeOnBuild";
        const string k_SceneWithoutRefName = "WithoutReference";
        const string k_SceneWithRefName1 = "WithReference1";
        const string k_SceneWithRefName2 = "WithReference2";
        const string k_SceneWithRefAlreadyUsedName = "WithReferenceAlreadyUsedInGraphics";

        EditorBuildSettingsScene[] m_BuildScenes;
        IncludeAdditionalRPAssets m_IncluderSettings;
        (bool addFromScene, bool addFromLabel, string label) m_FormerInclusionData;
        (String graphicPath, String[] qualityPaths) m_FormerQualityData;

        class ReferenceRPAssetComponent : MonoBehaviour
        {
            public RenderPipelineAsset m_ReferencedAsset;
        }

        public class TestCases : IEnumerable
        {
            static readonly (string name, (bool addFromScene, bool addFromLabel, string label) setup)[] includerSetups = new[]
            {
                ("NoInclusion", (false, false, "random")),
                ("NoInclusionButWithLabelSetUp", (false, false, k_Label)),
                ("OnlyScene", (true, false, "random")),
                ("OnlySceneButWithLabelSetUp", (true, false, k_Label)),
                ("OnlyLabel", (false, true, k_Label)),
                ("BothLabelAndScene", (false, true, k_Label)),
            };
            static readonly (string name, (String path, bool included)[] setup, int additionalExpectation)[] buildSceneSetups = new[]
            {
                ("EmptyBuildScenes", new (String path, bool included)[0], 0),
                ("ActiveSceneWithoutRef", new[]{ (String.Format(k_ScenePathFormat, k_SceneWithoutRefName), true) }, 0),
                ("InactiveSceneWithoutRef", new[]{ (String.Format(k_ScenePathFormat, k_SceneWithoutRefName), false) }, 0),
                ("ActiveSceneWithRef", new[]{ (String.Format(k_ScenePathFormat, k_SceneWithRefName1), true) }, 1),
                ("InactiveSceneWithRef", new[]{ (String.Format(k_ScenePathFormat, k_SceneWithRefName1), false) }, 0),
                ("TwoSceneBothWithDifferentRef", new[]{ (String.Format(k_ScenePathFormat, k_SceneWithRefName1), true), (String.Format(k_ScenePathFormat, k_SceneWithRefName2), true) }, 2),
                ("TwoSceneBothWithSameRef", new[]{ (String.Format(k_ScenePathFormat, k_SceneWithRefName1), true), (String.Format(k_ScenePathFormat, k_SceneWithRefName1), true) }, 1),
                ("TwoSceneOneWithRefAlreadyUsed", new[]{ (String.Format(k_ScenePathFormat, k_SceneWithoutRefName), true), (String.Format(k_ScenePathFormat, k_SceneWithRefAlreadyUsedName), true) }, 0),
            };
            static readonly (string name, (String graphicPath, String[] qualityPaths) setup, int baseExpectation)[] qualitySetups = new[]
            {
                ("NoRPAssetAnywhere", (null, new string[0]), 0),
                ("OnlyGraphicsRPAsset", (String.Format(k_AssetPathFormat, 0), new string[0]), 1),
                ("OnlyQualityRPAsset", (null, new[]
                    {
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 1),
                        String.Format(k_AssetPathFormat, 2),
                        String.Format(k_AssetPathFormat, 3),
                        String.Format(k_AssetPathFormat, 4),
                        String.Format(k_AssetPathFormat, 5)
                    }), 6),
                ("AllSameRPAsset", (String.Format(k_AssetPathFormat, 0), new[]
                    {
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0)
                    }), 1),
                ("AllQualitySameGraphicsDifferentRPAsset", (String.Format(k_AssetPathFormat, 1), new[]
                    {
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0),
                        String.Format(k_AssetPathFormat, 0)
                    }), 1),
                ("SomeSameAsGraphicsRPAsset", (String.Format(k_AssetPathFormat, 0), new[]
                    {
                        String.Format(k_AssetPathFormat, 0)
                    }), 1),
                ("SomeDifferentThanGraphicsRPAssets", (String.Format(k_AssetPathFormat, 0), new[]
                    {
                        String.Format(k_AssetPathFormat, 1),
                        String.Format(k_AssetPathFormat, 2)
                    }), 3),
            };

            public IEnumerator GetEnumerator()
            {
                for (int a = 0; a < includerSetups.Length; ++a)
                    for (int c = 0; c < buildSceneSetups.Length; ++c)
                        for (int d = 0; d < qualitySetups.Length; ++d)
                        {
                            int expected = qualitySetups[d].baseExpectation;
                            if (d > 0)
                            {
                                // d == 0 means we are in built-in (No asset setup in graphics nor quality settings)
                                expected += includerSetups[a].setup.addFromScene ? buildSceneSetups[c].additionalExpectation : 0; // adding from scene dependence
                                expected += includerSetups[a].setup.addFromLabel ? includerSetups[a].setup.addFromScene ? 1 : 2 : 0; // adding from labels
                            }
                            yield return new TestCaseData(includerSetups[a].setup, buildSceneSetups[c].setup, qualitySetups[d].setup)
                                .SetName($"Case: {includerSetups[a].name} {buildSceneSetups[c].name} {qualitySetups[d].name}")
                                .Returns(expected);
                        }
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var activeBuildTargetGroupName = activeBuildTargetGroup.ToString();
            var qualityAmount = QualitySettings.GetActiveQualityLevelsForPlatformCount(activeBuildTargetGroupName);
            Assert.AreEqual(qualityAmount, 6, "Some part of the test require that we override all quality. If the amount change, check the tests.");

            m_BuildScenes = EditorBuildSettings.scenes;
            EditorSceneManager.SetActiveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single));

            //register quality setup for cleaning, and force everything to null
            m_FormerQualityData = (GraphicsSettings.defaultRenderPipeline == null ? null : AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline), new string[qualityAmount]);
            QualitySettings.ForEach((i, name) =>
            {
                m_FormerQualityData.qualityPaths[i] = QualitySettings.renderPipeline == null ? null : AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
            });
            
            //register IncludeAdditionalRPAsset setup for cleaning (Dummy should be in use)
            QualitySettings.renderPipeline = LoadAsset(String.Format(k_AssetPathFormat, 0));
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<DummyPipeline>(ScriptableObject.CreateInstance<DummyGlobals>());
            m_IncluderSettings = GraphicsSettings.GetRenderPipelineSettings<IncludeAdditionalRPAssets>();
            Assert.IsNotNull(m_IncluderSettings);
            m_FormerInclusionData = (m_IncluderSettings.includeReferencedInScenes, m_IncluderSettings.includeAssetsByLabel, m_IncluderSettings.labelToInclude);
            QualitySettings.renderPipeline = LoadAsset(m_FormerQualityData.qualityPaths[QualitySettings.GetQualityLevel()]);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorBuildSettings.scenes = m_BuildScenes;
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<DummyPipeline>(null);
        }

        [TearDown]
        public void TearDown()
        {
            SetUpInclusion(m_FormerInclusionData);
            SetUpQuality(m_FormerQualityData);
        }
        
        [SetUp]
        public void SetUp()
        {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
        }
        
        private RenderPipelineAsset LoadAsset(string renderPipelineAssetPath)
        {
            if (string.IsNullOrEmpty(renderPipelineAssetPath))
                return null;

            var asset = AssetDatabase.LoadMainAssetAtPath(renderPipelineAssetPath);
            Assert.IsNotNull(asset, renderPipelineAssetPath);
            return asset as RenderPipelineAsset;
        }
        
        void SetBuildSettingsScene((String path, bool included)[] scenePaths)
        {
            var buildSettingsScene = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; ++i)
                buildSettingsScene[i] = new EditorBuildSettingsScene(scenePaths[i].path, scenePaths[i].included);
            EditorBuildSettings.scenes = buildSettingsScene;
        }

        void SetUpInclusion((bool addFromScene, bool addFromLabel, string label) includer)
        {
            m_IncluderSettings.includeReferencedInScenes = includer.addFromScene;
            m_IncluderSettings.includeAssetsByLabel = includer.addFromLabel;
            m_IncluderSettings.labelToInclude = includer.label;
        }

        void SetUpQuality((String graphicPath, String[] qualityPaths) qualitySetUp)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(qualitySetUp.graphicPath);
            QualitySettings.ForEach((i, name) =>
            {
                QualitySettings.renderPipeline = i < qualitySetUp.qualityPaths.Length ? LoadAsset(qualitySetUp.qualityPaths[i]) : null;
            });
        }
        
        [Test, TestCaseSource(typeof(TestCases))]
        public int GetRenderPipelineAssets(
            (bool addFromScene, bool addFromLabel, string label) inclusion,
            (String path, bool included)[] buildScenes,
            (String graphicPath, String[] qualityPaths) qualitySetUp)
        {
            SetUpQuality(qualitySetUp);
            SetBuildSettingsScene(buildScenes);
            SetUpInclusion(inclusion);

            using (ListPool<RenderPipelineAsset>.Get(out var rpAssets))
            {
                var result = EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets(rpAssets);
                Assert.AreEqual(rpAssets.Count != 0, result);
                return rpAssets.Count;
            }
        }

        [Test]
        [Description("https://jira.unity3d.com/browse/UUM-19235")]
        public void ThereAreNotQualityLevelsForTheCurrentPlatformAndOnlyTheAssetInGraphicsSettingsIsOverriden()
        {
            string assetPath = String.Format(k_AssetPathFormat, 0);
            GraphicsSettings.defaultRenderPipeline = LoadAsset(assetPath);

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var activeBuildTargetGroupName = activeBuildTargetGroup.ToString();

            // Disable all qualities
            var includedLevels = QualitySettings.GetActiveQualityLevelsForPlatform(activeBuildTargetGroupName);
            foreach (var level in includedLevels)
            {
                QualitySettings.TryExcludePlatformAt(activeBuildTargetGroupName,level, out var _);
            }

            using (ListPool<RenderPipelineAsset>.Get(out var rpAssets))
            {
                Assert.IsTrue(buildTarget.TryGetRenderPipelineAssets<RenderPipelineAsset>(rpAssets));
                Assert.AreEqual(1, rpAssets.Count);
                Assert.AreEqual(assetPath, AssetDatabase.GetAssetPath(rpAssets.First()));
            }

            // Restore
            foreach (var level in includedLevels)
            {
                QualitySettings.TryIncludePlatformAt(activeBuildTargetGroupName,level, out var _);
            }
        }

        [Test]
        public void GetRenderPipelineAssets_ReturnsFalseWhenNullListIsGiven()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            Assert.IsFalse(buildTarget.TryGetRenderPipelineAssets<RenderPipelineAsset>(null));
        }

        [Test]
        public void GetRenderPipelineAssets_ReturnsFalseWhenThereAreNoAssetsDefined()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var assetsList = new List<RenderPipelineAsset>();
            Assert.IsFalse(buildTarget.TryGetRenderPipelineAssets<RenderPipelineAsset>(assetsList));
            Assert.AreEqual(0, assetsList.Count);
        }
    }
}

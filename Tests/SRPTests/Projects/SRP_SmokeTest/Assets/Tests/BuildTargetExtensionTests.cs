using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    public class BuildTargetExtensionsTests
    {
        RenderPipelineAsset m_GraphicsSettingsRPAsset = null;
        RenderPipelineAsset[] m_RenderPipelineAssets = null;
        int m_CurrentQualityLevel = -1;
        [SetUp]
        public void SetUp()
        {
            m_GraphicsSettingsRPAsset = GraphicsSettings.defaultRenderPipeline;
            m_CurrentQualityLevel = QualitySettings.GetQualityLevel();
            m_RenderPipelineAssets = new RenderPipelineAsset[QualitySettings.count];
            QualitySettings.ForEach((tier, name) =>
            {
                m_RenderPipelineAssets[tier] = QualitySettings.renderPipeline;
            });
        }

        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_GraphicsSettingsRPAsset;
            QualitySettings.ForEach((tier, name) =>
            {
                QualitySettings.renderPipeline = m_RenderPipelineAssets[tier];
            });
            QualitySettings.SetQualityLevel(m_CurrentQualityLevel);
        }

        static TestCaseData[] s_ListTestsCaseDatas =
        {
            new TestCaseData(string.Empty, Array.Empty<(int, string)>())
                .SetName("When Graphics and Quality Settings do not contain RP assets defined, the method returns 0 RenderPipelineAssets")
                .Returns(0),
            new TestCaseData("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset", Array.Empty<(int, string)>())
                .SetName("When Graphics fallbacks to URP and the quality levels do not override the RP Asset, just the Graphics RP Asset is returned")
                .Returns(1),
            new TestCaseData(string.Empty, new (int, string)[]
                {
                    (0, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset")
                })
                .SetName("When just a Quality Level overrides a RP asset, just that RP asset is returned")
                .Returns(1),
            new TestCaseData("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset", new (int, string)[]
                {
                    (0, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset")
                })
                .SetName("When just a Quality Level overrides a RP asset and Graphics fallbacks to one, just those two are returned")
                .Returns(2),
            new TestCaseData("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset", new (int, string)[]
                {
                    (0, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset"),
                    (1, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset"),
                    (2, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset"),
                    (3, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset"),
                    (4, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset"),
                    (5, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset")
                })
                .SetName("When all the quality levels override the same RP asset, RP asset is not included, and just 1 asset from quality is returned besides the one on graphics")
                .Returns(2),
            new TestCaseData("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset", new (int, string)[]
                {
                    (0, "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset"),
                    (1, "Assets/PipelineAssets/UniversalRenderPipelineAsset 1.asset"),
                    (2, "Assets/PipelineAssets/UniversalRenderPipelineAsset 2.asset"),
                    (3, "Assets/PipelineAssets/UniversalRenderPipelineAsset 3.asset"),
                    (4, "Assets/PipelineAssets/UniversalRenderPipelineAsset 4.asset"),
                    (5, "Assets/PipelineAssets/UniversalRenderPipelineAsset 5.asset")
                })
                .SetName("When all the quality levels override different assets all of them are returned")
                .Returns(6)
        };

        private RenderPipelineAsset LoadAsset(string renderPipelineAssetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(renderPipelineAssetPath);
            if (!string.IsNullOrEmpty(renderPipelineAssetPath))
                Assert.IsNotNull(asset, renderPipelineAssetPath);
            return asset as RenderPipelineAsset;
        }

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatas))]
        public int GetRenderPipelineAssets(string renderPipelineAssetPath, (int, string)[] qualityLevels)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(renderPipelineAssetPath);

            foreach ((int tier, string path) level in qualityLevels)
            {
                QualitySettings.SetQualityLevel(level.tier);
                QualitySettings.renderPipeline = LoadAsset(level.path);
            }

            using (ListPool<RenderPipelineAsset>.Get(out var rpAssets))
            {
                Assert.IsTrue(EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets<RenderPipelineAsset>(rpAssets));
                return rpAssets.Count;
            }
        }

        [Test]
        [Description("https://jira.unity3d.com/browse/UUM-19235")]
        public void ThereAreNotQualityLevelsForTheCurrentPlatformAndOnlyTheAssetInGraphicsSettingsIsOverriden()
        {
            const string assetPath = "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset";
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
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;

namespace UnityEditor.Rendering.Universal.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class RenderSettingsConverterTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (QualitySettings.names.Length == 0)
                Assert.Ignore("Project without quality settings. Skipping test");
        }

        RenderPipelineAsset m_RenderPipelineAsset;
        int m_QualityLevel = -1;
        string m_DefaultFolder = string.Empty;
        string m_QualityLevelName = string.Empty;

        [SetUp]
        public void Setup()
        {
            m_RenderPipelineAsset = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = null;
            m_QualityLevel = QualitySettings.GetQualityLevel();
            m_QualityLevelName = QualitySettings.names[m_QualityLevel];

            m_DefaultFolder = UniversalProjectSettings.projectSettingsFolderPath;
            UniversalProjectSettings.projectSettingsFolderPath = nameof(RenderSettingsConverterTests);
        }

        [TearDown]
        public void TearDown()
        {
            var path = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}";
            if (AssetDatabase.AssetPathExists(path))
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
            }

            QualitySettings.renderPipeline = m_RenderPipelineAsset;
            UniversalProjectSettings.projectSettingsFolderPath = m_DefaultFolder;
        }

        [Test]
        public void WhenRunningTheConverter_TheCurrent_QualityLevel_IsNowURP_AndHasEverythingProperlyAssigned()
        {
            var renderSettingsConverter = new RenderSettingsConverter();
            var expectedEntry = $"[{m_QualityLevel}] {m_QualityLevelName}";

            InitializeConverterContext ctx = new() { items = new() };
            renderSettingsConverter.OnInitialize(ctx, null);

            Assert.AreEqual(m_QualityLevel, QualitySettings.GetQualityLevel(), "Initialization did not rollback quality level");

            ConverterItemDescriptor? desc = null;

            foreach (var item in ctx.items)
            {
                if (item.name.Equals(expectedEntry))
                {
                    desc = item;
                    break;
                }
            }

            Assert.IsTrue(desc.HasValue, "Initialization did not found the item to convert");

            RunItemContext runItemContext = new RunItemContext(new ConverterItemInfo
            {
                descriptor = desc.Value,
                index = 0,
            });

            renderSettingsConverter.OnRun(ref runItemContext);

            Assert.AreEqual(m_QualityLevel, QualitySettings.GetQualityLevel(), "Run did not rollback quality level");

            // ---------- ASSET REFERENCE CHECK  ----------
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            Assert.IsNotNull(urpAsset, "URP asset is not assigned");
            Assert.AreEqual($"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/{m_QualityLevelName}.asset", AssetDatabase.GetAssetPath(urpAsset));

            // ---------- RENDERER REFERENCE CHECK ----------

            Assert.IsNotNull(urpAsset.m_RendererDataList);
            Assert.AreEqual(1, urpAsset.m_RendererDataList.Length);
            Assert.AreEqual($"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/Default_Forward_Renderer.asset", AssetDatabase.GetAssetPath(urpAsset.m_RendererDataList[0]));

            // ---------- SETTINGS FORWARDING CHECK ----------

            var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier3);

            Assert.AreEqual(QualitySettings.softParticles, urpAsset.supportsCameraDepthTexture, "supportsCameraDepthTexture mismatch");
            Assert.AreEqual(tier.hdr, urpAsset.supportsHDR, "supportsHDR mismatch");
            Assert.AreEqual(QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing, urpAsset.msaaSampleCount, "msaaSampleCount mismatch");

            var expectedMainLightMode = QualitySettings.pixelLightCount == 0 ? LightRenderingMode.Disabled : LightRenderingMode.PerPixel;
            Assert.AreEqual(expectedMainLightMode, urpAsset.mainLightRenderingMode, "mainLightRenderingMode mismatch");

            Assert.AreEqual(QualitySettings.shadows != ShadowQuality.Disable, urpAsset.supportsMainLightShadows, "supportsMainLightShadows mismatch");
            Assert.AreEqual(RenderSettingsConverter.GetEquivalentMainlightShadowResolution((int)QualitySettings.shadowResolution), urpAsset.mainLightShadowmapResolution, "mainLightShadowmapResolution mismatch");

            var expectedAdditionalLightsMode = QualitySettings.pixelLightCount == 0 ? LightRenderingMode.PerVertex : LightRenderingMode.PerPixel;
            Assert.AreEqual(expectedAdditionalLightsMode, urpAsset.additionalLightsRenderingMode, "additionalLightsRenderingMode mismatch");
            Assert.AreEqual(QualitySettings.pixelLightCount != 0 ? Mathf.Max(0, QualitySettings.pixelLightCount) : 4, urpAsset.maxAdditionalLightsCount, "maxAdditionalLightsCount mismatch");
            Assert.AreEqual(QualitySettings.shadows != ShadowQuality.Disable, urpAsset.supportsAdditionalLightShadows, "supportsAdditionalLightShadows mismatch");
            Assert.AreEqual(RenderSettingsConverter.GetEquivalentAdditionalLightAtlasShadowResolution((int)QualitySettings.shadowResolution), urpAsset.additionalLightsShadowmapResolution, "additionalLightsShadowmapResolution mismatch");

            Assert.AreEqual(tier.reflectionProbeBlending, urpAsset.reflectionProbeBlending, "reflectionProbeBlending mismatch");
            Assert.AreEqual(tier.reflectionProbeBoxProjection, urpAsset.reflectionProbeBoxProjection, "reflectionProbeBoxProjection mismatch");

            Assert.AreEqual(QualitySettings.shadowDistance, urpAsset.shadowDistance, "shadowDistance mismatch");
            var expectedCascadeCount = tier.cascadedShadowMaps ? QualitySettings.shadowCascades : 1;
            Assert.AreEqual(expectedCascadeCount, urpAsset.shadowCascadeCount, "shadowCascadeCount mismatch");
            Assert.AreEqual(QualitySettings.shadowCascade2Split, urpAsset.cascade2Split, "cascade2Split mismatch");
            Assert.AreEqual(QualitySettings.shadowCascade4Split, urpAsset.cascade4Split, "cascade4Split mismatch");
            Assert.AreEqual(QualitySettings.shadows == ShadowQuality.All, urpAsset.supportsSoftShadows, "supportsSoftShadows mismatch");

            // ---------- RENDERER FORWARDING CHECK ----------
            Assert.AreEqual(1, urpAsset.m_RendererDataList.Length);
            Assert.IsNotNull(urpAsset.m_RendererDataList[0]);
        }
    }

}

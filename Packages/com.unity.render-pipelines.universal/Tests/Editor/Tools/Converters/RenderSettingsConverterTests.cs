using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

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
        RenderPipelineAsset m_DefaultRenderPipeline;
        int m_QualityLevel = -1;
        string m_DefaultFolder = string.Empty;
        string m_QualityLevelName = string.Empty;

        [SetUp]
        public void Setup()
        {
            m_RenderPipelineAsset = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = null;
            m_DefaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
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
            GraphicsSettings.defaultRenderPipeline = m_DefaultRenderPipeline;
            UniversalProjectSettings.projectSettingsFolderPath = m_DefaultFolder;
        }

        [Test]
        public void WhenRunningBuiltInToURP3DRenderSettingsConverter_TheCurrent_QualityLevel_IsNowURP_AndHasEverythingProperlyAssigned()
        {
            var renderSettingsConverter = new BuiltInToURP3DRenderSettingsConverter();
            var expectedEntry = $"[{m_QualityLevel}] {m_QualityLevelName}";

            void OnScanFinished(List<IRenderPipelineConverterItem> scanItems)
            {
                Assert.IsTrue(scanItems.Count > 0, "Initialization did not found the item to convert");

                // Find the Quality folder (RenderSettingsConverter returns Graphics and Quality folders)
                IFolderRenderPipelineConverterItem qualityFolder = null;
                foreach (var item in scanItems)
                {
                    if (item is IFolderRenderPipelineConverterItem folder && folder.name == "Quality")
                    {
                        qualityFolder = folder;
                        break;
                    }
                }

                Assert.IsNotNull(qualityFolder, "Quality folder not found in scan results");
                Assert.IsTrue(qualityFolder.children.Count > m_QualityLevel, $"Quality level {m_QualityLevel} out of range for {qualityFolder.children.Count} quality levels");

                Assert.AreEqual(m_QualityLevel, QualitySettings.GetQualityLevel(), "Initialization did not rollback quality level");

                var status = renderSettingsConverter.Convert(qualityFolder.children[m_QualityLevel], out string msg);
                Assert.IsTrue(status != Status.Error, msg);

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
#pragma warning disable CS0618 // Type or member is obsolete
                var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier3);
#pragma warning restore CS0618

#pragma warning disable CS0618 // Type or member is obsolete
                Assert.AreEqual(QualitySettings.softParticles, urpAsset.supportsCameraDepthTexture, "supportsCameraDepthTexture mismatch");
#pragma warning restore CS0618
                Assert.AreEqual(tier.hdr, urpAsset.supportsHDR, "supportsHDR mismatch");
                Assert.AreEqual(QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing, urpAsset.msaaSampleCount, "msaaSampleCount mismatch");

#pragma warning disable CS0618 // Type or member is obsolete
                var expectedMainLightMode = QualitySettings.pixelLightCount == 0 ? LightRenderingMode.Disabled : LightRenderingMode.PerPixel;
                Assert.AreEqual(expectedMainLightMode, urpAsset.mainLightRenderingMode, "mainLightRenderingMode mismatch");

                Assert.AreEqual(QualitySettings.shadows != UnityEngine.ShadowQuality.Disable, urpAsset.supportsMainLightShadows, "supportsMainLightShadows mismatch");
                Assert.AreEqual(BuiltInToURP3DRenderSettingsConverter.GetEquivalentMainlightShadowResolution((int)QualitySettings.shadowResolution), urpAsset.mainLightShadowmapResolution, "mainLightShadowmapResolution mismatch");

                var expectedAdditionalLightsMode = QualitySettings.pixelLightCount == 0 ? LightRenderingMode.PerVertex : LightRenderingMode.PerPixel;
                Assert.AreEqual(expectedAdditionalLightsMode, urpAsset.additionalLightsRenderingMode, "additionalLightsRenderingMode mismatch");
                Assert.AreEqual(QualitySettings.pixelLightCount != 0 ? Mathf.Max(0, QualitySettings.pixelLightCount) : 4, urpAsset.maxAdditionalLightsCount, "maxAdditionalLightsCount mismatch");
                Assert.AreEqual(QualitySettings.shadows != UnityEngine.ShadowQuality.Disable, urpAsset.supportsAdditionalLightShadows, "supportsAdditionalLightShadows mismatch");
                Assert.AreEqual(BuiltInToURP3DRenderSettingsConverter.GetEquivalentAdditionalLightAtlasShadowResolution((int)QualitySettings.shadowResolution), urpAsset.additionalLightsShadowmapResolution, "additionalLightsShadowmapResolution mismatch");

                Assert.AreEqual(tier.reflectionProbeBlending, urpAsset.reflectionProbeBlending, "reflectionProbeBlending mismatch");
                Assert.AreEqual(tier.reflectionProbeBoxProjection, urpAsset.reflectionProbeBoxProjection, "reflectionProbeBoxProjection mismatch");

                ShadowsVolumeOverride shadows = null;
                Assert.IsTrue(urpAsset.volumeProfile != null && urpAsset.volumeProfile.TryGet(out shadows), "The quality volume profile has no Shadows override");
                Assert.AreEqual(QualitySettings.shadowDistance, shadows.maxShadowDistance.value, "maxShadowDistance mismatch");
                var expectedCascadeCount = tier.cascadedShadowMaps ? QualitySettings.shadowCascades : 1;
                Assert.AreEqual(expectedCascadeCount, shadows.cascadeCount.value, "cascadeCount mismatch");
                var expectedSplit0 = expectedCascadeCount == 2 ? QualitySettings.shadowCascade2Split : QualitySettings.shadowCascade4Split.x;
                Assert.AreEqual(expectedSplit0, shadows.cascadeSplit0.value, "cascadeSplit0 mismatch");
                Assert.AreEqual(QualitySettings.shadowCascade4Split.y, shadows.cascadeSplit1.value, "cascadeSplit1 mismatch");
                Assert.AreEqual(QualitySettings.shadowCascade4Split.z, shadows.cascadeSplit2.value, "cascadeSplit2 mismatch");
                Assert.AreEqual(QualitySettings.shadows == UnityEngine.ShadowQuality.All, urpAsset.supportsSoftShadows, "supportsSoftShadows mismatch");
#pragma warning restore CS0618

                // ---------- RENDERER FORWARDING CHECK ----------
                Assert.AreEqual(1, urpAsset.m_RendererDataList.Length);
                Assert.IsNotNull(urpAsset.m_RendererDataList[0]);
            }

            renderSettingsConverter.Scan(OnScanFinished);
        }

        [Test]
        public void WhenRunningBuiltInToURP3DRenderSettingsConverter_Again_TheShadowsOverride_FollowsTheCurrentQualitySettings()
        {
            var renderSettingsConverter = new BuiltInToURP3DRenderSettingsConverter();
#pragma warning disable CS0618 // Type or member is obsolete
            float originalShadowDistance = QualitySettings.shadowDistance;

            void OnScanFinished(List<IRenderPipelineConverterItem> scanItems)
            {
                IFolderRenderPipelineConverterItem qualityFolder = null;
                foreach (var item in scanItems)
                {
                    if (item is IFolderRenderPipelineConverterItem folder && folder.name == "Quality")
                    {
                        qualityFolder = folder;
                        break;
                    }
                }

                Assert.IsNotNull(qualityFolder, "Quality folder not found in scan results");
                var qualityItem = qualityFolder.children[m_QualityLevel];

                VolumeProfile firstProfile;
                try
                {
                    QualitySettings.shadowDistance = 30f;
                    Assert.IsTrue(renderSettingsConverter.Convert(qualityItem, out string firstMessage) != Status.Error, firstMessage);
                    firstProfile = ((UniversalRenderPipelineAsset)QualitySettings.renderPipeline).volumeProfile;

                    QualitySettings.shadowDistance = 70f;
                    QualitySettings.renderPipeline = null;
                    LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape($"a copy of '{Link(firstProfile)}', so later changes to '{Link(firstProfile)}' no longer apply to it")));
                    Assert.IsTrue(renderSettingsConverter.Convert(qualityItem, out string secondMessage) != Status.Error, secondMessage);
                }
                finally
                {
                    QualitySettings.shadowDistance = originalShadowDistance;
#pragma warning restore CS0618
                }

                var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                Assert.IsNotNull(urpAsset, "URP asset is not assigned");
                Assert.AreNotSame(firstProfile, urpAsset.volumeProfile, "The second conversion must give the pipeline asset a copy of its volume profile");
                Assert.AreEqual(30f, ShadowDistanceIn(firstProfile), "The second conversion must not change the previous volume profile");

                ShadowsVolumeOverride shadows = null;
                Assert.IsTrue(urpAsset.volumeProfile != null && urpAsset.volumeProfile.TryGet(out shadows), "The quality volume profile has no Shadows override");
                Assert.AreEqual(70f, shadows.maxShadowDistance.value, "The second conversion must use the current shadow distance");
                Assert.IsTrue(shadows.maxShadowDistance.overrideState);
            }

            renderSettingsConverter.Scan(OnScanFinished);
        }

        static string Link(Object asset) =>
            $"<link=\"srpasset='{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset))}'\"><color={(EditorGUIUtility.isProSkin ? "#40a0ff" : "#0000FF")}><u>{asset.name}</u></color></link>";

        static float ShadowDistanceIn(VolumeProfile profile)
        {
            Assert.IsTrue(profile.TryGet(out ShadowsVolumeOverride shadows), $"'{profile.name}' has no Shadows override");
            return shadows.maxShadowDistance.value;
        }

        IRenderPipelineConverterItem FindCurrentQualityItem(BuiltInToURP3DRenderSettingsConverter converter)
        {
            IRenderPipelineConverterItem qualityItem = null;
            converter.Scan(scanItems =>
            {
                foreach (var item in scanItems)
                {
                    if (item is IFolderRenderPipelineConverterItem folder && folder.name == "Quality")
                        qualityItem = folder.children[m_QualityLevel];
                }
            });

            Assert.IsNotNull(qualityItem, "The current quality level was not found in the scan results");
            return qualityItem;
        }

        UniversalRenderPipelineAsset ConvertCurrentQualityLevel(BuiltInToURP3DRenderSettingsConverter converter, IRenderPipelineConverterItem qualityItem, float shadowDistance)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            float originalShadowDistance = QualitySettings.shadowDistance;
            try
            {
                QualitySettings.shadowDistance = shadowDistance;
                QualitySettings.renderPipeline = null;
                Assert.IsTrue(converter.Convert(qualityItem, out string message) != Status.Error, message);
            }
            finally
            {
                QualitySettings.shadowDistance = originalShadowDistance;
#pragma warning restore CS0618
            }

            return QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        }

        [Test]
        public void WhenRunningBuiltInToURP3DRenderSettingsConverter_Again_AVolumeProfileSharedWithAnotherPipelineAsset_IsNotChanged()
        {
            var renderSettingsConverter = new BuiltInToURP3DRenderSettingsConverter();
            var qualityItem = FindCurrentQualityItem(renderSettingsConverter);

            var urpAsset = ConvertCurrentQualityLevel(renderSettingsConverter, qualityItem, 30f);
            var sharedProfile = urpAsset.volumeProfile;

            var otherAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            otherAsset.volumeProfile = sharedProfile;
            AssetDatabase.CreateAsset(otherAsset, $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/Other.asset");

            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape($"a copy of '{Link(sharedProfile)}', so later changes to '{Link(sharedProfile)}' no longer apply to it. '{Link(sharedProfile)}' is still used by '{Link(otherAsset)}'.")));
            urpAsset = ConvertCurrentQualityLevel(renderSettingsConverter, qualityItem, 70f);

            Assert.AreSame(sharedProfile, otherAsset.volumeProfile, "The other pipeline asset must keep the shared volume profile");
            Assert.AreEqual(30f, ShadowDistanceIn(sharedProfile), "The conversion must not change a volume profile that another pipeline asset uses");
            Assert.AreNotSame(sharedProfile, urpAsset.volumeProfile, "The converted pipeline asset must use its own copy");
            Assert.AreEqual(70f, ShadowDistanceIn(urpAsset.volumeProfile), "The converted pipeline asset must use the current shadow distance");
        }

        [Test]
        public void WhenRunningBuiltInToURP3DRenderSettingsConverter_Again_OnTheDefaultRenderPipeline_ItWarnsThatTheDefaultRenderPipelineChanged()
        {
            var renderSettingsConverter = new BuiltInToURP3DRenderSettingsConverter();
            var qualityItem = FindCurrentQualityItem(renderSettingsConverter);

            var urpAsset = ConvertCurrentQualityLevel(renderSettingsConverter, qualityItem, 30f);
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            try
            {
                LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape($"'{Link(urpAsset)}' is also used as the default render pipeline in Graphics settings")));
                ConvertCurrentQualityLevel(renderSettingsConverter, qualityItem, 70f);
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = m_DefaultRenderPipeline;
            }
        }

        [Test]
        public void WhenRunningBuiltInToURP3DRenderSettingsConverter_WithoutAnActiveURP_TheLightFalloffMode_IsQuadratic()
        {
            var lightFalloffSettings = GraphicsSettings.GetRenderPipelineSettings<URPLightFalloffSettings>();
            if (lightFalloffSettings == null)
                Assert.Ignore("URP is not the default render pipeline. Skipping test");

            var previousLightFalloffMode = lightFalloffSettings.lightFalloffMode;
            lightFalloffSettings.lightFalloffMode = LightFalloffMode.InverseSquared;
            GraphicsSettings.defaultRenderPipeline = null;

            try
            {
                var renderSettingsConverter = new BuiltInToURP3DRenderSettingsConverter();

                void OnScanFinished(List<IRenderPipelineConverterItem> scanItems)
                {
                    IFolderRenderPipelineConverterItem qualityFolder = null;
                    foreach (var item in scanItems)
                    {
                        if (item is IFolderRenderPipelineConverterItem folder && folder.name == "Quality")
                        {
                            qualityFolder = folder;
                            break;
                        }
                    }

                    Assert.IsNotNull(qualityFolder, "Quality folder not found in scan results");

                    var status = renderSettingsConverter.Convert(qualityFolder.children[m_QualityLevel], out string msg);
                    Assert.IsTrue(status != Status.Error, msg);
                }

                renderSettingsConverter.Scan(OnScanFinished);

                Assert.AreEqual(LightFalloffMode.Quadratic, lightFalloffSettings.lightFalloffMode, "lightFalloffMode mismatch");
            }
            finally
            {
                lightFalloffSettings.lightFalloffMode = previousLightFalloffMode;
            }
        }

        [Test]
        public void WhenRunningBuiltInToURP2DRenderSettingsConverter_TheCurrent_QualityLevel_IsNowURP_AndHasEverythingProperlyAssigned()
        {
            var renderSettingsConverter = new BuiltInToURP2DRenderSettingsConverter();
            var expectedEntry = $"[{m_QualityLevel}] {m_QualityLevelName}";

            void OnScanFinished(List<IRenderPipelineConverterItem> scanItems)
            {
                Assert.IsTrue(scanItems.Count > 0, "Initialization did not found the item to convert");

                // Find the Quality folder (RenderSettingsConverter returns Graphics and Quality folders)
                IFolderRenderPipelineConverterItem qualityFolder = null;
                foreach (var item in scanItems)
                {
                    if (item is IFolderRenderPipelineConverterItem folder && folder.name == "Quality")
                    {
                        qualityFolder = folder;
                        break;
                    }
                }

                Assert.IsNotNull(qualityFolder, "Quality folder not found in scan results");
                Assert.IsTrue(qualityFolder.children.Count > m_QualityLevel, $"Quality level {m_QualityLevel} out of range for {qualityFolder.children.Count} quality levels");

                Assert.AreEqual(m_QualityLevel, QualitySettings.GetQualityLevel(), "Initialization did not rollback quality level");

                var status = renderSettingsConverter.Convert(qualityFolder.children[m_QualityLevel], out string msg);
                Assert.IsTrue(status != Status.Error, msg);

                Assert.AreEqual(m_QualityLevel, QualitySettings.GetQualityLevel(), "Run did not rollback quality level");

                // ---------- ASSET REFERENCE CHECK  ----------
                var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                Assert.IsNotNull(urpAsset, "URP asset is not assigned");
                Assert.AreEqual($"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/{m_QualityLevelName}.asset", AssetDatabase.GetAssetPath(urpAsset));

                // ---------- RENDERER REFERENCE CHECK ----------
                Assert.IsNotNull(urpAsset.m_RendererDataList);
                Assert.AreEqual(1, urpAsset.m_RendererDataList.Length);
                Assert.AreEqual($"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/Default_2D_Renderer.asset", AssetDatabase.GetAssetPath(urpAsset.m_RendererDataList[0]));
            }

            renderSettingsConverter.Scan(OnScanFinished);
        }
    }

}

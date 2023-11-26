#pragma warning disable 618 // Type or member is obsolete

using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{
    class MiscellaneousSettingsMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<IRenderPipelineGraphicsSettings>
    {
        abstract class HDRPMiscMigrationTestCase<TRenderPipelineGraphicsSettings> : IRenderPipelineGraphicsSettingsTestCase<IRenderPipelineGraphicsSettings>
            where TRenderPipelineGraphicsSettings : class, IRenderPipelineGraphicsSettings
        {
            public abstract bool IsMiscSettingsMigrationCorrect(TRenderPipelineGraphicsSettings settings, out string message);
            public abstract void SetUpMisc(HDRenderPipelineGlobalSettings globalSettingsAsset);

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset _)
            {
                SetUpMisc(globalSettingsAsset);
                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.ToRenderPipelineGraphicsSettings - 1;
            }

            public bool IsMigrationCorrect(IRenderPipelineGraphicsSettings settings, out string message)
            {
                return IsMiscSettingsMigrationCorrect(GraphicsSettings.GetRenderPipelineSettings<TRenderPipelineGraphicsSettings>(), out message);
            }
        }

        class AnalyticDerivativeSettingsMigrationTestCase : HDRPMiscMigrationTestCase<AnalyticDerivativeSettings>
        {
            public override void SetUpMisc(HDRenderPipelineGlobalSettings globalSettingsAsset)
            {
                globalSettingsAsset.analyticDerivativeEmulation = true;
                globalSettingsAsset.analyticDerivativeDebugOutput = true;
            }

            public override bool IsMiscSettingsMigrationCorrect(AnalyticDerivativeSettings settings, out string message)
            {
                message = string.Empty;

                bool isMigrationCorrect = settings != null;

                isMigrationCorrect &= settings.emulation;
                isMigrationCorrect &= settings.debugOutput;

                return isMigrationCorrect;
            }
        }

        class LensSettingsMigrationTestCase : HDRPMiscMigrationTestCase<LensSettings>
        {
            public override void SetUpMisc(HDRenderPipelineGlobalSettings globalSettingsAsset)
            {
                globalSettingsAsset.lensAttenuationMode = LensAttenuationMode.PerfectLens;
            }

            public override bool IsMiscSettingsMigrationCorrect(LensSettings settings, out string message)
            {
                message = string.Empty;
                return LensAttenuationMode.PerfectLens == settings.attenuationMode;
            }
        }

        class DiffusionProfileDefaultSettingsMigrationTestCase : HDRPMiscMigrationTestCase<DiffusionProfileDefaultSettings>
        {
            public override void SetUpMisc(HDRenderPipelineGlobalSettings globalSettingsAsset)
            {
                globalSettingsAsset.autoRegisterDiffusionProfiles = false;
            }

            public override bool IsMiscSettingsMigrationCorrect(DiffusionProfileDefaultSettings settings, out string message)
            {
                message = string.Empty;
                return !settings.autoRegister;
            }
        }

        class ColorGradingSettingsSettingsMigrationTestCase : HDRPMiscMigrationTestCase<ColorGradingSettings>
        {
            public override void SetUpMisc(HDRenderPipelineGlobalSettings globalSettingsAsset)
            {
                globalSettingsAsset.colorGradingSpace = ColorGradingSpace.sRGB;
            }

            public override bool IsMiscSettingsMigrationCorrect(ColorGradingSettings _, out string message)
            {
                message = string.Empty;
                return ColorGradingSpace.sRGB == GraphicsSettings.GetRenderPipelineSettings<ColorGradingSettings>().space;
            }
        }

        class SpecularFadeSettingsMigrationTestCase : HDRPMiscMigrationTestCase<SpecularFadeSettings>
        {
            public override void SetUpMisc(HDRenderPipelineGlobalSettings globalSettingsAsset)
            {
                globalSettingsAsset.specularFade = true;
            }

            public override bool IsMiscSettingsMigrationCorrect(SpecularFadeSettings settings, out string message)
            {
                message = string.Empty;
                return settings.enabled;
            }
        }

        class DynamicRenderPassCullingSettingsMigrationTestCase : HDRPMiscMigrationTestCase<RenderGraphSettings>
        {
            public override void SetUpMisc(HDRenderPipelineGlobalSettings globalSettingsAsset)
            {
                globalSettingsAsset.rendererListCulling = true;
            }

            public override bool IsMiscSettingsMigrationCorrect(RenderGraphSettings settings, out string message)
            {
                message = string.Empty;
                return settings.dynamicRenderPassCullingEnabled;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new AnalyticDerivativeSettingsMigrationTestCase())
                .SetName("When performing a migration of AnalyticDerivativeSettings, settings are being transferred correctly"),
            new TestCaseData(new LensSettingsMigrationTestCase())
                .SetName("When performing a migration of LensSettings, settings are being transferred correctly"),
            new TestCaseData(new DiffusionProfileDefaultSettingsMigrationTestCase())
                .SetName("When performing a migration of DiffusionProfileDefaultSettings, settings are being transferred correctly"),
            new TestCaseData(new ColorGradingSettingsSettingsMigrationTestCase())
                .SetName("When performing a migration of ColorGradingSettings, settings are being transferred correctly"),
            new TestCaseData(new SpecularFadeSettingsMigrationTestCase())
                .SetName("When performing a migration of SpecularFade, settings are being transferred correctly"),
            new TestCaseData(new DynamicRenderPassCullingSettingsMigrationTestCase())
                .SetName("When performing a migration of DynamicRenderPassCullingSettings, settings are being transferred correctly"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<IRenderPipelineGraphicsSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
#pragma warning restore 618

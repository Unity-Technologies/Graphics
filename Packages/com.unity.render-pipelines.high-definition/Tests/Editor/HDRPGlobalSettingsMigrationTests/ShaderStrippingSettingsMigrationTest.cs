using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{
    class ShaderStrippingSettingsMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<ShaderStrippingSetting>

    {
        class TestCase1 : IRenderPipelineGraphicsSettingsTestCase<ShaderStrippingSetting>
        {
            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.m_ShaderStrippingSetting.exportShaderVariants = false;
                globalSettingsAsset.m_ShaderStrippingSetting.shaderVariantLogLevel = ShaderVariantLogLevel.AllShaders;
#pragma warning restore 618

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.ShaderStrippingSettings;
            }

            public bool IsMigrationCorrect(ShaderStrippingSetting settings, out string message)
            {
                message = string.Empty;

                bool isMigrationCorrect = true;

                isMigrationCorrect &= settings.exportShaderVariants == false;
                isMigrationCorrect &= ShaderVariantLogLevel.AllShaders == settings.shaderVariantLogLevel;
                
                return isMigrationCorrect;
            }
        }

        class TestCase2 : IRenderPipelineGraphicsSettingsTestCase<ShaderStrippingSetting>
        {
            RenderPipelineSettings m_Settings;

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
                m_Settings = renderPipelineAsset.currentPlatformRenderPipelineSettings;

#pragma warning disable 618 // Type or member is obsolete
                var testSettings = RenderPipelineSettings.NewDefault();
                testSettings.m_ObsoleteSupportRuntimeDebugDisplay = true;
                renderPipelineAsset.currentPlatformRenderPipelineSettings = testSettings;
#pragma warning restore 618

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.First;
            }

            public void TearDown(HDRenderPipelineGlobalSettings globalSettingsAsset,
                HDRenderPipelineAsset renderPipelineAsset)
            {
                renderPipelineAsset.currentPlatformRenderPipelineSettings = m_Settings;
            }

            public bool IsMigrationCorrect(ShaderStrippingSetting settings, out string message)
            {
                message = string.Empty;

                bool isMigrationCorrect = true;

                isMigrationCorrect &= settings.stripRuntimeDebugShaders == false;

                return isMigrationCorrect;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase1())
                .SetName("When performing a migration of exportShaderVariants and shaderVariantLogLevel, settings are being transferred correctly"),
            new TestCaseData(new TestCase2())
                .SetName("When performing a full migration of strip debug variants,, settings are being transferred correctly from the HDRP asset to the Render Pipeline Graphics Settings"),

        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<ShaderStrippingSetting> testCase)
        {
            base.DoTest(testCase);
        }
    }
}

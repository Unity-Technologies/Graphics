using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShaderVariantLogLevel = UnityEngine.Rendering.ShaderVariantLogLevel;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class ShaderStrippingSettingsMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<ShaderStrippingSetting>
    {
        class TestCase1 : IRenderPipelineGraphicsSettingsTestCase<ShaderStrippingSetting>
        {
            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.m_ShaderStrippingSetting.exportShaderVariants = false;
                globalSettingsAsset.m_ShaderStrippingSetting.shaderVariantLogLevel = ShaderVariantLogLevel.AllShaders;
#pragma warning restore 618

                globalSettingsAsset.m_AssetVersion = 5;
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
            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.supportRuntimeDebugDisplay = true;
#pragma warning restore 618

                globalSettingsAsset.m_AssetVersion = 1;
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
                .SetName(
                    "When performing a migration of exportShaderVariants and shaderVariantLogLevel, settings are being transferred correctly"),
            new TestCaseData(new TestCase2())
                .SetName(
                    "When performing a full migration of strip debug variants,, settings are being transferred correctly from the HDRP asset to the Render Pipeline Graphics Settings"),

        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<ShaderStrippingSetting> testCase)
        {
            base.DoTest(testCase);
        }
    }

    class URPShaderStrippingSettingMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<URPShaderStrippingSetting>

    {
        class TestCase : IRenderPipelineGraphicsSettingsTestCase<URPShaderStrippingSetting>
        {
            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {

#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.m_URPShaderStrippingSetting.stripScreenCoordOverrideVariants = false;
                globalSettingsAsset.m_URPShaderStrippingSetting.stripUnusedPostProcessingVariants = true;
                globalSettingsAsset.m_URPShaderStrippingSetting.stripUnusedVariants = false;
#pragma warning restore 618

                globalSettingsAsset.m_AssetVersion = 5;
            }

            public bool IsMigrationCorrect(URPShaderStrippingSetting settings, out string message)
            {
                message = string.Empty;

                bool isMigrationCorrect = true;

                isMigrationCorrect &= settings.stripUnusedVariants == false;
                isMigrationCorrect &= settings.stripScreenCoordOverrideVariants == false;
                isMigrationCorrect &= settings.stripUnusedPostProcessingVariants;

                return isMigrationCorrect;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase())
                .SetName(
                    "When performing a migration of exportShaderVariants and shaderVariantLogLevel, settings are being transferred correctly"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<URPShaderStrippingSetting> testCase)
        {
            base.DoTest(testCase);
        }
    }
}

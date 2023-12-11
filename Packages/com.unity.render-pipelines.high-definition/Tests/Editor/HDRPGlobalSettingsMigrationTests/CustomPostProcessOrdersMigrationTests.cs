using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{
    class CustomPostProcessOrdersMigrationTests : RenderPipelineGraphicsSettingsMigrationTestBase<CustomPostProcessOrdersSettings>

    {
        [Serializable, HideInInspector]
        class CustomPostProcessesTestComponent : CustomPostProcessVolumeComponent, IPostProcessComponent
        {
            // For testing purposes we do not have a correct injection point for each setting as is not checked.
            public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterOpaqueAndSky;

            public override void Setup()
            {
            }

            public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
            {
            }

            public override void Cleanup()
            {
            }

            public bool IsActive() => false;
        }

        class TestCase : IRenderPipelineGraphicsSettingsTestCase<CustomPostProcessOrdersSettings>
        {
            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
                var type = typeof(CustomPostProcessesTestComponent).AssemblyQualifiedName;

#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.beforePostProcessCustomPostProcesses.Add(type);
                globalSettingsAsset.beforeTransparentCustomPostProcesses.Add(type);
                globalSettingsAsset.afterPostProcessBlursCustomPostProcesses.Add(type);
                globalSettingsAsset.afterPostProcessCustomPostProcesses.Add(type);
                globalSettingsAsset.beforeTAACustomPostProcesses.Add(type);
#pragma warning restore 618

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.CustomPostProcessOrdersSettings - 1;
            }

            public bool IsMigrationCorrect(CustomPostProcessOrdersSettings settings, out string message)
            {
                message = string.Empty;

                bool isMigrationCorrect = true;

                isMigrationCorrect &= settings.beforePostProcessCustomPostProcesses.Contains<CustomPostProcessesTestComponent>();
                isMigrationCorrect &= settings.beforeTransparentCustomPostProcesses.Contains<CustomPostProcessesTestComponent>();
                isMigrationCorrect &= settings.afterPostProcessBlursCustomPostProcesses.Contains<CustomPostProcessesTestComponent>();
                isMigrationCorrect &= settings.afterPostProcessCustomPostProcesses.Contains<CustomPostProcessesTestComponent>();
                isMigrationCorrect &= settings.beforeTAACustomPostProcesses.Contains<CustomPostProcessesTestComponent>();

                if (!isMigrationCorrect)
                    message = $"{nameof(CustomPostProcessesTestComponent)} has not been found in some list";
                return isMigrationCorrect;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase())
                .SetName("When performing a migration of CustomPostProcessOrdersSettings, settings are being transferred correctly")
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<CustomPostProcessOrdersSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.Rendering.HighDefinition;
using RenderingLayerMask = UnityEngine.RenderingLayerMask;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{
    class RenderingLayersMigrationTest : GlobalSettingsMigrationTest
    {
        class TestCase1 : IGlobalSettingsMigrationTestCase
        {
            private List<string> m_RollbackLayers = new();

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset,
                HDRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.renderingLayerNames = new string[5]
                {
                    "Test Layer 1",
                    "Test Layer 2",
                    "Test Layer 3",
                    "Test Layer 4",
                    "Test Layer 5",
                };
#pragma warning restore 618

                m_RollbackLayers.Clear();
                m_RollbackLayers.AddRange(RenderingLayerMask.GetDefinedRenderingLayerNames());
                var count = RenderingLayerMask.GetDefinedRenderingLayerCount();
                for (int i = 1; i < count; ++i)
                {
                    RenderPipelineEditorUtility.TrySetRenderingLayerName(i, string.Empty);
                }

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.RenderingLayerMask - 1;
            }

            public void TearDown(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
                for (int i = 1; i < m_RollbackLayers.Count; ++i)
                {
                    RenderPipelineEditorUtility.TrySetRenderingLayerName(i, m_RollbackLayers[i]);
                }
            }

            public bool IsMigrationCorrect(out string message)
            {
                message = string.Empty;
                if (RenderingLayerMask.GetDefinedRenderingLayerCount() != 5)
                {
                    message = "Mismatch number of rendering layer count";
                    return false;
                }

                var expectedLayers = new string[5]
                {
                    "Default",
                    "Test Layer 2",
                    "Test Layer 3",
                    "Test Layer 4",
                    "Test Layer 5",
                };

                string [] migratedLayers = RenderingLayerMask.GetDefinedRenderingLayerNames();

                if (!expectedLayers.SequenceEqual(migratedLayers))
                {
                    message = "Layers have not migrated correctly";
                    return false;
                }

                return true;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase1())
                .SetName(
                    "When performing a migration of Rendering Layers, settings are being transferred correctly to Tags and Layers"),

        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IGlobalSettingsMigrationTestCase testCase)
        {
            base.DoTest(testCase);
        }
    }
}

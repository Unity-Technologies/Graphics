using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.Rendering.HighDefinition;
using RenderingLayerMask = UnityEngine.RenderingLayerMask;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{
    class RenderingLayersMigrationTest : GlobalSettingsMigrationTest
    {
        class NoLayersDefined : IGlobalSettingsMigrationTestCase
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
                SetupRenderingLayers(m_RollbackLayers);
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

                string[] migratedLayers = RenderingLayerMask.GetDefinedRenderingLayerNames();

                if (!expectedLayers.SequenceEqual(migratedLayers))
                {
                    message = "Layers have not migrated correctly";
                    return false;
                }

                return true;
            }
        }

        class SameLayersAlreadyDefined : IGlobalSettingsMigrationTestCase
        {
            private List<string> m_RollbackLayers = new();

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset,
                HDRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.renderingLayerNames = new string[5]
                {
                    "Default",
                    "Test Layer 2",
                    "Test Layer 3",
                    "  Test Layer 4 ",
                    "Test Layer 5",
                };
#pragma warning restore 618

                var existedLayers = new string[5]
                {
                    "Default",
                    "Test Layer 2",
                    "Test Layer 3",
                    "Test Layer 4",
                    "Test Layer 5",
                };

                m_RollbackLayers.Clear();
                m_RollbackLayers.AddRange(RenderingLayerMask.GetDefinedRenderingLayerNames());

                SetupRenderingLayers(existedLayers);

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.RenderingLayerMask - 1;
            }

            public void TearDown(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
                SetupRenderingLayers(m_RollbackLayers);
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

                string[] migratedLayers = RenderingLayerMask.GetDefinedRenderingLayerNames();

                if (!expectedLayers.SequenceEqual(migratedLayers))
                {
                    message = "Layers have not migrated correctly";
                    return false;
                }

                return true;
            }
        }

        class AdditionalLayersNeeded : IGlobalSettingsMigrationTestCase
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

                var existedLayers = new string[5]
                {
                    "Default",
                    "Existing Layer 2",
                    "Existing Layer 3",
                    "Existing Layer 4",
                    "Existing Layer 5",
                };
#pragma warning restore 618

                m_RollbackLayers.Clear();
                m_RollbackLayers.AddRange(RenderingLayerMask.GetDefinedRenderingLayerNames());

                SetupRenderingLayers(existedLayers);

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.RenderingLayerMask - 1;
            }

            public void TearDown(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
                SetupRenderingLayers(m_RollbackLayers);
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
                    "Existing Layer 2 - Test Layer 2",
                    "Existing Layer 3 - Test Layer 3",
                    "Existing Layer 4 - Test Layer 4",
                    "Existing Layer 5 - Test Layer 5",
                };

                string[] migratedLayers = RenderingLayerMask.GetDefinedRenderingLayerNames();

                if (!expectedLayers.SequenceEqual(migratedLayers))
                {
                    message = "Layers have not migrated correctly";
                    return false;
                }

                return true;
            }
        }

        class MultipleMigrationHappened : IGlobalSettingsMigrationTestCase
        {
            private List<string> m_RollbackLayers = new();

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset,
                HDRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.renderingLayerNames = new string[5]
                {
                    "Existing Layer 1",
                    "  Existing Layer 2",
                    "Existing Layer 3  ",
                    "Existing Layer 4",
                    "  Existing Layer 5  ",
                };

                var existedLayers = new string[5]
                {
                    "Default",
                    "Existing Layer 2 - Test Layer 2",
                    "Existing Layer 3 - Test Layer 3",
                    "Existing Layer 4 - Test Layer 4",
                    "Existing Layer 5 - Test Layer 5",
                };
#pragma warning restore 618

                m_RollbackLayers.Clear();
                m_RollbackLayers.AddRange(RenderingLayerMask.GetDefinedRenderingLayerNames());

                SetupRenderingLayers(existedLayers);

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.RenderingLayerMask - 1;
            }

            public void TearDown(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
                SetupRenderingLayers(m_RollbackLayers);
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
                    "Existing Layer 2 - Test Layer 2",
                    "Existing Layer 3 - Test Layer 3",
                    "Existing Layer 4 - Test Layer 4",
                    "Existing Layer 5 - Test Layer 5",
                };

                string[] migratedLayers = RenderingLayerMask.GetDefinedRenderingLayerNames();

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
            new TestCaseData(new NoLayersDefined())
                .SetName("When performing a migration of Rendering Layers, settings are being transferred correctly to Tags and Layers"),
            new TestCaseData(new SameLayersAlreadyDefined())
                .SetName("When performing a migration of Rendering Layers second time, no duplicated names added"),
            new TestCaseData(new AdditionalLayersNeeded())
                .SetName("When performing a migration of Rendering Layers and some already exist, new Layers added with a dash"),
            new TestCaseData(new MultipleMigrationHappened())
                .SetName("When performing a migration of Rendering Layers and multiple migration happened, no duplicated names added"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IGlobalSettingsMigrationTestCase testCase)
        {
            base.DoTest(testCase);
        }

        internal static void SetupRenderingLayers(List<string> existedLayers)
        {
            SetupRenderingLayers(existedLayers.ToArray());
        }

        internal static void SetupRenderingLayers(string[] existedLayers)
        {
            for (int count = RenderingLayerMask.GetRenderingLayerCount(); count > 1; count--)
                RenderPipelineEditorUtility.TryRemoveLastRenderingLayerName();

            for (int i = 1; i < existedLayers.Length; ++i)
                RenderPipelineEditorUtility.TryAddRenderingLayerName(existedLayers[i]);
        }
    }
}

using System;
using System.Collections;
using Common;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace MultipleSRP.EditMode
{
    public class GraphicsSettingsTests
    {
        RenderPipelineAsset m_GraphicsSettingsRPAsset;
        RenderPipelineAsset m_QualitySettingsRPAsset;

        [SetUp]
        public void SetUp()
        {
            m_GraphicsSettingsRPAsset = GraphicsSettings.defaultRenderPipeline;
            m_QualitySettingsRPAsset = QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
        }

        [TearDown]
        public void TearDown()
        {
            RenderPipelineManager.activeRenderPipelineDisposed -= PipelineDisposedCallback;
            GraphicsSettings.defaultRenderPipeline = m_GraphicsSettingsRPAsset;
            QualitySettings.renderPipeline = m_QualitySettingsRPAsset;
        }

        void CloseOpenProjectSettingsWindows()
        {
            var projectSettingsWindows = Resources.FindObjectsOfTypeAll(typeof(ProjectSettingsWindow));
            if (projectSettingsWindows != null && projectSettingsWindows.Length > 0)
            {
                foreach (var window in projectSettingsWindows)
                    (window as EditorWindow).Close();
            }
        }

        void PipelineDisposedCallback()
        {
            Assert.Fail("Render Pipeline was disposed as a side effect of opening Project Settings > Graphics");
        }

        [UnityTest]
        public IEnumerator RenderPipelineNotRecreatedWhenActivatingGraphicsSettingsTab(
            [Values(typeof(UniversalRenderPipelineAsset), typeof(HDRenderPipelineAsset))]
            Type rpType)
        {
            CloseOpenProjectSettingsWindows();

            const string graphicsSettingsPath = "Project/Graphics";
            const string playerSettingsPath = "Project/Player";

            IEnumerator WaitAFewFrames()
            {
                for (int i = 0; i < 10; i++)
                    yield return null;
            }

            using (new RenderPipelineScope(rpType, forceInitialization: true))
            {
                yield return WaitAFewFrames();

                // Open Project Settings > Graphics (may trigger RP recreate on not-up-to-date projects)
                SettingsService.OpenProjectSettings(graphicsSettingsPath);
                yield return WaitAFewFrames();

                // Switch to Player Settings tab
                SettingsService.OpenProjectSettings(playerSettingsPath);
                yield return WaitAFewFrames();

                // Ensure the RP is actually initialized so that disposal is possible, subscribe to disposed event
                RenderPipelineScope.ForceInitialization();
                RenderPipelineManager.activeRenderPipelineDisposed += PipelineDisposedCallback;

                // Switch back to Graphics Settings tab, wait in case RP recreate is triggered
                var window = SettingsService.OpenProjectSettings(graphicsSettingsPath);
                yield return WaitAFewFrames();

                // All good if we reached here, cleanup
                RenderPipelineManager.activeRenderPipelineDisposed -= PipelineDisposedCallback;
                window.Close();
            }
        }
    }
}

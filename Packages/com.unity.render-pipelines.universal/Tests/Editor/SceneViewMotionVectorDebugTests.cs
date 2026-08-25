using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Tests
{
    // Regression coverage for the scene view motion vector debug overlay: the editor repaint loop
    // must advance the motion vector frame index from the SceneView camera alone, without any
    // Game view render arming a game camera first.
    class SceneViewMotionVectorDebugTests
    {
        SceneView m_SceneView;
        DebugFullScreenMode m_PreviousDebugMode;
        bool m_Initialized;

        static readonly MethodInfo s_GetMotionVectorFrameIndex = typeof(Camera).Assembly
            .GetType("UnityEngine.Rendering.RendererUpdateManagerBindings")
            .GetMethod("GetMotionVectorFrameIndex", BindingFlags.Public | BindingFlags.Static);

        static int motionVectorFrameIndex => (int)s_GetMotionVectorFrameIndex.Invoke(null, null);

        [SetUp]
        public void SetUp()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("Requires URP to be the active render pipeline.");

            // Close Game views and disarm all game cameras so only the SceneView path can advance the index.
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.GetType().Name == "GameView")
                    window.Close();
            foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>())
                camera.depthTextureMode = DepthTextureMode.None;

            var renderingSettings = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings;
            m_PreviousDebugMode = renderingSettings.fullScreenDebugMode;
            renderingSettings.fullScreenDebugMode = DebugFullScreenMode.MotionVector;

            m_SceneView = EditorWindow.GetWindow<SceneView>();
            m_SceneView.Show();
            m_Initialized = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (!m_Initialized)
                return;

            UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.fullScreenDebugMode = m_PreviousDebugMode;
            if (m_SceneView != null)
                m_SceneView.Close();
        }

        [UnityTest]
        public IEnumerator SceneViewRepaint_WithMotionVectorDebug_AdvancesMotionVectorFrameIndex()
        {
            int start = motionVectorFrameIndex;

            // The first repaints render the motion vector pass and arm the scene view usage;
            // subsequent repaints must then advance the frame index.
            for (int i = 0; i < 30 && motionVectorFrameIndex == start; ++i)
            {
                m_SceneView.Repaint();
                yield return null;
            }

            Assert.Greater(motionVectorFrameIndex, start,
                "Motion vector frame index did not advance from SceneView repaints alone (no Game view open). " +
                "The scene view motion vector debug overlay would appear frozen.");
        }
    }
}

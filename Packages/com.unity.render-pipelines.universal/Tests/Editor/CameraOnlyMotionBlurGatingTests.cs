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
    // Regression coverage for camera-only motion blur gating: MotionBlurMode.CameraOnly consumes the
    // per-camera view-projection history (MotionVectorsPersistentData) without rendering the motion
    // vector texture, so URP must still arm DepthTextureMode.MotionVectors on the camera. Otherwise
    // the engine's motion vector frame index never advances, the blur matrices freeze after the first
    // frame, and the effect silently outputs zero velocity (no blur at all).
    class CameraOnlyMotionBlurGatingTests
    {
        Camera m_Camera;
        RenderTexture m_TargetTexture;
        GameObject m_CameraGO;
        GameObject m_VolumeGO;
        VolumeProfile m_Profile;
        SceneView m_SceneView;
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

            // Close Game views and disarm all cameras so only the test camera can arm the frame index gate.
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.GetType().Name == "GameView")
                    window.Close();
            foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>())
                camera.depthTextureMode = DepthTextureMode.None;

            m_Profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var motionBlur = m_Profile.Add<MotionBlur>();
            motionBlur.active = true;
            motionBlur.mode.Override(MotionBlurMode.CameraOnly);
            motionBlur.intensity.Override(1f);

            m_VolumeGO = new GameObject("CameraOnlyMotionBlurTestVolume") { hideFlags = HideFlags.HideAndDontSave };
            var volume = m_VolumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000f; // Take precedence over any volume in the open scene.
            volume.sharedProfile = m_Profile;

            m_TargetTexture = new RenderTexture(64, 64, 24);
            m_CameraGO = new GameObject("CameraOnlyMotionBlurTestCamera") { hideFlags = HideFlags.HideAndDontSave };
            m_Camera = m_CameraGO.AddComponent<Camera>();
            m_Camera.targetTexture = m_TargetTexture;
            m_Camera.depthTextureMode = DepthTextureMode.None;
            var additionalCameraData = m_CameraGO.AddComponent<UniversalAdditionalCameraData>();
            additionalCameraData.renderPostProcessing = true;

            // Repainting the SceneView acts as the editor frame heartbeat that lets an armed gate
            // advance the frame index (same mechanism as SceneViewMotionVectorDebugTests).
            m_SceneView = EditorWindow.GetWindow<SceneView>();
            m_SceneView.Show();

            m_Initialized = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (!m_Initialized)
                return;

            if (m_SceneView != null)
                m_SceneView.Close();
            if (m_CameraGO != null)
                Object.DestroyImmediate(m_CameraGO);
            if (m_TargetTexture != null)
            {
                m_TargetTexture.Release();
                Object.DestroyImmediate(m_TargetTexture);
            }

            if (m_VolumeGO != null)
                Object.DestroyImmediate(m_VolumeGO);
            if (m_Profile != null)
                Object.DestroyImmediate(m_Profile);
        }

        [Test]
        public void RenderWithCameraOnlyMotionBlur_ArmsMotionVectorDepthTextureMode()
        {
            Assert.AreEqual(DepthTextureMode.None, m_Camera.depthTextureMode & DepthTextureMode.MotionVectors,
                "Test precondition failed: the camera already had the motion vector bit armed before rendering.");

            m_Camera.Render();

            // Note: the depth bit always accompanies the motion vector bit — the native SetDepthTextureMode
            // enforces it — so the bits alone can't distinguish this arming from the motion vector pass running.
            Assert.AreNotEqual(DepthTextureMode.None, m_Camera.depthTextureMode & DepthTextureMode.MotionVectors,
                "Rendering with an active camera-only MotionBlur volume must arm DepthTextureMode.MotionVectors " +
                "on the camera. Without it the motion vector frame index stays frozen and camera-only motion blur " +
                "silently stops blurring (zero velocity).");
        }

        [UnityTest]
        public IEnumerator CameraOnlyMotionBlur_AdvancesMotionVectorFrameIndex()
        {
            // A single render arms the gate (the bit is sticky); afterwards the editor repaint heartbeat
            // alone must advance the index.
            m_Camera.Render();

            int start = motionVectorFrameIndex;
            for (int i = 0; i < 30 && motionVectorFrameIndex == start; ++i)
            {
                m_SceneView.Repaint();
                yield return null;
            }

            Assert.Greater(motionVectorFrameIndex, start,
                "Motion vector frame index did not advance with an active camera-only MotionBlur volume. " +
                "MotionVectorsPersistentData would never update its view-projection history, so the blur " +
                "would render with zero velocity.");
        }
    }
}

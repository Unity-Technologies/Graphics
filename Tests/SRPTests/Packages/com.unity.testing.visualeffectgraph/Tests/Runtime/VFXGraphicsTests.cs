using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VFX;
using System.Reflection;
#endif
using NUnit.Framework;
using Object = UnityEngine.Object;
using UnityEngine.VFX.Utility;

namespace UnityEngine.VFX.Test
{
    public static class VisualEffectsGraphGraphicsTests
    {
        static int m_previousCaptureFrameRate;
        static float m_previousFixedTimeStep;
        static float m_previousMaxDeltaTime;
#if UNITY_EDITOR
        static bool m_previousAsyncShaderCompilation;
#endif
        [OneTimeSetUp]
        public static void Init()
        {
            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;
#if UNITY_EDITOR
            m_previousAsyncShaderCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;
#endif
        }

        public static IEnumerator Run(SceneGraphicsTestCase testCase)
        {
            GraphicsTestLogger.Log($"Running test case {testCase.ScenePath} with reference image {testCase.ScenePath}.");

#if UNITY_EDITOR
            while (SceneView.sceneViews.Count > 0)
            {
                var sceneView = SceneView.sceneViews[0] as SceneView;
                sceneView.Close();
            }
#endif
			GraphicsTestLogger.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}'.");
            SceneManagement.SceneManager.LoadScene(testCase.ScenePath);

            // Always wait one frame for scene load
            yield return null;

            var testSettingsInScene = Object.FindAnyObjectByType<GraphicsTestSettings>();
            var vfxTestSettingsInScene = Object.FindAnyObjectByType<VFXGraphicsTestSettings>();

            var imageComparisonSettings = new ImageComparisonSettings() { AverageCorrectnessThreshold = VFXGraphicsTestSettings.defaultAverageCorrectnessThreshold };
            if (testSettingsInScene != null)
            {
                imageComparisonSettings = testSettingsInScene.ImageComparisonSettings;
            }

            if (XRGraphicsAutomatedTests.enabled)
            {
                bool xrCompatible = vfxTestSettingsInScene != null ? vfxTestSettingsInScene.xrCompatible : true;
                Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(xrCompatible, 0, imageComparisonSettings);

#if VFX_TESTS_HAS_HDRP
                foreach (var volume in GameObject.FindObjectsByType<Volume>(FindObjectsSortMode.InstanceID))
                {
                    if (volume.profile.TryGet<Rendering.HighDefinition.Fog>(out var fog))
                        fog.volumeSliceCount.value *= 2;
                }
#endif
            }

            //Setup frame rate capture
            float simulateTime = VFXGraphicsTestSettings.defaultSimulateTime;
            int captureFrameRate = VFXGraphicsTestSettings.defaultCaptureFrameRate;
            float fixedTimeStepScale = VFXGraphicsTestSettings.defaultFixedTimeStepScale;

            if (vfxTestSettingsInScene != null)
            {
                simulateTime = vfxTestSettingsInScene.simulateTime;
                captureFrameRate = vfxTestSettingsInScene.captureFrameRate;
                fixedTimeStepScale = vfxTestSettingsInScene.fixedTimeStepScale;
            }
            float period = 1.0f / captureFrameRate;

            Time.captureFramerate = captureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = period * fixedTimeStepScale;
            UnityEngine.VFX.VFXManager.maxDeltaTime = period;

            //Waiting for the capture frame rate to be effective
            const int maxFrameWaiting = 8;
            int maxFrame = maxFrameWaiting;
            while (Time.deltaTime != period && maxFrame-- > 0)
                yield return new WaitForEndOfFrame();
            Assert.Greater(maxFrame, 0);

            var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            if (camera)
            {
                var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();

                var rt = RenderTexture.GetTemporary(imageComparisonSettings.TargetWidth, imageComparisonSettings.TargetHeight, 24);
                camera.targetTexture = rt;

                if (vfxComponents.Length > 0)
                {
                    //Waiting for the rendering to be ready, if at least one component has been culled, camera is ready
                    maxFrame = maxFrameWaiting;
                    while (vfxComponents.All(o => o.culled) && maxFrame-- > 0)
                        yield return new WaitForEndOfFrame();
                    Assert.Greater(maxFrame, 0);

                    foreach (var component in vfxComponents)
                        component.Reinit();
                }

#if UNITY_EDITOR
                //When we change the graph, if animator was already enable, we should reinitialize animator to force all BindValues
                var animators = Resources.FindObjectsOfTypeAll<Animator>();
                foreach (var animator in animators)
                {
                    animator.Rebind();
                }
                var audioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
#endif
                var paramBinders = Resources.FindObjectsOfTypeAll<VFXPropertyBinder>();
                foreach (var paramBinder in paramBinders)
                {
                    var binders = paramBinder.GetPropertyBinders<VFXBinderBase>();
                    foreach (var binder in binders)
                    {
                        binder.Reset();
                    }
                }

                if (XRGraphicsAutomatedTests.running)
                    camera.targetTexture = null;

                int waitFrameCount = (int)(simulateTime / period);
                int startFrameIndex = Time.frameCount;
                int expectedFrameIndex = startFrameIndex + waitFrameCount;

                while (Time.frameCount != expectedFrameIndex)
                {
                    yield return new WaitForEndOfFrame();
#if UNITY_EDITOR
                    foreach (var audioSource in audioSources)
                        if (audioSource.clip != null && audioSource.playOnAwake)
                            audioSource.PlayDelayed(Mathf.Repeat(simulateTime, audioSource.clip.length));
#endif
                }

                try
                {
                    camera.targetTexture = null;

                    ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, imageComparisonSettings, testCase.ReferenceImage.LoadMessage);

                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            Time.captureFramerate = m_previousCaptureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = m_previousMaxDeltaTime;
#if UNITY_EDITOR
            EditorSettings.asyncShaderCompilation = m_previousAsyncShaderCompilation;
#endif
        }
    }
}

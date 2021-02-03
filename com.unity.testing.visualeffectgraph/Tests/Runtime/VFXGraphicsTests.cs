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
    public class VFXGraphicsTests
    {
        int m_previousCaptureFrameRate;
        float m_previousFixedTimeStep;
        float m_previousMaxDeltaTime;
#if UNITY_EDITOR
        bool m_previousAsyncShaderCompilation;
#endif
        [OneTimeSetUp]
        public void Init()
        {
            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;
#if UNITY_EDITOR
            m_previousAsyncShaderCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;
#endif
        }

        [UnityTest, Category("VisualEffect")]
        [PrebuildSetup("SetupGraphicsTestCases")]
        [UseGraphicsTestCases]
        [Timeout(450 * 1000)] // Increase timeout to handle complex scenes with many shaders and XR variants
        public IEnumerator Run(GraphicsTestCase testCase)
        {
#if UNITY_EDITOR
            while (SceneView.sceneViews.Count > 0)
            {
                var sceneView = SceneView.sceneViews[0] as SceneView;
                sceneView.Close();
            }
#endif
            SceneManagement.SceneManager.LoadScene(testCase.ScenePath);

            // Always wait one frame for scene load
            yield return null;

            var testSettingsInScene = Object.FindObjectOfType<GraphicsTestSettings>();
            var vfxTestSettingsInScene = Object.FindObjectOfType<VFXGraphicsTestSettings>();

            var imageComparisonSettings = new ImageComparisonSettings() { AverageCorrectnessThreshold = VFXGraphicsTestSettings.defaultAverageCorrectnessThreshold };
            if (testSettingsInScene != null)
            {
                imageComparisonSettings = testSettingsInScene.ImageComparisonSettings;
            }

            if (XRGraphicsAutomatedTests.enabled)
            {
                bool xrCompatible = vfxTestSettingsInScene != null ? vfxTestSettingsInScene.xrCompatible : true;
                Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(xrCompatible, 0, imageComparisonSettings);
            }

            //Setup frame rate capture
            float simulateTime = VFXGraphicsTestSettings.defaultSimulateTime;
            int captureFrameRate = VFXGraphicsTestSettings.defaultCaptureFrameRate;

            if (vfxTestSettingsInScene != null)
            {
                simulateTime = vfxTestSettingsInScene.simulateTime;
                captureFrameRate = vfxTestSettingsInScene.captureFrameRate;
            }
            float period = 1.0f / captureFrameRate;

            Time.captureFramerate = captureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = period;
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

                //Waiting for the rendering to be ready, if at least one component has been culled, camera is ready
                maxFrame = maxFrameWaiting;
                while (vfxComponents.All(o => o.culled) && maxFrame-- > 0)
                    yield return new WaitForEndOfFrame();
                Assert.Greater(maxFrame, 0);

                foreach (var component in vfxComponents)
                    component.Reinit();

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

                    ImageAssert.AreEqual(testCase.ReferenceImage, camera, imageComparisonSettings);

                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            XRGraphicsAutomatedTests.running = false;

#if UNITY_EDITOR
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
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

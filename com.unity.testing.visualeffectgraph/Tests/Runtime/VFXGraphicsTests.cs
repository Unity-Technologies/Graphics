using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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
using UnityEngine.Experimental.VFX.Utility;

namespace UnityEngine.VFX.Test
{
    public class VFXGraphicsTests
    {
        static readonly float simulateTime = 6.0f;
        static readonly int captureFrameRate = 20;
        static readonly float frequency = 1.0f / (float)captureFrameRate;
        static readonly int captureSize = 512;

        int m_previousCaptureFrameRate;
        float m_previousFixedTimeStep;
        float m_previousMaxDeltaTime;
        [SetUp]
        public void Init()
        {
            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.Experimental.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.Experimental.VFX.VFXManager.maxDeltaTime;
            Time.captureFramerate = captureFrameRate;
            UnityEngine.Experimental.VFX.VFXManager.fixedTimeStep = frequency;
            UnityEngine.Experimental.VFX.VFXManager.maxDeltaTime = frequency;
        }

        static readonly string[] ExcludedTestsButKeepLoadScene =
        {
            "20_SpawnerChaining", // Unstable. TODO investigate why
            "RenderStates", // Unstable. There is an instability with shadow rendering. TODO Fix that
            "ConformAndSDF", // Turbulence is not deterministic
            "13_Decals", //doesn't render TODO investigate why <= this one is in world space
            "05_MotionVectors", //possible GPU Hang on this, skip it temporally
        };

        static readonly string[] UnstableMetalTests =
        {
            // Currently known unstable results, could be Metal or more generic HLSLcc issue across multiple graphics targets
        };

        [UnityTest, Category("VisualEffect")]
        [PrebuildSetup("SetupGraphicsTestCases")]
        [UseGraphicsTestCases]
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

            var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            if (camera)
            {
                var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
#if UNITY_EDITOR
                var vfxAssets = vfxComponents.Select(o => o.visualEffectAsset).Where(o => o != null).Distinct();
                foreach (var vfx in vfxAssets)
                {
                    //Use Reflection as workaround of the access issue in .net 4 (TODO : Clean this as soon as possible)
                    //var graph = vfx.GetResource().GetOrCreateGraph(); is possible with .net 3.5 but compilation fail with 4.0
                    var visualEffectAssetExt = AppDomain.CurrentDomain.GetAssemblies()  .Select(o => o.GetType("UnityEditor.VFX.VisualEffectAssetExtensions"))
                                                                                        .Where(o => o != null)
                                                                                        .FirstOrDefault();
                    var fnGetResource = visualEffectAssetExt.GetMethod("GetResource");
                    fnGetResource = fnGetResource.MakeGenericMethod(new Type[]{ typeof(VisualEffectAsset)});
                    var resource = fnGetResource.Invoke(null, new object[] { vfx });
                    var fnGetOrCreate = visualEffectAssetExt.GetMethod("GetOrCreateGraph");
                    var graph = fnGetOrCreate.Invoke(null, new object[] { resource }) as VFXGraph;
                    graph.RecompileIfNeeded();
                }
#endif

                var rt = RenderTexture.GetTemporary(captureSize, captureSize, 24);
                camera.targetTexture = rt;

                foreach (var component in vfxComponents)
                {
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
                var paramBinders = Resources.FindObjectsOfTypeAll<VFXParameterBinder>();
                foreach (var paramBinder in paramBinders)
                {
                    var binders = paramBinder.GetParameterBinders<VFXBinderBase>();
                    foreach (var binder in binders)
                    {
                        binder.Reset();
                    }
                }

                int waitFrameCount = (int)(simulateTime / frequency);
                int startFrameIndex = Time.frameCount;
                int expectedFrameIndex = startFrameIndex + waitFrameCount;
                while (Time.frameCount != expectedFrameIndex)
                {
                    yield return null;
#if UNITY_EDITOR
                    foreach (var audioSource in audioSources)
                        if (audioSource.clip != null && audioSource.playOnAwake)
                            audioSource.PlayDelayed(Mathf.Repeat(simulateTime, audioSource.clip.length));
#endif
                }

                Texture2D actual = null;
                try
                {
                    camera.targetTexture = null;
                    actual = new Texture2D(captureSize, captureSize, TextureFormat.RGB24, false);
                    RenderTexture.active = rt;
                    actual.ReadPixels(new Rect(0, 0, captureSize, captureSize), 0, 0);
                    RenderTexture.active = null;
                    actual.Apply();

                    if (!ExcludedTestsButKeepLoadScene.Any(o => testCase.ScenePath.Contains(o)) &&
                        !(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && UnstableMetalTests.Any(o => testCase.ScenePath.Contains(o))))
                    {
                        ImageAssert.AreEqual(testCase.ReferenceImage, actual, new ImageComparisonSettings() { AverageCorrectnessThreshold = 10e-5f });
                    }
                    else
                    {
                        Debug.LogFormat("GraphicTest '{0}' result has been ignored", testCase.ReferenceImage);
                    }
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rt);
                    if (actual != null)
                        UnityEngine.Object.Destroy(actual);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureFramerate = m_previousCaptureFrameRate;
            UnityEngine.Experimental.VFX.VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            UnityEngine.Experimental.VFX.VFXManager.maxDeltaTime = m_previousMaxDeltaTime;
#if UNITY_EDITOR
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
#endif
        }
    }
}

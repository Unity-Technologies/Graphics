using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using System.IO;
#if UNITY_EDITOR
using NUnit.Framework;
#endif

namespace UnityEngine.VFX.Test
{
    public class VFXGraphicsTests
    {
        static readonly float simulateTime = 6.0f;
        static readonly int captureFrameRate = 20;
        static readonly float frequency = 1.0f / (float)captureFrameRate;

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
            "07_UnityLogo", //Unstable with HDRP. TODO investigate why
            "13_Decals", //doesn't render TODO investigate why
            "14_DecalsFlipBook", //doesn't render TODO investigate why
            "05_MotionVectors" //possible GPU Hang on this, skip it temporally
        };

        [UnityTest, Category("VisualEffect")]
        [PrebuildSetup("SetupGraphicsTestCases")]
        [UseGraphicsTestCases]
        public IEnumerator Run(GraphicsTestCase testCase)
        {
            SceneManager.LoadScene(testCase.ScenePath);

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
                    var graph = vfx.GetResource().GetOrCreateGraph();
                    graph.RecompileIfNeeded();
                }
#endif

                foreach (var component in vfxComponents)
                {
                    component.Reinit();
                }

                int waitFrameCount = (int)(simulateTime / frequency);
                int startFrameIndex = Time.frameCount;
                int expectedFrameIndex = startFrameIndex + waitFrameCount;
                while (Time.frameCount != expectedFrameIndex)
                {
                    yield return null;
                }

                if (!ExcludedTestsButKeepLoadScene.Any(o => testCase.ScenePath.Contains(o)))
                {
                    ImageAssert.AreEqual(testCase.ReferenceImage, camera, new ImageComparisonSettings() { AverageCorrectnessThreshold = 10e-5f });
                }
                else
                {
                    Debug.LogFormat("GraphicTest '{0}' result has been ignored", testCase.ReferenceImage);
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

using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Testing.VisualEffectGraph;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
#endif


namespace UnityEngine.VFX.Test
{
    [TestFixture]
    [PrebuildSetup("SetupGraphicsTestCases")]
    public class VFXCheckGarbage
    {
        public static readonly string[] s_CustomGarbageWrapperTest = new[] { "Reference_Forcing_Garbage_Creation", "Basic_Usage" };
        public static readonly string[] s_Scenarios = new[] { "021_Check_Garbage_Spawner", "021_Check_Garbage_OutputEvent" };
        private static readonly int kForceGarbageID = Shader.PropertyToID("forceGarbage");
        private static readonly WaitForEndOfFrame kWaitForEndOfFrame = new WaitForEndOfFrame();

Recorder m_gcAllocRecorder;
        AssetBundle m_AssetBundle;

        int m_previousCaptureFrameRate;
        float m_previousFixedTimeStep;
        float m_previousMaxDeltaTime;

#if UNITY_EDITOR
        bool m_previousAsyncShaderCompilation;
#endif

        [OneTimeSetUp]
        public void SetUp()
        {
            m_gcAllocRecorder = Recorder.Get("GC.Alloc");
            m_gcAllocRecorder.FilterToCurrentThread();
            m_gcAllocRecorder.enabled = false;

            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;
#if UNITY_EDITOR
            //During VisualEffect.PrepareMaterial, we can have this stack GetWritableProperties => EnsurePropertiesExist => BuildProperties => SetupKeywordsAndPasses => ApplyMaterialPropertyDrawersFromNative
            //Disabling asyncShaderCompilation is avoiding this unexpected capture
            m_previousAsyncShaderCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;
#endif

            Time.captureFramerate = 10;
            VFXManager.fixedTimeStep = 0.1f;
            VFXManager.maxDeltaTime = 0.1f;

            m_AssetBundle = AssetBundleHelper.Load("scene_in_assetbundle");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Time.captureFramerate = m_previousCaptureFrameRate;
            VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            VFXManager.maxDeltaTime = m_previousMaxDeltaTime;

#if UNITY_EDITOR
            //UnityEditorInternal.ProfilerDriver.ClearAllFrames(); //Voluntary letting the capture readable in profiler windows if needed for inspection
            ProfilerDriver.enabled = false;
            EditorSettings.asyncShaderCompilation = m_previousAsyncShaderCompilation;
#endif

#if UNITY_EDITOR
            while (SceneView.sceneViews.Count > 0)
            {
                var sceneView = SceneView.sceneViews[0] as SceneView;
                sceneView.Close();
            }
#endif
            AssetBundleHelper.Unload(m_AssetBundle);
        }

        [UnityTest]
        public IEnumerator Create_Garbage_Scenario([ValueSource(nameof(s_Scenarios))] string scenario, [ValueSource(nameof(s_CustomGarbageWrapperTest))] string garbageMode)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(string.Format("Packages/com.unity.testing.visualeffectgraph/Scenes/{0}.unity", scenario));
            yield return null;

            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
            Assert.AreEqual(1u, vfxComponents.Length);
            var currentVFX = vfxComponents[0];

            var forceGarbage = garbageMode == s_CustomGarbageWrapperTest[0];
            Assert.IsTrue(currentVFX.HasBool(kForceGarbageID));
            currentVFX.SetBool(kForceGarbageID, forceGarbage);

            int maxFrame = 8;
            while (currentVFX.culled && maxFrame-- > 0)
                yield return kWaitForEndOfFrame;

#if UNITY_EDITOR
            UnityEditorInternal.ProfilerDriver.ClearAllFrames();
            ProfilerDriver.enabled = true;
#endif

            m_gcAllocRecorder.enabled = true;
            int frameCount = 16;
            for (int i = 0; i < frameCount; ++i)
                yield return kWaitForEndOfFrame;
            m_gcAllocRecorder.enabled = false;

#if UNITY_EDITOR
            ProfilerDriver.enabled = false;
            yield return kWaitForEndOfFrame;
#endif

            int allocationCountFromCustomCallback = m_gcAllocRecorder.sampleBlockCount;
            Debug.LogFormat("Global GC.Alloc Count: {0}", allocationCountFromCustomCallback);
#if UNITY_EDITOR
            var currentFrameIndex = ProfilerDriver.GetPreviousFrameIndex(Time.frameCount);
            Assert.Greater(currentFrameIndex, 0u, "Can't retrieve Profiler Frame");

            long totalGcAllocSizeFromVFXUpdate = 0u;

            var aggregatedAllocation = new List<string>();
            var frameAllocations = new List<(int start, int end, long size)>();

            currentFrameIndex = Time.frameCount;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                currentFrameIndex = ProfilerDriver.GetPreviousFrameIndex(currentFrameIndex);
                var frameData = ProfilerDriver.GetRawFrameDataView(currentFrameIndex, 0);
                if (!frameData.valid)
                {
                    continue;
                }

                var gcAllocMarkerId = frameData.GetMarkerId("GC.Alloc");
                var mainVFXMarker = frameData.GetMarkerId("VFX.Update");
                if (gcAllocMarkerId == FrameDataView.invalidMarkerId || mainVFXMarker == FrameDataView.invalidMarkerId)
                {
                    continue;
                }

                int sampleCount = frameData.sampleCount;
                int cursor = 0;
                frameAllocations.Clear();

                while (cursor < sampleCount)
                {
                    if (mainVFXMarker == frameData.GetSampleMarkerId(cursor))
                    {
                        var nextSibling = frameData.GetSampleChildrenCountRecursive(cursor) + cursor;
                        int startMaker = cursor;
                        while (cursor < nextSibling)
                        {
                            if (gcAllocMarkerId == frameData.GetSampleMarkerId(cursor))
                            {
                                long gcAllocSize = frameData.GetSampleMetadataAsLong(cursor, 0);
                                if (!forceGarbage) //No need to capture allocation stack during reference run
                                    frameAllocations.Add(new(startMaker, cursor, gcAllocSize));
                                totalGcAllocSizeFromVFXUpdate += gcAllocSize;
                            }
                            cursor++;
                        }
                        break;
                    }
                    cursor++;
                }

                foreach (var allocation in frameAllocations)
                {
                    var message = new StringBuilder();
                    foreach (var allocCursor in Enumerable.Range(allocation.start, allocation.end - allocation.start))
                    {
                        var sampleName = allocCursor < frameData.sampleCount ? frameData.GetSampleName(allocCursor) : "OutOfRange";
                        message.AppendFormat("{0}, ", sampleName);
                    }
                    message.AppendFormat("{0} bytes", allocation.size);
                    var finalMessage = message.ToString();
                    if (!aggregatedAllocation.Contains(finalMessage))
                        aggregatedAllocation.Add(finalMessage);
                }
            }

            if (forceGarbage)
            {
                Assert.AreNotEqual(0u, totalGcAllocSizeFromVFXUpdate);
            }
            else
            {
                Assert.AreEqual(0u, totalGcAllocSizeFromVFXUpdate, aggregatedAllocation.Any() ? aggregatedAllocation.Aggregate((a, b) => $"{a}\n{b}") : string.Empty);

            }
#else
            var knownAllocation = 4u; //Previous coroutine call is expecting at most 4 GC.Alloc
            knownAllocation += 4u; //Lazy allocation from GUI.Repaint (standalone are in development mode, OnGUI is called in PlaymodeTestRunner)
            if (forceGarbage)
            {
                Assert.IsTrue(allocationCountFromCustomCallback > knownAllocation);
            }
            else
            {
                Assert.IsTrue(allocationCountFromCustomCallback <= knownAllocation);
            }
#endif
        }
    }
}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using UnityEngine.VFX.Test;
using UnityEngine.TestTools.Graphics;
using Object = UnityEngine.Object;
using UnityEngine.Profiling;
using UnityEngine.VFX.PerformanceTest;
using Unity.Profiling;
using Unity.Testing.VisualEffectGraph;
using UnityEngine.TestTools.Graphics.Performance;

using static UnityEngine.TestTools.Graphics.Performance.PerformanceTestUtils;
using static UnityEngine.TestTools.Graphics.Performance.PerformanceMetricNames;

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXRuntimePerformanceTests : PerformanceTests
    {
#if UNITY_EDITOR
        bool m_PreviousAsyncShaderCompilation;
        [OneTimeSetUp]
        public void Init()
        {
            m_PreviousAsyncShaderCompilation = UnityEditor.EditorSettings.asyncShaderCompilation;
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
        }

        [OneTimeTearDown]
        public void Clear()
        {
            UnityEditor.EditorSettings.asyncShaderCompilation = m_PreviousAsyncShaderCompilation;
        }
#else
        int m_PreviousVSyncCount;
        int m_PreviousTargetFrameRate;
        [OneTimeSetUp]
        public void Init()
        {
            m_PreviousVSyncCount = QualitySettings.vSyncCount;
            m_PreviousTargetFrameRate = Application.targetFrameRate;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 1000;
        }

        [OneTimeTearDown]
        public void Clear()
        {
            QualitySettings.vSyncCount = m_PreviousVSyncCount;
            Application.targetFrameRate = m_PreviousTargetFrameRate;
        }
#endif

        static IEnumerable<CounterTestDescription> GetCounterTests()
        {
            yield break;
        }


        static IEnumerable<MemoryTestDescription> GetMemoryTests()
        {
            yield break;
        }

        static IEnumerable<string> allMarkerName
        {
            get
            {
                yield return "PlayerLoop";
                yield return "EarlyUpdate.PresentBeforeUpdate";
                yield return "Gfx.WaitForPresentOnGfxThread";
                yield return "PostLateUpdate.PresentAfterDraw";
                yield return "NintendoCore.WaitPresentEnd";
                yield return "WaitForTargetFPS";
                yield return "GPU Frame Time";
                yield return "VFX.MeshSystem.Render";
                yield return "VFX.ParticleSystem.CopySpawnEventAttribute";
                yield return "VFX.ParticleSystem.CopyEventListCountCommand";
                yield return "VFX.ParticleSystem.CopyCount";
                yield return "VFX.ParticleSystem.ResetCount";
                yield return "VFX.ParticleSystem.CopyIndirectCount";
                yield return "VFX.ParticleSystem.UpdateMaterial";
                yield return "VFX.ParticleSystem.Init";
                yield return "VFX.ParticleSystem.BatchInit";
                yield return "VFX.ParticleSystem.Update";
                yield return "VFX.ParticleSystem.BatchUpdate";
                yield return "VFX.ParticleSystem.BatchUpdateStrip";
                yield return "VFX.ParticleSystem.BatchUploadStepData";
                yield return "VFX.ParticleSystem.BatchUploadVisibleIndirectionBuffer";
                yield return "VFX.ParticleSystem.BatchCopyDeadListCount";
                yield return "VFX.ParticleSystem.PerStripUpdate";
                yield return "VFX.ParticleSystem.RenderPoint";
                yield return "VFX.ParticleSystem.RenderPointIndirect";
                yield return "VFX.ParticleSystem.RenderLine";
                yield return "VFX.ParticleSystem.RenderLineIndirect";
                yield return "VFX.ParticleSystem.RenderTriangle";
                yield return "VFX.ParticleSystem.RenderTriangleIndirect";
                yield return "VFX.ParticleSystem.RenderQuad";
                yield return "VFX.ParticleSystem.RenderQuadIndirect";
                yield return "VFX.ParticleSystem.RenderOctagon";
                yield return "VFX.ParticleSystem.RenderOctagonIndirect";
                yield return "VFX.ParticleSystem.RenderHexahedron";
                yield return "VFX.ParticleSystem.RenderHexahedronIndirect";
                yield return "VFX.ParticleSystem.RenderMesh";
                yield return "VFX.ParticleSystem.RenderMeshIndirect";
                yield return "VFX.Update";
                yield return "VFX.PrepareCamera";
                yield return "VFX.ProcessCamera";
                yield return "VFX.ProcessCommandList";
                yield return "VFX.FillIndirectRenderArgs";
                yield return "VFX.CopyBuffer";
                yield return "VFX.InitializeDeadListBuffer";
                yield return "VFX.ZeroInitializeBuffer";
                yield return "VFX.SortBuffer";
                yield return "VFX.NotifyModifiedAsset";
                yield return "VFX.NotifyDeletedAsset";
                yield return "VFX.RegisterGizmos";
                yield return "VFX.CullJob";
                yield return "VFXEditor.VisualEffectImporter.GenerateAssetData";
                yield return "VFXEditor.VisualEffectImporter.GenerateAssetDataOneShader";
                yield return "VFXEditor.VisualEffectResource.GetResourceAtPath";
                yield return "VFXEditor.VisualEffectResource.GetResourceAtPath_Depoint";
                yield return "VisualEffect.PrepareMaterial";
                yield return "VisualEffect.Update";
                yield return "VisualEffect.Simulate";
                yield return "VisualEffect.Transfer";
                yield return "QueuePrepareIntegrateMainThreadObjects"; //VFXRenderer.AddAsRenderNode
            }
        }

        public static IEnumerator Load_And_Prepare(GraphicsTestCase testCase)
        {
            Debug.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImagePathLog}.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(testCase.ScenePath);
            yield return new WaitForEndOfFrame();

            float simulateTime = VFXGraphicsTestSettings.defaultSimulateTime;
            int captureFrameRate = VFXGraphicsTestSettings.defaultCaptureFrameRate;

            var testSettingsInScene = Object.FindAnyObjectByType<GraphicsTestSettings>();
            var vfxTestSettingsInScene = Object.FindAnyObjectByType<VFXGraphicsTestSettings>();

            if (vfxTestSettingsInScene != null)
            {
                simulateTime = vfxTestSettingsInScene.simulateTime;
                captureFrameRate = vfxTestSettingsInScene.captureFrameRate;
            }
            float period = 1.0f / captureFrameRate;
            int waitFrameCount = (int)(simulateTime / period);

            //Manual warmup for VFX (equivalent state of graphicTest, ensure there is enough particle alive)
            var previousCaptureFrameRate = Time.captureFramerate;
            var previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            var previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;

            Time.captureFramerate = captureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = period;
            UnityEngine.VFX.VFXManager.maxDeltaTime = period;

            while (waitFrameCount-- > 0)
                yield return new WaitForEndOfFrame();
            Time.captureFramerate = previousCaptureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = previousMaxDeltaTime;
            yield return new WaitForEndOfFrame();
        }

        [Timeout(600 * 1000), Version("1"), UnityTest, VFXPerformanceUseGraphicsTestCases, Performance]
#if UNITY_EDITOR
        [PrebuildSetup("SetupGraphicsTestCases")]
#endif
        public IEnumerator Counters(GraphicsTestCase testCase)
        {
            yield return Load_And_Prepare(testCase);

            var samplers = allMarkerName.Select(name =>
            {
                (Recorder recorder, SampleGroup cpu, SampleGroup gpu) newSample;
                newSample = (   Recorder.Get(name),
                                new SampleGroup(FormatSampleGroupName(k_Timing, k_CPU, name), SampleUnit.Millisecond, false),
                                new SampleGroup(FormatSampleGroupName(k_Timing, k_GPU, name), SampleUnit.Millisecond, false));
                return newSample;
            }).ToArray();

            foreach (var sampler in samplers)
                sampler.recorder.enabled = true;

            // Wait for the markers to be initialized
            for (int i = 0; i < 20; i++)
                yield return new WaitForEndOfFrame();

            for (int i = 0; i < 120; i++)
            {
                foreach (var sampler in samplers)
                {
                    if (sampler.recorder.elapsedNanoseconds > 0)
                        Measure.Custom(sampler.cpu, (double)sampler.recorder.elapsedNanoseconds / 1000000.0);
                    if (sampler.recorder.gpuElapsedNanoseconds > 0)
                        Measure.Custom(sampler.gpu, (double)sampler.recorder.gpuElapsedNanoseconds / 1000000.0);
                }
                yield return new WaitForEndOfFrame();
            }

            foreach (var sampler in samplers)
                sampler.recorder.enabled = false;
        }
    }

    public class VFXRuntimeMemoryTests : PerformanceTests
    {
#if UNITY_EDITOR
        bool m_PreviousAsyncShaderCompilation;
        [OneTimeSetUp]
        public void Init()
        {
            m_PreviousAsyncShaderCompilation = UnityEditor.EditorSettings.asyncShaderCompilation;
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
        }

        [OneTimeTearDown]
        public void Clear()
        {
            UnityEditor.EditorSettings.asyncShaderCompilation = m_PreviousAsyncShaderCompilation;
        }
#endif

        private IEnumerable<Type> GetVFXMemoryObjectTypes()
        {
            yield return typeof(VisualEffect);
            yield return typeof(VisualEffectAsset);
            foreach (var other in GetMemoryObjectTypes())
                yield return other;
        }

        private static long s_minObjectSize = 1024 * 64;

        private IEnumerator FreeMemory()
        {
            GC.Collect();
            var unloadAsync = Resources.UnloadUnusedAssets();
            while (!unloadAsync.isDone)
                yield return new WaitForEndOfFrame();
        }

        [Timeout(600 * 1000), Version("1"), UnityTest, VFXPerformanceUseGraphicsTestCases, Performance, Order(0)]
#if UNITY_EDITOR
        [PrebuildSetup("SetupGraphicsTestCases")]
#endif
        public IEnumerator Memory(GraphicsTestCase testCase)
        {
            yield return FreeMemory();

            yield return VFXRuntimePerformanceTests.Load_And_Prepare(testCase);

            var results = new List<(string name, long size)>();
            long totalMemory = 0u;
            long totalMemoryVfx = 0;
            foreach (var type in GetVFXMemoryObjectTypes())
            {
                var allObjectOfType = Resources.FindObjectsOfTypeAll(type);
                foreach (var obj in allObjectOfType)
                {
                    long currSize = Profiler.GetRuntimeMemorySizeLong(obj);
                    totalMemory += currSize;
                    if (type == typeof(VisualEffect) || type == typeof(VisualEffectAsset))
                    {
                        totalMemoryVfx += currSize;
                    }
                    else
                    {
                        //Only for not vfx object, skip small object report
                        if (currSize < s_minObjectSize)
                            continue;
                    }

                    string name = obj.GetType().Name;
                    if (type == typeof(VisualEffect)) //Special naming for VisualEffectComponent
                    {
                        var visualEffect = obj as VisualEffect;
                        var asset = visualEffect.visualEffectAsset;
                        if (asset != null)
                            name += "." + asset.name;
                    }
                    else if (!String.IsNullOrEmpty(obj.name))
                    {
                        name += "." + obj.name;
                    }
                    results.Add((name, currSize));
                }
            }

            foreach (var result in results)
                Measure.Custom(new SampleGroup(FormatSampleGroupName(k_Memory, result.name), SampleUnit.Byte, false), result.size);
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, "totalMemory"), SampleUnit.Byte, false), totalMemory);
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, "totalMemoryVfx"), SampleUnit.Byte, false), totalMemoryVfx);
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, "totalMemoryAllocated"), SampleUnit.Byte, false), Profiler.GetTotalAllocatedMemoryLong());
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, "totalMemoryAllocatedForGraphicsDriver"), SampleUnit.Byte, false), Profiler.GetAllocatedMemoryForGraphicsDriver());

            yield return FreeMemory();
        }

        [Version("1"), UnityTest, Performance, Order(1000)]
        public IEnumerator RemainingMemoryAfterAllRun()
        {
            yield return FreeMemory();

            var assetBundle = AssetBundleHelper.Load("scene_in_assetbundle");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Packages/com.unity.testing.visualeffectgraph/Scenes/Empty.unity");
            for (int i = 0; i < 4; ++i)
                yield return new WaitForEndOfFrame();

            var globalSampleToFetch = new[] { "System Used Memory", "Total Reserved Memory", "Gfx Used Memory", "Gfx Reserved Memory" };
            var profilerRecorders = new ProfilerRecorder[globalSampleToFetch.Length];
            for (int i = 0; i < globalSampleToFetch.Length; ++i)
            {
                profilerRecorders[i] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, globalSampleToFetch[i]);
            }

            yield return new WaitForEndOfFrame();
            for (int i = 0; i < globalSampleToFetch.Length; ++i)
            {
                var profilerRecorder = profilerRecorders[i];
                Assert.AreNotEqual((long)0, profilerRecorder.LastValue);
                var name = "Remaining_" + globalSampleToFetch[i].Replace(" ", "");
                Measure.Custom(new SampleGroup(name, SampleUnit.Byte, false), profilerRecorder.LastValue);
            }

            foreach (var profilerRecorder in profilerRecorders)
                profilerRecorder.Dispose();

            AssetBundleHelper.Unload(assetBundle);

            yield return FreeMemory();
        }
    }
}

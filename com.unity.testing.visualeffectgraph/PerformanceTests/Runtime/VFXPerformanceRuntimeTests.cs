using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using UnityEngine.VFX.Test;
using UnityEngine.TestTools.Graphics;
using Object = UnityEngine.Object;
using UnityEngine.Profiling;
using static PerformanceTestUtils;
using static PerformanceMetricNames;

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXRuntimePerformanceTests : PerformanceTests
    {
        const int GlobalTimeout = 120 * 1000;

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
                yield return "VFX.MeshSystem.Render";
                yield return "VFX.ParticleSystem.CopySpawnEventAttribute";
                yield return "VFX.ParticleSystem.CopyEventListCountCommand";
                yield return "VFX.ParticleSystem.CopyCount";
                yield return "VFX.ParticleSystem.ResetCount";
                yield return "VFX.ParticleSystem.CopyIndirectCount";
                yield return "VFX.ParticleSystem.UpdateMaterial";
                yield return "VFX.ParticleSystem.Init";
                yield return "VFX.ParticleSystem.Update";
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
                yield return "VFX.FillIndirectRenderArgs";
                yield return "VFX.CopyBuffer";
                yield return "VFX.InitializeDeadListBuffer";
                yield return "VFX.ZeroInitializeBuffer";
                yield return "VFX.SortBuffer";
                yield return "VFX.NotifyModifiedAsset";
                yield return "VFX.NotifyDeletedAsset";
                yield return "VFX.DefaultCommandBuffer";
                yield return "VFX.RegisterGizmos";
                yield return "VFXEditor.VisualEffectImporter.GenerateAssetData";
                yield return "VFXEditor.VisualEffectImporter.GenerateAssetDataOneShader";
                yield return "VFXEditor.VisualEffectResource.GetResourceAtPath";
                yield return "VFXEditor.VisualEffectResource.GetResourceAtPath_Depoint";
            }
        }

        public static IEnumerator Load_And_Prepare(GraphicsTestCase testCase)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(testCase.ScenePath);
            yield return null;

            float simulateTime = VFXGraphicsTestSettings.defaultSimulateTime;
            int captureFrameRate = VFXGraphicsTestSettings.defaultCaptureFrameRate;

            var testSettingsInScene = Object.FindObjectOfType<GraphicsTestSettings>();
            var vfxTestSettingsInScene = Object.FindObjectOfType<VFXGraphicsTestSettings>();

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
                yield return null;
            Time.captureFramerate = previousCaptureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = previousMaxDeltaTime;
            yield return null;
        }

        [Timeout(GlobalTimeout), Version("1"), UnityTest, UseGraphicsTestCases, PrebuildSetup("SetupGraphicsTestCases"), Performance]
        public IEnumerator Counters(GraphicsTestCase testCase)
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;

            yield return Load_And_Prepare(testCase);

            //TODO : Avoid too much garbage here
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
                yield return null;

            for (int i = 0; i < 120; i++)
            {
                foreach (var sampler in samplers)
                {
                    if (sampler.recorder.elapsedNanoseconds > 0)
                        Measure.Custom(sampler.cpu, (double)sampler.recorder.elapsedNanoseconds / 1000000.0);
                    if (sampler.recorder.gpuElapsedNanoseconds > 0)
                        Measure.Custom(sampler.gpu, (double)sampler.recorder.gpuElapsedNanoseconds / 1000000.0);
                }
                yield return null;
            }

            foreach (var sampler in samplers)
                sampler.recorder.enabled = false;

            UnityEngine.Debug.unityLogger.logEnabled = true;
        }
    }

    public class VFXRuntimeMemoryTests : PerformanceTests
    {
        const int GlobalTimeout = 120 * 1000;
        [Timeout(GlobalTimeout), Version("1"), UnityTest, UseGraphicsTestCases, PrebuildSetup("SetupGraphicsTestCases"), Performance]
        public IEnumerator Memory(GraphicsTestCase testCase)
        {
            var totalMemoryAllocated = Profiler.GetTotalAllocatedMemoryLong();
            var totalMemoryAllocatedForGraphicsDriver = Profiler.GetAllocatedMemoryForGraphicsDriver();

            UnityEngine.Debug.unityLogger.logEnabled = false;
            yield return VFXRuntimePerformanceTests.Load_And_Prepare(testCase);

            var allVisualEffect = Resources.FindObjectsOfTypeAll<VisualEffect>();
            var allVisualEffectAsset = Resources.FindObjectsOfTypeAll<VisualEffectAsset>();

            var results = new List<(string name, long size)>();
            long totalMemoryVfx = 0;
            foreach (var visualEffect in allVisualEffect)
            {
                var asset = visualEffect.visualEffectAsset;
                var name = "VisualEffectComponent." + (asset != null ? asset.name : "null");
                long currSize = Profiler.GetRuntimeMemorySizeLong(visualEffect);
                totalMemoryVfx += currSize;
                results.Add((name, currSize));
            }

            foreach (var visualEffectAsset in allVisualEffectAsset)
            {
                var name = "VisualEffectAsset." + visualEffectAsset;
                long currSize = Profiler.GetRuntimeMemorySizeLong(visualEffectAsset);
                totalMemoryVfx += currSize;
                results.Add((name, currSize));
            }

            //Apply delta of whole memory
            totalMemoryAllocated = Profiler.GetTotalAllocatedMemoryLong() - totalMemoryAllocated;
            totalMemoryAllocatedForGraphicsDriver = Profiler.GetAllocatedMemoryForGraphicsDriver() - totalMemoryAllocatedForGraphicsDriver;

            foreach (var result in results)
                Measure.Custom(new SampleGroup(FormatSampleGroupName(k_Memory, result.name), SampleUnit.Byte, false), result.size);
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, "totalMemoryVfx"), SampleUnit.Byte, false), totalMemoryVfx);
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, "totalMemoryAllocated"), SampleUnit.Byte, false), totalMemoryAllocated);
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, "totalMemoryAllocatedForGraphicsDriver"), SampleUnit.Byte, false), totalMemoryAllocatedForGraphicsDriver);

            yield return null;
            //Force garbage collection to avoid unexpected state in following test
            GC.Collect();
            Resources.UnloadUnusedAssets();
            yield return null;

            UnityEngine.Debug.unityLogger.logEnabled = true;
        }
    }
}

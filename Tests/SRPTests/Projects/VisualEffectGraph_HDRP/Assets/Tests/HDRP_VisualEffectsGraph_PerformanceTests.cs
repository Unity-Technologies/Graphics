using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;
using UnityEngine.TestTools.Graphics.Performance;
using Unity.PerformanceTesting;
using Unity.Testing.VisualEffectGraph.Tests;
using UnityEditor.VFX.PerformanceTest;
using UnityEngine.VFX.PerformanceTest;

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXRuntimePerformanceTests : PerformanceTests
    {
        [IgnoreGraphicsTest("05_MotionVectors", "No reference images provided")]
        [IgnoreGraphicsTest("13_Decals", "No reference images provided")]
        [IgnoreGraphicsTest("32_ExcludeFromTAA", "No reference images provided")]
        [IgnoreGraphicsTest("34_ShaderGraphGeneration", "No reference images provided")]
        [IgnoreGraphicsTest("ShadergraphSampleScene", "Unstable in QV")]
        [IgnoreGraphicsTest("Shapes", "No reference images provided")]
        [IgnoreGraphicsTest("StripsMotionVector", "No reference images provided")]
        [IgnoreGraphicsTest("020_PrefabInstanciation", "No reference images provided")]
        [IgnoreGraphicsTest("021_Check_Garbage_OutputEvent", "No reference images provided")]
        [IgnoreGraphicsTest("021_Check_Garbage_Spawner", "No reference images provided")]
        [IgnoreGraphicsTest("022_Repro_Crash_Null_Indexbuffer", "No reference images provided")]
        [IgnoreGraphicsTest("023_Check_Garbage_Timeline", "No reference images provided")]
        [IgnoreGraphicsTest("026_InstancingGPUevents", "See UUM-88671", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("36_SkinnedSDF", "See UUM-66822", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxOne })]
        [IgnoreGraphicsTest("39_SmokeLighting_APV", "Too many bindings when using APVs", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("102_ShadergraphShadow", "See UUM-96202", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("Repro_SampleGradient_Branch_Instancing", "Compute shader ([Repro_SampleGradient_Branch_Instancing] [Minimal] Update Particles): Property (Repro_SampleGradient_Branch_Instancing_Buffer) at kernel index (0) is not set")]
        [IgnoreGraphicsTest("Empty", "No reference images provided")]
        [IgnoreGraphicsTest("Empty_With_Camera", "No reference images provided")]
        [IgnoreGraphicsTest("StressTestRuntime_GPUEvent", "No reference images provided")]
        [IgnoreGraphicsTest("Timeline_FirstFrame", "No reference images provided")]
        [IgnoreGraphicsTest("NamedObject_ExposedProperties", "No reference images provided")]
        [IgnoreGraphicsTest("PrewarmCompute", "No reference images provided")]

        [MockHmdSetup(99)]
        [AssetBundleSetup]
        [PerformanceTestSettingsSetup]
        [Timeout(600 * 1000), Version("1"), Performance, UnityTest]
        [VfxPerformanceGraphicsTest("Assets/AllTests/VFXTests/GraphicsTests", "Packages/com.unity.testing.visualeffectgraph/Scenes")]
        public IEnumerator Counters(
            SceneGraphicsTestCase testCase
        )
        {
            yield return VisualEffectsGraphRuntimePerformanceTests.Counters(testCase);
        }

#if ENABLE_VR
        [TearDown]
        public void TearDownXR()
        {
            XRGraphicsAutomatedTests.running = false;
        }
#endif
    }

    public class VFXRuntimeMemoryTests : PerformanceTests
    {
        [IgnoreGraphicsTest("05_MotionVectors", "No reference images provided")]
        [IgnoreGraphicsTest("13_Decals", "No reference images provided")]
        [IgnoreGraphicsTest("32_ExcludeFromTAA", "No reference images provided")]
        [IgnoreGraphicsTest("34_ShaderGraphGeneration", "No reference images provided")]
        [IgnoreGraphicsTest("ShadergraphSampleScene", "Unstable in QV")]
        [IgnoreGraphicsTest("Shapes", "No reference images provided")]
        [IgnoreGraphicsTest("StripsMotionVector", "No reference images provided")]
        [IgnoreGraphicsTest("020_PrefabInstanciation", "No reference images provided")]
        [IgnoreGraphicsTest("021_Check_Garbage_OutputEvent", "No reference images provided")]
        [IgnoreGraphicsTest("021_Check_Garbage_Spawner", "No reference images provided")]
        [IgnoreGraphicsTest("022_Repro_Crash_Null_Indexbuffer", "No reference images provided")]
        [IgnoreGraphicsTest("023_Check_Garbage_Timeline", "No reference images provided")]
        [IgnoreGraphicsTest("026_InstancingGPUevents", "See UUM-88671", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("36_SkinnedSDF", "See UUM-66822", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxOne })]
        [IgnoreGraphicsTest("39_SmokeLighting_APV", "Too many bindings when using APVs", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("102_ShadergraphShadow", "See UUM-96202", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("Repro_SampleGradient_Branch_Instancing", "Compute shader ([Repro_SampleGradient_Branch_Instancing] [Minimal] Update Particles): Property (Repro_SampleGradient_Branch_Instancing_Buffer) at kernel index (0) is not set")]
        [IgnoreGraphicsTest("Empty", "No reference images provided")]
        [IgnoreGraphicsTest("Empty_With_Camera", "No reference images provided")]
        [IgnoreGraphicsTest("StressTestRuntime_GPUEvent", "No reference images provided")]
        [IgnoreGraphicsTest("Timeline_FirstFrame", "No reference images provided")]
        [IgnoreGraphicsTest("NamedObject_ExposedProperties", "No reference images provided")]
        [IgnoreGraphicsTest("PrewarmCompute", "No reference images provided")]

        [MockHmdSetup(99)]
        [AssetBundleSetup]
        [PerformanceTestSettingsSetup]
        [Timeout(600 * 1000), Version("1"), UnityTest, Performance, Order(0)]
        [VfxPerformanceGraphicsTest("Assets/AllTests/VFXTests/GraphicsTests", "Packages/com.unity.testing.visualeffectgraph/Scenes")]
        public IEnumerator Memory(
            SceneGraphicsTestCase testCase
        )
        {
            yield return VisualEffectsGraphRuntimeMemoryTests.Memory(testCase);
        }

        [PerformanceTestSettingsSetup]
        [Version("1"), UnityTest, Performance, Order(1000)]
        public IEnumerator RemainingMemoryAfterAllRun()
        {
            yield return VisualEffectsGraphRuntimeMemoryTests.RemainingMemoryAfterAllRun();
        }

#if ENABLE_VR
        [TearDown]
        public void TearDownXR()
        {
            XRGraphicsAutomatedTests.running = false;
        }
#endif
    }
}

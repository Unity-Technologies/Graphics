using System;
using System.Collections;
using NUnit.Framework;
using Unity.Testing.VisualEffectGraph.Tests;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.VFX.Test
{
    public class VFXGraphicsTests
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
        [IgnoreGraphicsTest("026_RWBuffer", "Unstable: https://jira.unity3d.com/browse/UUM-119810")]
        [IgnoreGraphicsTest("36_SkinnedSDF", "See UUM-66822 and VFXG-539", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxOne, RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("39_SmokeLighting_APV", "Too many bindings when using APVs", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("102_ShadergraphShadow", "See UUM-96202", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("015_FixedTime", "See UUM-109089", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
        [IgnoreGraphicsTest("DebugAlbedo", "Onscreen assert", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch2 })]
        [IgnoreGraphicsTest("36_SkinnedSDF", "Onscreen assert", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch2 })]
        [IgnoreGraphicsTest("015_FixedTime", "Onscreen assert", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch2 })]
        [IgnoreGraphicsTest("Repro_SampleGradient_Branch_Instancing", "Compute shader ([Repro_SampleGradient_Branch_Instancing] [Minimal] Update Particles): Property (Repro_SampleGradient_Branch_Instancing_Buffer) at kernel index (0) is not set")]
        [IgnoreGraphicsTest("Empty", "No reference images provided")]
        [IgnoreGraphicsTest("Empty_With_Camera", "No reference images provided")]
        [IgnoreGraphicsTest("StressTestRuntime_GPUEvent", "No reference images provided")]
        [IgnoreGraphicsTest("Timeline_FirstFrame", "No reference images provided")]
        [IgnoreGraphicsTest("NamedObject_ExposedProperties", "No reference images provided")]
        [IgnoreGraphicsTest("PrewarmCompute", "No reference images provided")]

        [MockHmdSetup(99)]
        [AssetBundleSetup]
        [UnityTest, Category("VisualEffect")]
        [SceneGraphicsTest("Assets/AllTests/VFXTests/GraphicsTests", "Packages/com.unity.testing.visualeffectgraph/Scenes")]
        [Timeout(450 * 1000)] // Increase timeout to handle complex scenes with many shaders and XR variants
        public IEnumerator Run(
            SceneGraphicsTestCase testCase
        )
        {
            yield return VisualEffectsGraphGraphicsTests.Run(testCase);
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

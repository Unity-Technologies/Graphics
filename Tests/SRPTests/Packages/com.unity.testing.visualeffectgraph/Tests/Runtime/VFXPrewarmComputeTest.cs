using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Testing.VisualEffectGraph;
using Unity.Testing.VisualEffectGraph.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.TestTools;


namespace UnityEngine.VFX.Test
{
    [TestFixture]
    [AssetBundleSetup]
    public class VFXPrewarmComputeTest
    {
        private const string kScenePath = "Packages/com.unity.testing.visualeffectgraph/Scenes/PrewarmCompute.unity";
        private int m_PreviousFrameRate;
        AssetBundle m_AssetBundle;
        private List<ProfilerRecorder> m_Recorders;

        [OneTimeSetUp]
        public void Init()
        {
            m_PreviousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 30;
            m_AssetBundle = AssetBundleHelper.Load("scene_in_assetbundle");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Application.targetFrameRate = m_PreviousFrameRate;
            if (m_Recorders != null)
            {
                foreach (var recorder in m_Recorders)
                {
                    recorder.Stop();
                }
            }
            AssetBundleHelper.Unload(m_AssetBundle);
        }

        [UnityTest]
        [DeviceFilter(GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Vulkan, GraphicsDeviceType.Metal)]
        public IEnumerator All_Compute_Shaders_Prewarmed()
        {
            // Find the CreateComputePipelineImpl marker
            m_Recorders = new List<ProfilerRecorder>();
            var allHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(allHandles);
            foreach (var handle in allHandles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.Name.Contains("CreateComputePipelineImpl"))
                {
                    ProfilerRecorder recorder = new ProfilerRecorder(handle, 1,  (ProfilerRecorderOptions)128 |
                                                                                 ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
                                                                                 ProfilerRecorderOptions.SumAllSamplesInFrame |
                                                                                 ProfilerRecorderOptions.StartImmediately);
                    recorder.Start();
                    m_Recorders.Add(recorder);
                }
            }
            if (m_Recorders.Count == 0)
            {
                Assert.Fail("Could not find CreateComputePipeline marker");
            }

            // Load the scene
            var vfxBundle = AssetBundleHelper.Load("vfx_in_assetbundle");
            var prefab = vfxBundle.LoadAsset($"Packages/com.unity.testing.visualeffectgraph/Scenes/PrewarmComputeFX.prefab") as GameObject;
            UnityEngine.SceneManagement.SceneManager.LoadScene(kScenePath);
            yield return null;

            // Prewarm compute shaders
            var vfxComponents = Object.FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var vfxComponent in vfxComponents)
                vfxComponent.visualEffectAsset.PrewarmComputeShaders();

            // Wait for a few frames to collect profiler samples
            int prewarmSamples = 0;
            int maxWaitFrames = 100;
            do
            {
                foreach (var recorder in m_Recorders)
                {
                    prewarmSamples += (int)recorder.GetSample(0).Count;
                }
                yield return new WaitForEndOfFrame();
            } while (maxWaitFrames-- > 0);

            Assert.Greater(prewarmSamples, 0);

            // Activate VFX and ensure no more compute shaders are created on the fly
            foreach (var vfxComponent in vfxComponents)
            {
                vfxComponent.gameObject.SetActive(true);
            }
            int onTheFlySamples = 0;

            for (int i = 0; i < 32; i++)
            {
                foreach (var recorder in m_Recorders)
                {
                    onTheFlySamples += (int)recorder.GetSample(0).Count;
                }
                yield return new WaitForEndOfFrame();
            }

            Assert.AreEqual(0, onTheFlySamples);
            AssetBundleHelper.Unload(vfxBundle);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Testing.VisualEffectGraph;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.VFX.Test
{
    [TestFixture]
    [PrebuildSetup("SetupGraphicsTestCases")]
    public class VFXStressTests
    {

        AssetBundle m_AssetBundle;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_AssetBundle = AssetBundleHelper.Load("scene_in_assetbundle");
        }

        [OneTimeTearDown]
        public void TearDown()
        {

#if UNITY_EDITOR
            while (SceneView.sceneViews.Count > 0)
            {
                var sceneView = SceneView.sceneViews[0] as SceneView;
                sceneView.Close();
            }
#endif
            AssetBundleHelper.Unload(m_AssetBundle);
        }

        private static readonly string[] kRuntimeScene = new[] { "GPUEvent" };

        [UnityTest]
        public IEnumerator Runtime([ValueSource(nameof(kRuntimeScene))] string scene)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene($"Packages/com.unity.testing.visualeffectgraph/Scenes/StressTestRuntime_{scene}.unity");
            yield return null;

            for (int i = 0; i < 128; ++i)
                yield return new WaitForEndOfFrame();
        }

        static float RandomFloat(System.Random source)
        {
            return source.Next(-100,100) / 100.0f;
        }

        public IEnumerator ProccessMemoryTest(string vfxAssetPath)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Packages/com.unity.testing.visualeffectgraph/Scenes/Empty_With_Camera.unity");
            yield return null;

            var mainCamera = Camera.main;
            Assert.IsNotNull(mainCamera);

            var vfxBundle = AssetBundleHelper.Load("vfx_in_assetbundle");

            var prefab = vfxBundle.LoadAsset($"Packages/com.unity.testing.visualeffectgraph/Scenes/StressTestMemory_{vfxAssetPath}.prefab") as GameObject;
            Assert.IsNotNull(prefab);
            var prefabVfx = prefab.GetComponent<VisualEffect>();
            Assert.IsNotNull(prefabVfx);
            var vfxAsset = prefabVfx.visualEffectAsset;
            Assert.IsNotNull(vfxAsset);

            var gameObjects = new List<GameObject>();
            var random = new System.Random(0x12345);
            for (int pass = 0; pass < 8; ++pass)
            {
                if (gameObjects.Count > 0)
                {
                    bool vfxAlive = false;
                    foreach (var vfx in VFXManager.GetComponents())
                    {
                        if (vfx.culled == false && vfx.aliveParticleCount > 0)
                        {
                            vfxAlive = true;
                            break;
                        }
                    }
                    Assert.IsTrue(vfxAlive);

                    foreach (var gameObject in gameObjects)
                        Object.Destroy(gameObject);
                }

                for (int subPass = 0; subPass < 8; ++subPass)
                {
                    for (int vfx = 0; vfx < 128; vfx++)
                    {
                        var newGameObject = new GameObject("VFX_" + vfx, typeof(VisualEffect));
                        newGameObject.transform.Translate(new Vector3(RandomFloat(random), RandomFloat(random), RandomFloat(random)));
                        newGameObject.GetComponent<VisualEffect>().visualEffectAsset = vfxAsset;
                        gameObjects.Add(newGameObject);
                    }
                    yield return new WaitForEndOfFrame();
                }

                yield return new WaitForSeconds(0.1f);
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Packages/com.unity.testing.visualeffectgraph/Scenes/Empty.unity");
            yield return new WaitForEndOfFrame();
            AssetBundleHelper.Unload(vfxBundle);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(0, VFXManager.GetComponents().Length);
            GC.Collect();
            yield return Resources.UnloadUnusedAssets();
            yield return new WaitForEndOfFrame();
        }

        private static void LogDiff(StringBuilder str, string value, long a, long b)
        {
            str.Append($"{value}: {a} => {b} ({((b - a) / (double)a)*100.0}%)");
            str.AppendLine();
        }

        private static readonly string[] kMemoryVFX = new[] { "SmallBatch", "BigBatch" };
        [UnityTest, Ignore("Inspired from repro step of UUM-52800, this suite is too long and not stable enough to be run on Yamato.")]
        public IEnumerator MemoryTest([ValueSource(nameof(kMemoryVFX))] string vfxAssetPath)
        {
            var totalUsedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            var totalReservedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            var gpuUsedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Gfx Used Memory");
            var gpuReservedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Gfx Reserved Memory");

            //Pre-allocating run
            for (var preload = 0; preload < 2; ++preload)
                yield return ProccessMemoryTest(vfxAssetPath);

            Assert.AreNotEqual((long)0, totalUsedMemory.LastValue);
            Assert.AreNotEqual((long)0, totalReservedMemory.LastValue);
            Assert.AreNotEqual((long)0, gpuUsedMemory.LastValue);
            Assert.AreNotEqual((long)0, gpuReservedMemory.LastValue);

            var previousTotalUsedMemory = totalUsedMemory.LastValue;
            var previousTotalReservedMemory = totalReservedMemory.LastValue;
            var previousGpuUsedMemory = gpuUsedMemory.LastValue;
            var previousGpuReservedMemory = gpuReservedMemory.LastValue;

            //Actual run
            yield return ProccessMemoryTest(vfxAssetPath);

            var currentTotalUsedMemory = totalUsedMemory.LastValue;
            var currentTotalReservedMemory = totalReservedMemory.LastValue;
            var currentGpuUsedMemory = gpuUsedMemory.LastValue;
            var currentGpuReservedMemory = gpuReservedMemory.LastValue;

            var stringBuilder = new StringBuilder("================== MemoryTest ==================\n");
            LogDiff(stringBuilder,"System Used Memory", previousTotalUsedMemory, currentTotalUsedMemory);
            LogDiff(stringBuilder,"Total Reserved Memory", previousTotalReservedMemory, currentTotalReservedMemory);
            LogDiff(stringBuilder,"Gfx Used Memory", previousGpuUsedMemory, currentGpuUsedMemory);
            LogDiff(stringBuilder,"Gfx Reserved Memory", previousGpuReservedMemory, currentGpuReservedMemory);
            var summary = stringBuilder.ToString();

            Assert.LessOrEqual((double)currentTotalUsedMemory, previousTotalUsedMemory, summary);
            Assert.LessOrEqual((double)currentTotalReservedMemory, previousTotalReservedMemory, summary);
            Assert.LessOrEqual((double)currentGpuUsedMemory, previousGpuUsedMemory, summary);
            Assert.LessOrEqual((double)currentGpuReservedMemory, previousGpuReservedMemory, summary);

            totalUsedMemory.Dispose();
            totalReservedMemory.Dispose();
            gpuUsedMemory.Dispose();
            gpuReservedMemory.Dispose();
        }
    }
}

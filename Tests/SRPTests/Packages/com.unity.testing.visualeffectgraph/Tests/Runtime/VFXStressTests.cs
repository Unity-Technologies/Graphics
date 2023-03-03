using System;
using System.Collections;
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

        private static readonly string kGPUEventScene = "GPUEvent_StressTest";

        [UnityTest]
        public IEnumerator GPUEvent()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(string.Format("Packages/com.unity.testing.visualeffectgraph/Scenes/{0}.unity", kGPUEventScene));
            yield return null;

            for (int i = 0; i < 128; ++i)
                yield return new WaitForEndOfFrame();
        }
    }
}

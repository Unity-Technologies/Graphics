using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;


namespace UnityEngine.VFX.Test
{
    [TestFixture]
    public class VFXCameraCullingTest
    {
        Recorder m_VFXSortRecorder;
        private const string kScenePath = "Packages/com.unity.testing.visualeffectgraph/Scenes/009_MultiCamera.unity";

        private const int kCameraVisibleCount = 4;
        //The camera command markers appear twice in the main thread
        //Once in the Render Pipeline preparation, once in the Render Context Submit.
        private const int kMarkerMultiplier = 2;


        [OneTimeSetUp]
        public void Init()
        {
        }

        [UnityTest]
        public IEnumerator Ensure_Camera_Commands_Are_Culled()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(kScenePath);
            yield return null;

            m_VFXSortRecorder = Recorder.Get("VFX.SortBuffer");
            m_VFXSortRecorder.FilterToCurrentThread();

            m_VFXSortRecorder.enabled = true;
            yield return new WaitForEndOfFrame();;
            m_VFXSortRecorder.enabled = false;

            int sortBufferCommandCount = m_VFXSortRecorder.sampleBlockCount;
            Debug.Log($"Sort Buffer Counts : {sortBufferCommandCount}");
            Assert.AreEqual(sortBufferCommandCount, kCameraVisibleCount * kMarkerMultiplier);
        }
    }
}

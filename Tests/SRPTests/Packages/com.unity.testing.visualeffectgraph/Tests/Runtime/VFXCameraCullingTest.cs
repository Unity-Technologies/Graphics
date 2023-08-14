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

        private const int kWaitFrameCount = 16;
        [OneTimeSetUp]
        public void Init()
        {
            m_VFXSortRecorder = Recorder.Get("VFX.SortBuffer");
            m_VFXSortRecorder.FilterToCurrentThread();
            m_VFXSortRecorder.enabled = false;
        }

        [UnityTest]
        public IEnumerator Ensure_Camera_Commands_Are_Culled()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(kScenePath);
            yield return null;

            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();

            int maxFrame = 8;
            while (vfxComponents[^1].culled && maxFrame-- > 0)
                yield return new WaitForEndOfFrame();

            m_VFXSortRecorder.enabled = true;
            for(int i = 0; i < kWaitFrameCount; i++)
                yield return new WaitForEndOfFrame();
            m_VFXSortRecorder.enabled = false;

            int sortBufferCommandCount = m_VFXSortRecorder.sampleBlockCount;
            Debug.Log($"Sort Buffer Counts : {sortBufferCommandCount}");
            Assert.AreEqual(kCameraVisibleCount * kMarkerMultiplier, sortBufferCommandCount);
        }
    }
}

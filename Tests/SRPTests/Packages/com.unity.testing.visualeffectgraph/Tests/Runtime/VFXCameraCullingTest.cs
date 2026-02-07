using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;


namespace UnityEngine.VFX.Test
{
    [TestFixture]
    public class VFXCameraCullingTest
    {
        Recorder m_VFXSortRecorder;
        Recorder m_VFXProcessCameraRecorder;
        private const string kScenePath = "Packages/com.unity.testing.visualeffectgraph/Scenes/009_MultiCamera.unity";
        private const int kCameraVisibleCount = 4;
        private const int kCameraTotalCount = 6;
        //The camera command markers appear twice in the main thread
        //Once in the Render Pipeline preparation, once in the Render Context Submit.
        private const int kMarkerMultiplier = 2;
        private const int kWaitFrameCount = 32;
        private int m_PreviousFrameRate;

        [OneTimeSetUp]
        public void Init()
        {
            m_VFXSortRecorder = Recorder.Get("VFX.SortBuffer");
            m_VFXSortRecorder.FilterToCurrentThread();
            m_VFXSortRecorder.enabled = false;
            m_VFXProcessCameraRecorder = Recorder.Get("VFX.ProcessCamera");
            m_VFXProcessCameraRecorder.FilterToCurrentThread();
            m_VFXProcessCameraRecorder.enabled = false;
            m_PreviousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 30;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Application.targetFrameRate = m_PreviousFrameRate;
        }

        [UnityTest]
        public IEnumerator Ensure_Camera_Commands_Are_Culled()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(kScenePath);
            yield return null;

            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();

            m_VFXSortRecorder.enabled = true;
            m_VFXProcessCameraRecorder.enabled = true;

            int maxFrame = 8;
            while (vfxComponents[^1].culled && maxFrame-- > 0)
                yield return new WaitForEndOfFrame();

            //Extra wait frame to ensure that the profiler recorder is ready
            yield return new WaitForEndOfFrame();

            bool foundValidFrame = false;
            for (int i = 0; i < kWaitFrameCount; i++)
            {
                if (m_VFXProcessCameraRecorder.sampleBlockCount == kCameraTotalCount * kMarkerMultiplier)
                {
                    foundValidFrame = true;
                    Assert.AreEqual(kCameraVisibleCount * kMarkerMultiplier, m_VFXSortRecorder.sampleBlockCount);
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            Assert.IsTrue(foundValidFrame, $"No valid frame with {kCameraTotalCount * kMarkerMultiplier} VFX.ProcessCamera markers could be found");
        }
    }
}

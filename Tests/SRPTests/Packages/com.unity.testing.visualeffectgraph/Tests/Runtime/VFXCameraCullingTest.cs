using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;


namespace UnityEngine.VFX.Test
{
    [TestFixture]
    public class VFXCameraCullingTest
    {
        ProfilerRecorder m_VFXSortRecorder;
        ProfilerRecorder m_VFXProcessCameraRecorder;
        private const string kScenePath = "Packages/com.unity.testing.visualeffectgraph/Scenes/009_MultiCamera.unity";
        private const int kCameraVisibleCount = 4;
        private const int kCameraTotalCount = 6;
        //The camera command markers appear twice in the main thread
        //Once in the Render Pipeline preparation, once in the Render Context Submit.
        private const int kMarkerMultiplier = 2;
        private const int kWaitFrameCount = 32;
        private const ProfilerRecorderOptions k_RecorderOptions =
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.SumAllSamplesInFrame |
            ProfilerRecorderOptions.WrapAroundWhenCapacityReached;
        private int m_PreviousFrameRate;

        [OneTimeSetUp]
        public void Init()
        {
            m_VFXSortRecorder = new ProfilerRecorder("VFX.SortBuffer", 1, k_RecorderOptions);
            m_VFXProcessCameraRecorder = new ProfilerRecorder("VFX.ProcessCamera", 1, k_RecorderOptions);
            m_PreviousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 30;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_VFXSortRecorder.Dispose();
            m_VFXProcessCameraRecorder.Dispose();
            Application.targetFrameRate = m_PreviousFrameRate;
        }

        static int GetSampleBlockCount(ProfilerRecorder recorder)
        {
            return recorder.Count == 1 ? (int)recorder.GetSample(0).Count : 0;
        }

        [UnityTest]
        public IEnumerator Ensure_Camera_Commands_Are_Culled()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(kScenePath);
            yield return null;

            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();

            m_VFXSortRecorder.Start();
            m_VFXProcessCameraRecorder.Start();

            int maxFrame = 8;
            while (vfxComponents[^1].culled && maxFrame-- > 0)
                yield return new WaitForEndOfFrame();

            //Extra wait frame to ensure that the profiler recorder is ready
            yield return new WaitForEndOfFrame();

            bool foundValidFrame = false;
            for (int i = 0; i < kWaitFrameCount; i++)
            {
                if (GetSampleBlockCount(m_VFXProcessCameraRecorder) == kCameraTotalCount * kMarkerMultiplier)
                {
                    foundValidFrame = true;
                    Assert.AreEqual(kCameraVisibleCount * kMarkerMultiplier, GetSampleBlockCount(m_VFXSortRecorder));
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            Assert.IsTrue(foundValidFrame, $"No valid frame with {kCameraTotalCount * kMarkerMultiplier} VFX.ProcessCamera markers could be found");
        }
    }
}

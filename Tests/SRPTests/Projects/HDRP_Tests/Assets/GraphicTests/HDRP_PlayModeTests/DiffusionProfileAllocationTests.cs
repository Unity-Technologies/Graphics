using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics.Allocations;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class DiffusionProfileAllocationTests
    {
        const int k_AllowedPoolRefillIterations = 6;

        GameObject m_CameraObject;
        GameObject m_VolumeObject;
        Camera m_Camera;
        RenderTexture m_Target;
        VolumeProfile m_Profile;
        DiffusionProfileSettings m_DiffusionProfile;
        AllocationLogWatcher m_Logs;

        [OneTimeSetUp]
        public void AllocationFixtureSetUp()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
            {
                Assert.Ignore("This fixture measures HDRP's volume update, so it only runs when HDRP is "
                    + "the active render pipeline.");
            }
        }

        [SetUp]
        public void AllocationTestSetUp()
        {
            m_Logs = new AllocationLogWatcher();
            m_Logs.Start();

            m_DiffusionProfile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();

            m_Profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var diffusionProfileList = m_Profile.Add<DiffusionProfileList>(true);
            diffusionProfileList.diffusionProfiles.value = new[] { m_DiffusionProfile };

            m_VolumeObject = new GameObject("DiffusionProfileAllocationVolume");
            var volume = m_VolumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = m_Profile;

            m_Target = new RenderTexture(640, 360, 24);
            m_CameraObject = new GameObject("DiffusionProfileAllocationCamera");
            m_Camera = m_CameraObject.AddComponent<Camera>();
            m_CameraObject.AddComponent<HDAdditionalCameraData>();
            m_Camera.targetTexture = m_Target;
            m_Camera.Render();

            Assert.That(RenderPipelineManager.currentPipeline, Is.InstanceOf<HDRenderPipeline>());
        }

        [TearDown]
        public void AllocationTestTearDown()
        {
            var hasErrors = m_Logs.hasErrors;
            var describedLogs = m_Logs.Describe();
            m_Logs.Dispose();

            m_Camera.targetTexture = null;
            Object.DestroyImmediate(m_CameraObject);
            Object.DestroyImmediate(m_VolumeObject);
            m_Target.Release();
            Object.DestroyImmediate(m_Target);
            Object.DestroyImmediate(m_Profile);
            Object.DestroyImmediate(m_DiffusionProfile);

            Assert.That(hasErrors, Is.False, describedLogs);
        }

        [UnityTest]
        public IEnumerator FrameWithDiffusionProfileOverride_DoesNotAllocatePerFrame()
        {
            var detector = new AllocationDetector
            {
                logWatcher = m_Logs,
                allowedAllocatingIterations = k_AllowedPoolRefillIterations
            };

            AllocationResult result = null;
            var frames = detector.RunIncremental(
                nameof(FrameWithDiffusionProfileOverride_DoesNotAllocatePerFrame),
                "HDRP global volume with a diffusion profile override",
                m_Camera.Render,
                r => result = r);

            while (frames.MoveNext())
                yield return frames.Current;

            var resolved = HDCamera.GetOrCreate(m_Camera).volumeStack.GetComponent<DiffusionProfileList>();
            Assert.That(resolved.diffusionProfiles.value, Does.Contain(m_DiffusionProfile),
                "The override never reached the camera's volume stack, so the measured frames did not "
                + "interpolate a diffusion profile list and the allocation result means nothing.");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.passed, Is.True, result.Report());
        }
    }
}

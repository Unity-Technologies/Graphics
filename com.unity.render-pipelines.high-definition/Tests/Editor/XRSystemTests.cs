using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class XRSystemTests
    {
        XRSystem xrSystem;
        Camera[] cameras;

        // Simulate multiple frames with many cameras, passes and views
        const int k_FrameCount = 90;
        const int k_CameraCount = 3;
        const int k_PassCount = 4;
        const int k_ViewCount = 2;

        [SetUp]
        public void SetUp()
        {
            xrSystem = new XRSystem(null);
            TextureXR.maxViews = k_ViewCount;
            cameras = new Camera[k_CameraCount];
            for (int cameraIndex = 0; cameraIndex < k_CameraCount; ++cameraIndex)
            {
                var cameraGameObject = new GameObject();
                cameras[cameraIndex] = cameraGameObject.AddComponent<Camera>();
            }

            SimulateOneFrame();
        }

        [TearDown]
        public void TearDown()
        {
            xrSystem = null;
            cameras = null;
        }

        public void SimulateOneFrame()
        {
            foreach (var camera in cameras)
            {
                for (int passIndex = 0; passIndex < k_PassCount; ++passIndex)
                {
                    var passCreateInfo = new XRPassCreateInfo
                    {
                        multipassId = 0,
                        cullingPassId = 0,
                        cullingParameters = new ScriptableCullingParameters(),
                        renderTarget = camera.targetTexture,
                        customMirrorView = null
                    };

                    var xrPass = XRPass.Create(passCreateInfo);

                    for (int viewIndex = 0; viewIndex < k_ViewCount; ++viewIndex)
                    {
                        xrPass.AddViewInternal(new XRView());
                    }

                    xrSystem.AddPassToFrame(camera, xrPass);
                }
            }

            xrSystem.ReleaseFrame();
        }

        [Test]
        public void ZeroGCMemoryPerFrame()
        {
            for (int i = 0; i < k_FrameCount; ++i)
            {
                Assert.That(() => SimulateOneFrame(), Is.Not.AllocatingGCMemory());
            }
        }
    }
}

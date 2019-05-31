using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public class XRSystemTests
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
            xrSystem = new XRSystem();
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
                    var xrPass = XRPass.Create(passIndex);

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

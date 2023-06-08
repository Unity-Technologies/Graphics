#if ENABLE_VR && ENABLE_XR_MODULE
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Tests
{
    class XRDisplaySubsystemCoreTests
    {
        // Utils
        protected IEnumerator WaitOneFrame()
        {
            yield return null;
        }

        Camera m_Camera;
        XRDisplaySubsystem m_XRDisplay;
        private RenderPipelineAsset m_RenderPipelineAsset;

        [SetUp]
        public void Setup()
        {
            var camObj = new GameObject();
            camObj.AddComponent<Camera>();
            m_Camera = camObj.GetComponent<Camera>();
            camObj.tag = "MainCamera";

            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);

            // Skip tests
            if (displays.Count == 0)
                Assert.Ignore("No active XR provider found, skipping XRSystem core tests.");

            m_XRDisplay = displays[0];
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Camera)
            {
                Object.Destroy(m_Camera.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator SetOcclusionMeshScaleTest()
        {
            XRSRPSettings.occlusionMeshScale = 1.0f;
            yield return WaitOneFrame();

            // Set scale
            float targetScale = 0.5f;
            XRSRPSettings.occlusionMeshScale = targetScale;
            yield return WaitOneFrame();

            // Examine XRPass
            float occlusionScale = 0;
            var xrLayout = XRSystem.NewLayout();
            xrLayout.AddCamera(m_Camera, true);
            if (xrLayout.GetActivePasses().Count > 0)
            {
                (Camera _, XRPass xrPass) = xrLayout.GetActivePasses()[0];
                {
                    occlusionScale = xrPass.occlusionMeshScale;
                }
            }
            Assert.AreEqual(targetScale, occlusionScale);
            yield return WaitOneFrame();

            // Reset to default 1.0f
            XRSRPSettings.occlusionMeshScale = 1.0f;
            yield return WaitOneFrame();
        }

        [UnityTest]
        public IEnumerator SetMirrorViewModeTest()
        {
            XRSRPSettings.mirrorViewMode = XRMirrorViewBlitMode.SideBySide;
            yield return WaitOneFrame();
            // Check XRDisplay mirror blit mode
            int mirrorBlitMode = m_XRDisplay.GetPreferredMirrorBlitMode();
            Assert.AreEqual(mirrorBlitMode, XRMirrorViewBlitMode.SideBySide);

            // Repeat for Left mode
            XRSRPSettings.mirrorViewMode = XRMirrorViewBlitMode.LeftEye;
            yield return WaitOneFrame();
            // Check XRDisplay mirror blit mode
            mirrorBlitMode = m_XRDisplay.GetPreferredMirrorBlitMode();
            Assert.AreEqual(mirrorBlitMode, XRMirrorViewBlitMode.LeftEye);
        }

        [UnityTest]
        public IEnumerator CalculateViewCornersTest()
        {
            yield return WaitOneFrame();

            // Retrieve XRPass
            var xrLayout = XRSystem.NewLayout();
            xrLayout.AddCamera(m_Camera, true);
            XRPass firstPass = XRSystem.emptyPass;
            if (xrLayout.GetActivePasses().Count > 0)
            {
                (Camera _, XRPass xrPass) = xrLayout.GetActivePasses()[0];
                {
                    firstPass = xrPass;
                }
            }

            // z 0 case: all corners should be zero
            float z0 = 0;
            Vector3[] corners = CoreUtils.CalculateViewSpaceCorners(firstPass.GetProjMatrix(0), z0);
            Assert.AreEqual(Vector3.zero, corners[0]);
            Assert.AreEqual(Vector3.zero, corners[1]);
            Assert.AreEqual(Vector3.zero, corners[2]);
            Assert.AreEqual(Vector3.zero, corners[3]);

            // z2 corners = z1 corners * z2/z1 
            float z1 = 1;
            float z2 = 2;
            Vector3[] cornersZ1 = CoreUtils.CalculateViewSpaceCorners(firstPass.GetProjMatrix(0), z1 /*z*/);
            Vector3[] cornersZ2 = CoreUtils.CalculateViewSpaceCorners(firstPass.GetProjMatrix(0), z2 /*z*/);
            Assert.AreEqual(cornersZ1[0] * z2 / z1, cornersZ2[0]);
            Assert.AreEqual(cornersZ1[1] * z2 / z1, cornersZ2[1]);
            Assert.AreEqual(cornersZ1[2] * z2 / z1, cornersZ2[2]);
            Assert.AreEqual(cornersZ1[3] * z2 / z1, cornersZ2[3]);
        }
    }
}
#endif

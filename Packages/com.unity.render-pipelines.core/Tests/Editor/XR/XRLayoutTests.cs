using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Experimental.Tests.XR
{
    [TestFixture]
   class XRLayoutTests
    {
        private XRDisplaySubsystem m_CurrentSubsystem;
        private Camera m_Camera;
        private XRLayout m_LayoutTest = new ();
        private int m_SavedMaxViews;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject(nameof(XRLayoutTests));
            m_Camera = go.AddComponent<Camera>();

            // TextureXR.slices defaults to 1 without an XR device, which prevents
            // AddView from accepting more than one view. Raise the limit for tests
            // that construct multi-view passes.
            m_SavedMaxViews = TextureXR.slices;
            TextureXR.maxViews = 2;
        }

        [TearDown]
        public void TearDown()
        {
            m_LayoutTest.Clear();

            Object.DestroyImmediate(m_Camera.gameObject);

            TextureXR.maxViews = m_SavedMaxViews;

            Assert.IsEmpty(m_LayoutTest.GetActivePasses());
        }

        [Test]
        public void EmptyPassAreAdded()
        {
            const int k_Iterations = 5;
            for (int i = 0; i < k_Iterations; ++i)
            {
                m_LayoutTest.AddCamera(m_Camera, false);
            }
            
            Assert.AreEqual(k_Iterations, m_LayoutTest.GetActivePasses().Count);

            foreach (var pass in m_LayoutTest.GetActivePasses())
            {
                Assert.AreEqual(m_Camera, pass.Item1);
                Assert.AreEqual(XRSystem.emptyPass, pass.Item2);
            }
        }

        public static IEnumerable<TestCaseData> s_TestCasesMultiPass
        {
            get
            {
                yield return new TestCaseData(0, 0, 0);
                yield return new TestCaseData(1, 0, 0);
                yield return new TestCaseData(1, 1, 1);
                yield return new TestCaseData(2, 1, 2);
                yield return new TestCaseData(3, 2, 6);
                yield return new TestCaseData(20, 2, 40);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_TestCasesMultiPass))]
        public void CreateDefaultLayoutMockMultipass(int renderPassCount, int renderParameterCount, int expectedActivePassesCount)
        {
            Assert.AreEqual(expectedActivePassesCount, renderPassCount * renderParameterCount);

            for (int renderPassIndex = 0; renderPassIndex < renderPassCount; ++renderPassIndex)
            {
                for (int renderParamIndex = 0; renderParamIndex < renderParameterCount; ++renderParamIndex)
                {
                    m_LayoutTest.AddPass(m_Camera, new XRPass());
                }
            }

            Assert.AreEqual(expectedActivePassesCount, m_LayoutTest.GetActivePasses().Count);
        }

        public static IEnumerable<TestCaseData> s_TestCasesSinglePass
        {
            get
            {
                yield return new TestCaseData(0, 0, 0);
                yield return new TestCaseData(1, 0, 1);
                yield return new TestCaseData(1, 1, 1);
                yield return new TestCaseData(2, 1, 2);
                yield return new TestCaseData(3, 1, 3);
                yield return new TestCaseData(20, 1, 20);
            }
        }

        [Test]
        [TestCaseSource(nameof(s_TestCasesSinglePass))]
        public void CreateDefaultLayoutMockSinglepass(int renderPassCount, int renderParameterCount, int expectedActivePassesCount)
        {
            for (int renderPassIndex = 0; renderPassIndex < renderPassCount; ++renderPassIndex)
            {
                var xrPass = new XRPass();

                for (int renderParamIndex = 0; renderParamIndex < renderParameterCount; ++renderParamIndex)
                {
                    xrPass.AddView(new XRView());
                }

                m_LayoutTest.AddPass(m_Camera, xrPass);
            }

            Assert.AreEqual(expectedActivePassesCount, m_LayoutTest.GetActivePasses().Count);
        }

        static XRPass CreatePassWithLayout(XRLayoutType layoutType, int multipassId)
        {
            var createInfo = new XRPassCreateInfo
            {
                xrLayoutType = layoutType,
                multipassId = multipassId,
                uvScales = Vector4.one,
                uvOffsets = Vector4.zero,
            };
            var pass = new XRPass();
            pass.InitBase(createInfo);
            return pass;
        }

        [Test]
        public void SinglePassStereoLayout_HasOnePass()
        {
            var pass = CreatePassWithLayout(XRLayoutType.SinglePassStereo, multipassId: 0);
            pass.AddView(new XRView());
            pass.AddView(new XRView());
            m_LayoutTest.AddPass(m_Camera, pass);

            Assert.AreEqual(1, m_LayoutTest.GetActivePasses().Count);

            var (_, xrPass) = m_LayoutTest.GetActivePasses()[0];
            Assert.AreEqual(XRLayoutType.SinglePassStereo, xrPass.xrLayoutType);
            Assert.AreEqual(2, xrPass.viewCount);
            Assert.IsTrue(xrPass.isFirstCameraPass);
            Assert.IsTrue(xrPass.isLastCameraPass);
            Assert.IsFalse(xrPass.isQuadViewInnerPass);
        }

        [Test]
        public void TwoPassStereoLayout_HasTwoPasses()
        {
            for (int i = 0; i < 2; ++i)
            {
                var pass = CreatePassWithLayout(XRLayoutType.TwoPassStereo, multipassId: i);
                pass.AddView(new XRView());
                m_LayoutTest.AddPass(m_Camera, pass);
            }

            Assert.AreEqual(2, m_LayoutTest.GetActivePasses().Count);

            var (_, pass0) = m_LayoutTest.GetActivePasses()[0];
            Assert.AreEqual(XRLayoutType.TwoPassStereo, pass0.xrLayoutType);
            Assert.AreEqual(1, pass0.viewCount);
            Assert.IsTrue(pass0.isFirstCameraPass);
            Assert.IsFalse(pass0.isLastCameraPass);
            Assert.IsFalse(pass0.isQuadViewInnerPass);

            var (_, pass1) = m_LayoutTest.GetActivePasses()[1];
            Assert.AreEqual(XRLayoutType.TwoPassStereo, pass1.xrLayoutType);
            Assert.IsFalse(pass1.isFirstCameraPass);
            Assert.IsTrue(pass1.isLastCameraPass);
            Assert.IsFalse(pass1.isQuadViewInnerPass);
        }

        [Test]
        public void TwoPassQuadViewsLayout_HasPeripheralAndFovealPasses()
        {
            for (int i = 0; i < 2; ++i)
            {
                var pass = CreatePassWithLayout(XRLayoutType.TwoPassQuadViews, multipassId: i);
                pass.AddView(new XRView());
                pass.AddView(new XRView());
                m_LayoutTest.AddPass(m_Camera, pass);
            }

            Assert.AreEqual(2, m_LayoutTest.GetActivePasses().Count);

            // Pass 0: peripheral (outer) views
            var (_, peripheral) = m_LayoutTest.GetActivePasses()[0];
            Assert.AreEqual(XRLayoutType.TwoPassQuadViews, peripheral.xrLayoutType);
            Assert.AreEqual(2, peripheral.viewCount);
            Assert.IsTrue(peripheral.isFirstCameraPass);
            Assert.IsFalse(peripheral.isLastCameraPass);
            Assert.IsFalse(peripheral.isQuadViewInnerPass);

            // Pass 1: foveal (inner) views
            var (_, foveal) = m_LayoutTest.GetActivePasses()[1];
            Assert.AreEqual(XRLayoutType.TwoPassQuadViews, foveal.xrLayoutType);
            Assert.AreEqual(2, foveal.viewCount);
            Assert.IsFalse(foveal.isFirstCameraPass);
            Assert.IsTrue(foveal.isLastCameraPass);
            Assert.IsTrue(foveal.isQuadViewInnerPass);
        }
    }
}

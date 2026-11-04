using System;
using System.Collections.Generic;
using NUnit.Framework;
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
            XRSystem.ClearCameraRenderTargets();

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

        const ulong k_ContainerA = 1000;
        const ulong k_ContainerB = 2000;

        static Camera CreateSecondCamera() => new GameObject(nameof(XRLayoutTests) + ".Second").AddComponent<Camera>();

        [Test]
        public void Registry_Empty_HasNoTargetsAndCameraUnbound()
        {
            Assert.IsFalse(XRSystem.TryGetRenderTarget(m_Camera, out _));
            Assert.IsFalse(XRSystem.TryGetCameraForTarget(k_ContainerA, out _));
        }

        [Test]
        public void Registry_SetThenGet_RoundTrips()
        {
            XRSystem.SetCameraRenderTarget(k_ContainerA, m_Camera);

            Assert.IsTrue(XRSystem.TryGetRenderTarget(m_Camera, out var renderTargetId));
            Assert.AreEqual(k_ContainerA, renderTargetId);
            Assert.IsTrue(XRSystem.TryGetCameraForTarget(k_ContainerA, out var camera));
            Assert.AreEqual(m_Camera, camera);
        }

        [Test]
        public void Registry_ClearSingle_RemovesOnlyThatBinding()
        {
            var second = CreateSecondCamera();
            try
            {
                XRSystem.SetCameraRenderTarget(k_ContainerA, m_Camera);
                XRSystem.SetCameraRenderTarget(k_ContainerB, second);
                XRSystem.ClearCameraRenderTarget(k_ContainerA);

                Assert.IsFalse(XRSystem.TryGetRenderTarget(m_Camera, out _));
                Assert.IsFalse(XRSystem.TryGetCameraForTarget(k_ContainerA, out _));
                Assert.IsTrue(XRSystem.TryGetRenderTarget(second, out var renderTargetId));
                Assert.AreEqual(k_ContainerB, renderTargetId);
            }
            finally
            {
                Object.DestroyImmediate(second.gameObject);
            }
        }

        [Test]
        public void Registry_ClearAll_RemovesEveryBinding()
        {
            var second = CreateSecondCamera();
            try
            {
                XRSystem.SetCameraRenderTarget(k_ContainerA, m_Camera);
                XRSystem.SetCameraRenderTarget(k_ContainerB, second);
                XRSystem.ClearCameraRenderTargets();

                Assert.IsFalse(XRSystem.TryGetRenderTarget(m_Camera, out _));
                Assert.IsFalse(XRSystem.TryGetRenderTarget(second, out _));
            }
            finally
            {
                Object.DestroyImmediate(second.gameObject);
            }
        }

        [Test]
        public void Registry_SameCameraTwoTargets_IsRejected()
        {
            XRSystem.SetCameraRenderTarget(k_ContainerA, m_Camera);

            Assert.Throws<InvalidOperationException>(() => XRSystem.SetCameraRenderTarget(k_ContainerB, m_Camera));
            Assert.IsFalse(XRSystem.TryGetCameraForTarget(k_ContainerB, out _));
            Assert.IsTrue(XRSystem.TryGetRenderTarget(m_Camera, out var renderTargetId));
            Assert.AreEqual(k_ContainerA, renderTargetId);
        }

        [Test]
        public void Registry_SameTargetTwoCameras_IsRejected()
        {
            var second = CreateSecondCamera();
            try
            {
                XRSystem.SetCameraRenderTarget(k_ContainerA, m_Camera);

                Assert.Throws<InvalidOperationException>(() => XRSystem.SetCameraRenderTarget(k_ContainerA, second));
                Assert.IsTrue(XRSystem.TryGetCameraForTarget(k_ContainerA, out var camera));
                Assert.AreEqual(m_Camera, camera);
                Assert.IsFalse(XRSystem.TryGetRenderTarget(second, out _));
            }
            finally
            {
                Object.DestroyImmediate(second.gameObject);
            }
        }

        [Test]
        public void Registry_NullCamera_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => XRSystem.SetCameraRenderTarget(k_ContainerA, null));
            Assert.IsFalse(XRSystem.TryGetCameraForTarget(k_ContainerA, out _));
        }

        [Test]
        public void Registry_DefaultTargetId_IsReserved()
        {
            Assert.Throws<ArgumentException>(() => XRSystem.SetCameraRenderTarget(0, m_Camera));
            Assert.IsFalse(XRSystem.TryGetRenderTarget(m_Camera, out _));
        }

        public static IEnumerable<TestCaseData> s_TestCasesCollectTargetPasses
        {
            get
            {
                yield return new TestCaseData(new ulong[] { k_ContainerA, k_ContainerA, k_ContainerB }, k_ContainerA, new[] { 0, 1 }).SetName("Contiguous_FirstContainer");
                yield return new TestCaseData(new ulong[] { k_ContainerA, k_ContainerA, k_ContainerB }, k_ContainerB, new[] { 2 }).SetName("Contiguous_SecondContainer");
                yield return new TestCaseData(new ulong[] { k_ContainerA, k_ContainerB, k_ContainerA, k_ContainerB }, k_ContainerA, new[] { 0, 2 }).SetName("Interleaved_FirstContainer");
                yield return new TestCaseData(new ulong[] { k_ContainerA, k_ContainerB, k_ContainerA, k_ContainerB }, k_ContainerB, new[] { 1, 3 }).SetName("Interleaved_SecondContainer");
                yield return new TestCaseData(new ulong[] { k_ContainerA, k_ContainerA, k_ContainerA }, k_ContainerA, new[] { 0, 1, 2 }).SetName("MoreThanTwoMatches_ReturnsAll");
                yield return new TestCaseData(new ulong[] { k_ContainerB, k_ContainerB }, k_ContainerA, new int[0]).SetName("HiddenContainer_NoMatch");
                yield return new TestCaseData(new ulong[0], k_ContainerA, new int[0]).SetName("NoPasses_NoMatch");
                yield return new TestCaseData(new ulong[] { 0, 0 }, 0UL, new[] { 0, 1 }).SetName("DefaultTarget_AllDefaultPasses_ReturnsAll");
                yield return new TestCaseData(new ulong[] { 0, k_ContainerA, k_ContainerA }, 0UL, new[] { 0 }).SetName("DefaultTarget_IgnoresRenderTargetPasses");
                yield return new TestCaseData(new ulong[] { k_ContainerA, k_ContainerB }, 0UL, new int[0]).SetName("DefaultTarget_OnlyRenderTargetPasses_NoMatch");
                yield return new TestCaseData(new ulong[] { k_ContainerA, ulong.MaxValue }, ulong.MaxValue, new[] { 1 }).SetName("MaxTargetId_Matches");
            }
        }

        [Test]
        [TestCaseSource(nameof(s_TestCasesCollectTargetPasses))]
        public void CollectTargetPasses_SelectsExactlyTheTargetPasses(ulong[] passTargetIds, ulong target, int[] expected)
        {
            var matchedPasses = new int[passTargetIds.Length];
            int matchCount = XRSystem.CollectTargetPasses(passTargetIds, target, matchedPasses);

            CollectionAssert.AreEqual(expected, matchedPasses.AsSpan(0, matchCount).ToArray());
        }

        public static IEnumerable<TestCaseData> s_TestCasesClassifyLayout
        {
            get
            {
                yield return new TestCaseData(new int[0], true, false, XRLayoutType.Unknown).SetName("NoPasses_Unknown");
                yield return new TestCaseData(new[] { 1 }, true, false, XRLayoutType.OnePassOneView).SetName("OnePass_OneView_OnePassOneView");
                yield return new TestCaseData(new[] { 2 }, true, false, XRLayoutType.SinglePassStereo).SetName("OnePass_TwoViews_SinglePass_SinglePassStereo");
                yield return new TestCaseData(new[] { 2 }, false, false, XRLayoutType.TwoPassStereo).SetName("OnePass_TwoViews_NoSinglePass_TwoPassStereo");
                yield return new TestCaseData(new[] { 1, 1 }, false, false, XRLayoutType.TwoPassStereo).SetName("TwoPasses_OneViewEach_TwoPassStereo");
                yield return new TestCaseData(new[] { 2, 2 }, true, false, XRLayoutType.TwoPassQuadViews).SetName("TwoPasses_TwoViewsEach_SinglePass_TwoPassQuadViews");
                yield return new TestCaseData(new[] { 2, 2 }, false, false, XRLayoutType.Unknown).SetName("TwoPasses_TwoViewsEach_NoSinglePass_Unknown");
                yield return new TestCaseData(new[] { 1, 2 }, true, false, XRLayoutType.Unknown).SetName("TwoPasses_MixedViews_Unknown");
                yield return new TestCaseData(new[] { 2, 2, 2 }, true, false, XRLayoutType.Unknown).SetName("ThreePasses_Unknown");
                yield return new TestCaseData(new[] { 2 }, true, true, XRLayoutType.SinglePassStereo).SetName("RenderTarget_OnePass_TwoViews_SinglePassStereo");
                yield return new TestCaseData(new[] { 1, 1 }, false, true, XRLayoutType.TwoPassStereo).SetName("RenderTarget_TwoPasses_OneViewEach_TwoPassStereo");
                yield return new TestCaseData(new[] { 2, 2 }, true, true, XRLayoutType.Unknown).SetName("RenderTarget_TwoPasses_TwoViewsEach_QuadViewsRejected");
            }
        }

        [Test]
        [TestCaseSource(nameof(s_TestCasesClassifyLayout))]
        public void ClassifyLayout_ReturnsExpectedLayoutType(int[] viewCountsPerPass, bool canUseSinglePass, bool hasRenderTarget, XRLayoutType expected)
        {
            Assert.AreEqual(expected, XRSystem.ClassifyLayout(viewCountsPerPass, canUseSinglePass, hasRenderTarget));
        }

        internal struct MockRenderPass
        {
            public ulong renderTargetId;
            public int renderParamCount;
            public bool canUseSinglePass;

            public MockRenderPass(ulong renderTargetId, int renderParamCount, bool canUseSinglePass)
            {
                this.renderTargetId = renderTargetId;
                this.renderParamCount = renderParamCount;
                this.canUseSinglePass = canUseSinglePass;
            }
        }

        // Mirrors the matching and classification steps of XRSystem.CreateDefaultLayout
        static XRLayoutType ClassifyContainer(MockRenderPass[] passes, ulong target, out int matchCount)
        {
            var passTargetIds = new ulong[passes.Length];
            for (int i = 0; i < passes.Length; ++i)
                passTargetIds[i] = passes[i].renderTargetId;

            var matchedPasses = new int[passes.Length];
            matchCount = XRSystem.CollectTargetPasses(passTargetIds, target, matchedPasses);

            var viewCounts = new int[matchCount];
            bool canUseSinglePass = true;
            for (int i = 0; i < matchCount; ++i)
            {
                viewCounts[i] = passes[matchedPasses[i]].renderParamCount;
                canUseSinglePass &= passes[matchedPasses[i]].canUseSinglePass;
            }
            return XRSystem.ClassifyLayout(viewCounts, canUseSinglePass, target != 0);
        }

        public static IEnumerable<TestCaseData> s_TestCasesContainerLayouts
        {
            get
            {
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 2, true) },
                    XRLayoutType.SinglePassStereo, 1, XRLayoutType.SinglePassStereo, 1).SetName("BothSinglePassStereo");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 1, false), new MockRenderPass(k_ContainerA, 1, false), new MockRenderPass(k_ContainerB, 1, false), new MockRenderPass(k_ContainerB, 1, false) },
                    XRLayoutType.TwoPassStereo, 2, XRLayoutType.TwoPassStereo, 2).SetName("BothTwoPassStereo_Contiguous");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 1, false), new MockRenderPass(k_ContainerB, 1, false), new MockRenderPass(k_ContainerA, 1, false), new MockRenderPass(k_ContainerB, 1, false) },
                    XRLayoutType.TwoPassStereo, 2, XRLayoutType.TwoPassStereo, 2).SetName("BothTwoPassStereo_Interleaved");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 1, false), new MockRenderPass(k_ContainerB, 1, false) },
                    XRLayoutType.SinglePassStereo, 1, XRLayoutType.TwoPassStereo, 2).SetName("MixedSinglePassAndTwoPass");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 2, false) },
                    XRLayoutType.SinglePassStereo, 1, XRLayoutType.TwoPassStereo, 1).SetName("SecondContainerFallsBackToTwoPass");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 1, false), new MockRenderPass(k_ContainerB, 1, false) },
                    XRLayoutType.OnePassOneView, 1, XRLayoutType.OnePassOneView, 1).SetName("BothOnePassOneView");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 2, true), new MockRenderPass(k_ContainerB, 2, true) },
                    XRLayoutType.Unknown, 2, XRLayoutType.Unknown, 2).SetName("BothQuadViews_Contiguous_Rejected");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 2, true), new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 2, true) },
                    XRLayoutType.Unknown, 2, XRLayoutType.Unknown, 2).SetName("BothQuadViews_Interleaved_Rejected");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 1, false), new MockRenderPass(k_ContainerB, 1, false), new MockRenderPass(k_ContainerB, 1, false) },
                    XRLayoutType.SinglePassStereo, 1, XRLayoutType.Unknown, 3).SetName("ContainerWithThreePasses_Unknown");
            }
        }

        [Test]
        [TestCaseSource(nameof(s_TestCasesContainerLayouts))]
        public void ContainerPasses_ClassifyToExpectedLayoutPerContainer(MockRenderPass[] passes, XRLayoutType expectedLayoutA, int expectedPassCountA, XRLayoutType expectedLayoutB, int expectedPassCountB)
        {
            Assert.AreEqual(expectedLayoutA, ClassifyContainer(passes, k_ContainerA, out int passCountA));
            Assert.AreEqual(expectedPassCountA, passCountA);

            Assert.AreEqual(expectedLayoutB, ClassifyContainer(passes, k_ContainerB, out int passCountB));
            Assert.AreEqual(expectedPassCountB, passCountB);
        }

        public static IEnumerable<TestCaseData> s_TestCasesUnregisteredCamera
        {
            get
            {
                yield return new TestCaseData(
                    new[] { new MockRenderPass(0, 1, false), new MockRenderPass(0, 1, false) },
                    XRLayoutType.TwoPassStereo, 2).SetName("NoRenderTargets_TakesAllPasses");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(0, 2, true), new MockRenderPass(0, 2, true) },
                    XRLayoutType.TwoPassQuadViews, 2).SetName("NoRenderTargets_QuadViewsAllowed");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(0, 2, true), new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 2, true) },
                    XRLayoutType.SinglePassStereo, 1).SetName("MainViewAndContainers_TakesMainViewOnly");
                yield return new TestCaseData(
                    new[] { new MockRenderPass(k_ContainerA, 2, true), new MockRenderPass(k_ContainerB, 2, true) },
                    XRLayoutType.Unknown, 0).SetName("ContainersOnly_NoPasses");
            }
        }

        [Test]
        [TestCaseSource(nameof(s_TestCasesUnregisteredCamera))]
        public void UnregisteredCamera_OnlyTakesDefaultTargetPasses(MockRenderPass[] passes, XRLayoutType expectedLayout, int expectedPassCount)
        {
            // CreateDefaultLayout returns before classifying when no passes match, so the camera gets no XR passes
            Assert.AreEqual(expectedLayout, ClassifyContainer(passes, 0, out int passCount));
            Assert.AreEqual(expectedPassCount, passCount);
        }
    }
}

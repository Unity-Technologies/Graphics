using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Experimental.Tests.XR
{
    [TestFixture]
    class XRPassTests
    {
        [Test]
        public void EmptyPass_IsFirstAndLastPass()
        {
            Assert.IsTrue(XRSystem.emptyPass.isFirstCameraPass);
            Assert.IsTrue(XRSystem.emptyPass.isLastCameraPass);
        }

        [Test]
        public void EmptyPass_HasUnknownLayoutType()
        {
            Assert.AreEqual(XRLayoutType.Unknown, XRSystem.emptyPass.xrLayoutType);
        }

        [Test]
        public void EmptyPass_IsNotQuadViewInnerPass()
        {
            Assert.IsFalse(XRSystem.emptyPass.isQuadViewInnerPass);
        }

        [Test]
        public void EmptyPass_HasIdentityUvScalesAndZeroOffsets()
        {
            Assert.AreEqual(Vector4.one, XRSystem.emptyPass.uvScales);
            Assert.AreEqual(Vector4.zero, XRSystem.emptyPass.uvOffsets);
        }

        static XRPass CreatePass(XRLayoutType layoutType, int multipassId)
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

        // --- totalCameraPasses ---

        [TestCase(XRLayoutType.SinglePassStereo, ExpectedResult = 1)]
        [TestCase(XRLayoutType.TwoPassStereo, ExpectedResult = 2)]
        [TestCase(XRLayoutType.TwoPassQuadViews, ExpectedResult = 2)]
        public int TotalCameraPasses_MatchesLayoutType(XRLayoutType layoutType)
        {
            var pass = CreatePass(layoutType, multipassId: 0);
            return pass.totalCameraPasses;
        }

        // --- isFirstCameraPass ---

        [TestCase(XRLayoutType.SinglePassStereo, 0, ExpectedResult = true)]
        [TestCase(XRLayoutType.TwoPassStereo, 0, ExpectedResult = true)]
        [TestCase(XRLayoutType.TwoPassStereo, 1, ExpectedResult = false)]
        [TestCase(XRLayoutType.TwoPassQuadViews, 0, ExpectedResult = true)]
        [TestCase(XRLayoutType.TwoPassQuadViews, 1, ExpectedResult = false)]
        public bool IsFirstCameraPass_MatchesMultipassId(XRLayoutType layoutType, int multipassId)
        {
            var pass = CreatePass(layoutType, multipassId);
            return pass.isFirstCameraPass;
        }

        // --- isLastCameraPass ---

        [TestCase(XRLayoutType.SinglePassStereo, 0, ExpectedResult = true)]
        [TestCase(XRLayoutType.TwoPassStereo, 0, ExpectedResult = false)]
        [TestCase(XRLayoutType.TwoPassStereo, 1, ExpectedResult = true)]
        [TestCase(XRLayoutType.TwoPassQuadViews, 0, ExpectedResult = false)]
        [TestCase(XRLayoutType.TwoPassQuadViews, 1, ExpectedResult = true)]
        public bool IsLastCameraPass_DerivedFromTotalPasses(XRLayoutType layoutType, int multipassId)
        {
            var pass = CreatePass(layoutType, multipassId);
            return pass.isLastCameraPass;
        }

        // --- isQuadViewInnerPass ---

        [TestCase(XRLayoutType.SinglePassStereo, 0, ExpectedResult = false)]
        [TestCase(XRLayoutType.TwoPassStereo, 0, ExpectedResult = false)]
        [TestCase(XRLayoutType.TwoPassStereo, 1, ExpectedResult = false)]
        [TestCase(XRLayoutType.TwoPassQuadViews, 0, ExpectedResult = false)]
        [TestCase(XRLayoutType.TwoPassQuadViews, 1, ExpectedResult = true)]
        public bool IsQuadViewInnerPass_OnlyTrueForLastPassOfTwoPassQuadViews(XRLayoutType layoutType, int multipassId)
        {
            var pass = CreatePass(layoutType, multipassId);
            return pass.isQuadViewInnerPass;
        }

        // --- uvScales / uvOffsets ---

        [Test]
        public void UvScalesAndOffsets_PropagatedFromCreateInfo()
        {
            var expectedScales = new Vector4(0.5f, 0.6f, 0.7f, 0.8f);
            var expectedOffsets = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);
            var createInfo = new XRPassCreateInfo
            {
                xrLayoutType = XRLayoutType.TwoPassQuadViews,
                multipassId = 1,
                uvScales = expectedScales,
                uvOffsets = expectedOffsets,
            };
            var pass = new XRPass();
            pass.InitBase(createInfo);

            Assert.AreEqual(expectedScales, pass.uvScales);
            Assert.AreEqual(expectedOffsets, pass.uvOffsets);
        }
    }
}

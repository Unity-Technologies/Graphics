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

        [SetUp]
        public void Setup()
        {
            var go = new GameObject(nameof(XRLayoutTests));
            m_Camera = go.AddComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            m_LayoutTest.Clear();

            Object.DestroyImmediate(m_Camera.gameObject);

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
    }
}

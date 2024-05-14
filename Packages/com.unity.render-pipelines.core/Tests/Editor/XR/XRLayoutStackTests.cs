using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Experimental.Tests.XR
{
    [TestFixture]
    class XRLayoutStackTests
    {
        private XRLayoutStack m_StackTest = new ();

        [TearDown]
        public void TearDown()
        {
            m_StackTest.Dispose();
        }

        [Test]
        public void New_ReturnsNonNullObject()
        {
            var layout = m_StackTest.New();
            Assert.NotNull(layout);
            m_StackTest.Release();
        }

        [Test]
        public void Top_AfterNew_ReturnsCorrectObject()
        {
            var layout = m_StackTest.New();
            Assert.AreEqual(layout, m_StackTest.top);
            m_StackTest.Release();
        }

        [Test]
        public void NewNTimes_ReturnsTheTopToTheLatestElement()
        {
            var layouts = new List<XRLayout>();

            const int k_Iterations = 5;

            // Creating instances and adding them to the list
            for (int i = 0; i < k_Iterations; i++)
            {
                layouts.Add(m_StackTest.New());
            }

            // Releasing instances and validating
            for (int i = k_Iterations - 1; i >= 0; i--)
            {
                Assert.AreEqual(layouts[i], m_StackTest.top);
                m_StackTest.Release();
            }

            Top_WithoutNew_ThrowsException();
        }

        [Test]
        public void Top_WithoutNew_ThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var top  = m_StackTest.top;
            });
        }

        [Test]
        public void Release_WithoutNew_ThrowsException()
        {
            Assert.Throws<InvalidOperationException>(m_StackTest.Release);
        }

        [Test]
        public void Dispose_WithoutRelease_ThrowsException()
        {
            m_StackTest.New();
            Assert.Throws<Exception>(m_StackTest.Dispose);
            m_StackTest.Release();
        }

        [Test]
        public void CheckStackBetweenFramesReturnsTheSameXRLayout()
        {
            var stack = m_StackTest.New();
            m_StackTest.Release();

            const int k_Iterations = 5;

            // Creating instances and adding them to the list
            for (int i = 0; i < k_Iterations; i++)
            {
                m_StackTest.New();
                Assert.AreEqual(stack, m_StackTest.top);
                m_StackTest.Release();
            }
        }
    }
}

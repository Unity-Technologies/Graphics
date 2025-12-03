using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class SweepLineRectUtilsTests
    {
        const float Epsilon = 1e-5f;

        static object[][] s_Empty = new object[][]
        {
            new object[] { new List<Rect>() }
        };

        [Test]
        [TestCaseSource("s_Empty")]
        public void TestEmpty(List<Rect> rects)
        {
            var area = SweepLineRectUtils.CalculateRectUnionArea(rects);
            Assert.That(area, Is.EqualTo(0f).Within(Epsilon));
        }

        static object[][] s_SingleRect = new object[][]
        {
            new object[] { new Rect(0.1f, 0.2f, 0.3f, 0.4f), 0.12f },
            new object[] { new Rect(0f, 0f, 1f, 1f), 1f },
        };

        [Test]
        [TestCaseSource("s_SingleRect")]
        public void SingleRect(Rect r, float expected)
        {
            var area = SweepLineRectUtils.CalculateRectUnionArea(new List<Rect> { r });
            Assert.That(area, Is.EqualTo(expected).Within(Epsilon));
        }

        static object[][] s_NonOverlapping = new object[][]
        {
            new object[]
            {
                new List<Rect> { new Rect(0f, 0f, 0.5f, 0.5f), new Rect(0.5f, 0.5f, 0.5f, 0.5f) }, 0.5f
            },
            new object[]
            {
                new List<Rect>
                {
                    new Rect(0f, 0f, 0.2f, 0.2f), // 0.04
                    new Rect(0.2f, 0f, 0.3f, 0.2f), // 0.06
                    new Rect(0.5f, 0.5f, 0.25f, 0.25f) // 0.0625
                },
                0.1625f
            },
            new object[]
            {
                new List<Rect> { new Rect(0f, 0f, 0.5f, 1f), new Rect(0.5f, 0f, 0.5f, 1f) }, 1f
            },
            new object[]
            {
                new List<Rect> { new Rect(0.2f, 0.2f, 0f, 0.5f), new Rect(0.2f, 0.2f, 0.5f, 0f) }, 0f
            },
            new object[]
            {
                new List<Rect> { new Rect(0f, 0f, 0f, 0f), new Rect(0.1f, 0.1f, 0.2f, 0.3f) }, 0.06f
            },
            new object[]
            {
                new List<Rect>
                {
                    new Rect(0f, 0.3f, 0.3f, 0.3f),
                    new Rect(0.6f, 0.3f, 0.3f, 0.3f),
                    new Rect(0.3f, 0.3f, 0.3f, 0.3f),
                    new Rect(0f, 0.6f, 0.3f, 0.3f),
                    new Rect(0.7f, 0.7f, 0.3f, 0.3f),
                },
                0.45f
            }
        };

        [Test]
        [TestCaseSource("s_NonOverlapping")]
        public void NonOverlappingRects(List<Rect> rects, float expected)
        {
            var area = SweepLineRectUtils.CalculateRectUnionArea(rects);
            Assert.That(area, Is.EqualTo(expected).Within(Epsilon));
        }

        static object[][] s_Overlapping = new object[][]
        {
            new object[]
            {
                new List<Rect>
                {
                    new Rect(0.2f, 0.2f, 0.4f, 0.3f),
                    new Rect(0.2f, 0.2f, 0.4f, 0.3f)
                },
                0.4f * 0.3f
            },
            new object[]
            {
                new List<Rect>
                {
                    new Rect(0f, 0f, 0.6f, 0.5f),
                    new Rect(0.3f, 0f, 0.6f, 0.5f)
                },
                0.9f * 0.5f
            },
            new object[]
            {
                new List<Rect>
                {
                    new Rect(0f, 0.3f, 0.4f, 0.4f),
                    new Rect(0.6f, 0.3f, 0.4f, 0.4f),
                    new Rect(0.3f, 0f, 0.4f, 0.4f),
                    new Rect(0.3f, 0.6f, 0.4f, 0.4f)
                },
                0.64f - 0.04f
            }
        };

        [Test]
        [TestCaseSource("s_Overlapping")]
        public void OverlappingRects(List<Rect> rects, float expected)
        {
            var area = SweepLineRectUtils.CalculateRectUnionArea(rects);
            Assert.That(area, Is.EqualTo(expected).Within(Epsilon));
        }

        static object[][] s_ClampingOutside = new object[][]
        {
            new object[]
            {
                new List<Rect>
                {
                    new Rect(-0.2f, -0.2f, 0.3f, 0.3f), // clamps to [0,0,0.1,0.1] area 0.01
                    new Rect(0.9f, 0.9f, 0.5f, 0.5f),   // clamps to [0.9,0.9,0.1,0.1] area 0.01
                },
                0.02f
            },
            new object[] { new List<Rect> { new Rect(-10f, -10f, 20f, 20f) }, 1f }
        };

        [Test]
        [TestCaseSource("s_ClampingOutside")]
        public void ClampingOutside(List<Rect> rects, float expected)
        {
            var area = SweepLineRectUtils.CalculateRectUnionArea(rects);
            Assert.That(area, Is.EqualTo(expected).Within(Epsilon));
        }

        static object[][] s_IntervalsSorting = new object[][]
        {
            new object[]
            {
                new List<Rect>
                {
                    new Rect(0.0f, 0.2f, 0.2f, 0.2f),
                    new Rect(0.0f, 0.0f, 0.2f, 0.1f),
                    new Rect(0.0f, 0.3f, 0.2f, 0.2f),
                },
                0.2f * 0.4f
            },
            new object[]
            {
                new List<Rect>
                {
                    new Rect(0.0f, 0.0f, 0.2f, 0.1f),
                    new Rect(0.3f, 0.0f, 0.3f, 0.1f),
                    new Rect(0.5f, 0.0f, 0.2f, 0.1f),
                },
                0.6f * 0.1f
            }
        };

        [Test]
        [TestCaseSource("s_IntervalsSorting")]
        public void IntervalsSorting(List<Rect> rects, float expected)
        {
            var area = SweepLineRectUtils.CalculateRectUnionArea(rects);
            Assert.That(area, Is.EqualTo(expected).Within(Epsilon));
        }
    }
}

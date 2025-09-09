#if UNITY_EDITOR

using NUnit.Framework;
using System;
using Unity.Mathematics;
using UnityEngine.PathTracing.Lightmapping;

namespace UnityEngine.PathTracing.Tests
{
    internal class ChartIdenticationTests
    {
        [Test]
        public void TestUnionFind()
        {
            uint vertexCount = 8;
            var vertexChartIds = new UInt32[vertexCount];
            var triangleIndices = new UInt32[]
            {
                0, 1, 7,
                4, 5, 6,
                2, 6, 4,
                0, 3, 7
            };
            ChartIdentification.InitializeRepresentatives(vertexChartIds);
            ChartIdentification.UnionTriangleEdges(triangleIndices, vertexChartIds);
            ChartIdentification.FindRepresentatives(vertexChartIds);

            Assert.AreEqual(0, vertexChartIds[0]);
            Assert.AreEqual(0, vertexChartIds[1]);
            Assert.AreEqual(2, vertexChartIds[2]);
            Assert.AreEqual(0, vertexChartIds[3]);
            Assert.AreEqual(2, vertexChartIds[4]);
            Assert.AreEqual(2, vertexChartIds[5]);
            Assert.AreEqual(2, vertexChartIds[6]);
            Assert.AreEqual(0, vertexChartIds[7]);
        }

        [Test]
        public void TestCompaction()
        {
            var vertexChartIds = new UInt32[]
            {
                3, 3, 3,
                3, 2, 2,
                9, 9, 0,
                0, 3, 3
            };
            ChartIdentification.Compact(vertexChartIds, out uint chartCount);

            Assert.AreEqual(4, chartCount);

            Assert.AreEqual(0, vertexChartIds[0]);
            Assert.AreEqual(0, vertexChartIds[1]);
            Assert.AreEqual(0, vertexChartIds[2]);
            Assert.AreEqual(0, vertexChartIds[3]);
            Assert.AreEqual(1, vertexChartIds[4]);
            Assert.AreEqual(1, vertexChartIds[5]);
            Assert.AreEqual(2, vertexChartIds[6]);
            Assert.AreEqual(2, vertexChartIds[7]);
            Assert.AreEqual(3, vertexChartIds[8]);
            Assert.AreEqual(3, vertexChartIds[9]);
            Assert.AreEqual(0, vertexChartIds[10]);
            Assert.AreEqual(0, vertexChartIds[11]);
        }

        [Test]
        public void TestDeduplication()
        {
            uint vertexCount = 6;
            var vertexChartIds = new UInt32[vertexCount];
            var inputVertexUvs = new float2[]
            {
                new(-1.0f, 0.0f),
                new(0.0f, 1.0f),
                new(0.0f, -1.0f),
                new(0.0f, 1.0f),
                new(1.0f, 0.0f),
                new(0.0f, -1.0f)
            };
            var inputVertexNormals = new float3[]
            {
                new(-1.0f, 0.0f, 0.0f),
                new(0.0f, 1.0f, 0.0f),
                new(0.0f, -1.0f, 0.0f),
                new(0.0f, 1.0f, 0.0f),
                new(1.0f, 0.0f, 0.0f),
                new(0.0f, -1.0f, 0.0f)
            };
            var inputVertexPositions = new float3[]
            {
                new(-1.0f, 0.0f, 0.0f),
                new(0.0f, 1.0f, 0.0f),
                new(0.0f, -1.0f, 0.0f),
                new(0.0f, 1.0f, 0.0f),
                new(1.0f, 0.0f, 0.0f),
                new(0.0f, -2.0f, 0.0f)
            };

            ChartIdentification.InitializeRepresentatives(vertexChartIds);
            ChartIdentification.UnionDuplicateVertices(inputVertexUvs, inputVertexPositions, inputVertexNormals, vertexChartIds, true);
            ChartIdentification.FindRepresentatives(vertexChartIds);

            Assert.AreEqual(0, vertexChartIds[0]);
            Assert.AreEqual(3, vertexChartIds[1]);
            Assert.AreEqual(2, vertexChartIds[2]);
            Assert.AreEqual(3, vertexChartIds[3]);
            Assert.AreEqual(4, vertexChartIds[4]);
            Assert.AreEqual(5, vertexChartIds[5]);
        }
    }
}
#endif

using System;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.BasicModelTests.Performance
{
    class LookupPerformance : BasicModelPerformanceBase
    {
        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is approx. O(nlog2(n)).")]
        public void GetEdgesForPortPerformanceTest(int count)
        {
            /*
             * Number of nodes: 2*count
             * Number of ports: 12*count
             * Number of edges: 3*count
             * Number of lookup: 6*count
             */

            var fromNodes = new IONodeModel[count];
            var toNodes = new IONodeModel[count];

            // It is more efficient to create all nodes first, then to create the edges.
            // This avoids dirtying the port-edge index before each edge creation.
            for (var i = 0; i < count; i++)
            {
                fromNodes[i] = m_GraphAsset.GraphModel.CreateNode<IONodeModel>($"Node {i}", new Vector2(100, i * 200));
                fromNodes[i].ExeInputCount = 1;
                fromNodes[i].ExeOutputCount = 1;
                fromNodes[i].InputCount = 2;
                fromNodes[i].OutputCount = 2;
                fromNodes[i].DefineNode();

                toNodes[i] = m_GraphAsset.GraphModel.CreateNode<IONodeModel>($"Node {i}", new Vector2(100, i * 200));
                toNodes[i].ExeInputCount = 1;
                toNodes[i].ExeOutputCount = 1;
                toNodes[i].InputCount = 2;
                toNodes[i].OutputCount = 2;
                toNodes[i].DefineNode();
            }

            for (var i = 0; i < count; i++)
            {
                var inputs = toNodes[i].InputsByDisplayOrder.ToList();
                var outputs = fromNodes[i].OutputsByDisplayOrder.ToList();

                // Create edge between execution ports.
                m_GraphAsset.GraphModel.CreateEdge(inputs[0], outputs[0]);

                // Create edges from the first output data port to each input data port.
                m_GraphAsset.GraphModel.CreateEdge(inputs[1], outputs[1]);
                m_GraphAsset.GraphModel.CreateEdge(inputs[2], outputs[1]);
            }

            var allPorts = m_GraphAsset.GraphModel.NodeModels
                .OfType<IONodeModel>()
                .SelectMany(n => n.Ports)
                .ToList();

            Measure.Method(() =>
                {
                    foreach (var portModel in allPorts)
                    {
                        m_GraphAsset.GraphModel.GetEdgesForPort(portModel);
                    }
                }).WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void GetElementByGUIDPerformanceTest(int count)
        {
            var guids = new SerializableGUID[count];

            for (var i = 0; i < count; i++)
            {
                var node = m_GraphAsset.GraphModel.CreateNode<IONodeModel>($"Node {i}", new Vector2(100, i * 200));
                node.ExeInputCount = 1;
                node.ExeOutputCount = 1;
                node.InputCount = 2;
                node.OutputCount = 2;
                node.DefineNode();

                guids[i] = node.Guid;
            }

            Measure.Method(() =>
                {
                    for (int j = 0; j < 100; j++)
                    for(var i = 0; i < count; i++)
                    {
                        m_GraphAsset.GraphModel.TryGetModelFromGuid(guids[i], out var _);
                    }
                }).WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }
    }
}

using System;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Random = System.Random;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.BasicModelTests.Performance
{
    class CreateDeletePerformance : BasicModelPerformanceBase
    {
        const int k_WarmupCount = 3;
        const int k_MeasurementCount = 10;

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateNodesPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var node = m_GraphAsset.GraphModel.CreateNode<IONodeModel>($"Node {i}", new Vector2(100, i * 200));
                        node.ExeInputCount = 1;
                        node.ExeOutputCount = 1;
                        node.InputCount = 2;
                        node.OutputCount = 1;
                        node.DefineNode();
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateAndDeleteNodesPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var node = m_GraphAsset.GraphModel.CreateNode<IONodeModel>($"Node {i}", new Vector2(100, i * 200));
                        node.ExeInputCount = 1;
                        node.ExeOutputCount = 1;
                        node.InputCount = 2;
                        node.OutputCount = 1;
                        node.DefineNode();
                    }

                    m_GraphAsset.GraphModel.DeleteNodes(m_GraphAsset.GraphModel.NodeModels.ToList(), false);
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateAndDeleteEdgesPerformanceTest(int count)
        {
            var fromNodes = new IONodeModel[count];
            var toNodes = new IONodeModel[count];

            for (var i = 0; i < count; i++)
            {
                fromNodes[i] = m_GraphAsset.GraphModel.CreateNode<IONodeModel>($"Node {i}", new Vector2(100, i * 200));
                fromNodes[i].ExeInputCount = 1;
                fromNodes[i].ExeOutputCount = 1;
                fromNodes[i].InputCount = 2;
                fromNodes[i].OutputCount = 1;
                fromNodes[i].DefineNode();

                toNodes[i] = m_GraphAsset.GraphModel.CreateNode<IONodeModel>($"Node {i}", new Vector2(100, i * 200));
                toNodes[i].ExeInputCount = 1;
                toNodes[i].ExeOutputCount = 1;
                toNodes[i].InputCount = 2;
                toNodes[i].OutputCount = 1;
                toNodes[i].DefineNode();
            }

            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        m_GraphAsset.GraphModel.CreateEdge(toNodes[i].InputsByDisplayOrder.First(),
                            fromNodes[i].OutputsByDisplayOrder.First());
                    }
                    m_GraphAsset.GraphModel.DeleteEdges(m_GraphAsset.GraphModel.EdgeModels.ToList());
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateStickyNotesPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var stickyNote = m_GraphAsset.GraphModel.CreateStickyNote(new Rect(new Vector2(100, i * 400), new Vector2( 300, 300)));
                        stickyNote.Title = $"Sticky {i}";
                        stickyNote.Contents = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris";
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateAndDeleteStickyNotesPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var stickyNote = m_GraphAsset.GraphModel.CreateStickyNote(new Rect(new Vector2(100, i * 400), new Vector2( 300, 300)));
                        stickyNote.Title = $"Sticky {i}";
                        stickyNote.Contents = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris";
                    }

                    m_GraphAsset.GraphModel.DeleteStickyNotes(m_GraphAsset.GraphModel.StickyNoteModels.ToList());
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreatePlacematsPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var placemat = m_GraphAsset.GraphModel.CreatePlacemat(new Rect(new Vector2(100, i * 400), new Vector2( 300, 300)));
                        placemat.Title = $"Placemat {i}";
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateAndDeletePlacematsPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var placemat = m_GraphAsset.GraphModel.CreatePlacemat(new Rect(new Vector2(100, i * 400), new Vector2( 300, 300)));
                        placemat.Title = $"Placemat {i}";
                    }

                    m_GraphAsset.GraphModel.DeletePlacemats(m_GraphAsset.GraphModel.PlacematModels.ToList());
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(100)]
        [TestCase(400)]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateVariableDeclarationsPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        m_GraphAsset.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, $"Variable {i}", ModifierFlags.Read, false);
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(50)]
        [TestCase(75, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(100, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(200, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(1000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(2000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateVariableDeclarationsWithSameNamePerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        m_GraphAsset.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, $"Variable", ModifierFlags.Read, false);
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(10, 10)]
        [TestCase(10, 100)]
        [TestCase(100, 100, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(10, 1000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(100, 1000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(1000, 1000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(10, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(100, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(1000, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(10000, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void DuplicateVariableDeclarationsPerformanceTest(int countToDuplicate, int totalCount)
        {
            var rand = new Random(133742);
            var indicesToCopy = new byte[countToDuplicate];
            rand.NextBytes(indicesToCopy);

            for (int i = 0; i < totalCount; i++)
            {
                m_GraphAsset.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, $"Variable {i}", ModifierFlags.Read, false);
            }

            var existingVariables = m_GraphAsset.GraphModel.VariableDeclarations.ToList();

            Measure.Method(() =>
                {
                    for (var i = 0; i < countToDuplicate; i++)
                    {
                        m_GraphAsset.GraphModel.DuplicateGraphVariableDeclaration(existingVariables[i]);
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(10, 10)]
        [TestCase(10, 100)]
        [TestCase(100, 100, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(10, 1000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(100, 1000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(1000, 1000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(10, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(100, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(1000, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        [TestCase(10000, 10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void DuplicateVariableDeclarationsWithSameNamePerformanceTest(int countToDuplicate, int totalCount)
        {
            var rand = new Random(133742);
            var indicesToCopy = new byte[countToDuplicate];
            rand.NextBytes(indicesToCopy);

            for (int i = 0; i < totalCount; i++)
            {
                m_GraphAsset.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, $"Variable", ModifierFlags.Read, false);
            }

            var existingVariables = m_GraphAsset.GraphModel.VariableDeclarations.ToList();

            Measure.Method(() =>
                {
                    for (var i = 0; i < countToDuplicate; i++)
                    {
                        m_GraphAsset.GraphModel.DuplicateGraphVariableDeclaration(existingVariables[i]);
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(1000, Explicit = true, Reason = "Too slow, see GTF-634")]
        [TestCase(2000, Explicit = true, Reason = "Too slow, see GTF-634")]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateAndDeleteVariableDeclarationsPerformanceTest(int count)
        {
            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        m_GraphAsset.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, $"Variable {i}", ModifierFlags.Read, false);
                    }

                    m_GraphAsset.GraphModel.DeleteVariableDeclarations(m_GraphAsset.GraphModel.VariableDeclarations.ToList());
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(1000, Explicit = true, Reason = "Too slow, see GTF-634")]
        [TestCase(2000, Explicit = true, Reason = "Too slow, see GTF-634")]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateVariableNodesPerformanceTest(int count)
        {
            var variables = new IVariableDeclarationModel[count];
            for (var i = 0; i < count; i++)
            {
                variables[i] = m_GraphAsset.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, $"Variable {i}", ModifierFlags.Read, false);
            }

            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        m_GraphAsset.GraphModel.CreateVariableNode(variables[i], new Vector2(100, i * 200));
                    }
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(1000, Explicit = true, Reason = "Too slow, see GTF-634")]
        [TestCase(2000, Explicit = true, Reason = "Too slow, see GTF-634")]
        [TestCase(10000, Explicit = true, Reason = "Slow. Use to check that time is O(n).")]
        public void CreateAndDeleteVariableNodesPerformanceTest(int count)
        {
            var variables = new IVariableDeclarationModel[count];
            for (var i = 0; i < count; i++)
            {
                variables[i] = m_GraphAsset.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, $"Variable {i}", ModifierFlags.Read, false);
            }

            Measure.Method(() =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        m_GraphAsset.GraphModel.CreateVariableNode(variables[i], new Vector2(100, i * 200));
                    }

                    m_GraphAsset.GraphModel.DeleteNodes(m_GraphAsset.GraphModel.NodeModels.ToList(), false);
                }).WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }
    }
}

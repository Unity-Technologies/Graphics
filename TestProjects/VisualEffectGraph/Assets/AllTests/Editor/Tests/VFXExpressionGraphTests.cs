#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXExpressionGraphTests
    {
        private class ContextTest : VFXContext
        {
            public class InputProperties
            {
                public float f = 1.0f;
            }

            public ContextTest() : base(VFXContextType.Init, VFXDataType.None, VFXDataType.Particle)
            {}
        }

        private class BlockTest : VFXBlock
        {
            public class InputProperties
            {
                public float f = 2.0f;
                public Vector2 v2 = new Vector2(3.0f, 4.0f);
            }

            public override VFXContextType compatibleContexts   { get { return VFXContextType.All; } }
            public override VFXDataType compatibleData          { get { return VFXDataType.Particle; } }
        }

        private struct Graphs
        {
            public VFXGraph vfx;
            public VFXExpressionGraph exp;
        }

        private VFXGraph CreateGraph(bool complex)
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var context = ScriptableObject.CreateInstance<ContextTest>();
            var block = ScriptableObject.CreateInstance<BlockTest>();

            context.AddChild(block);
            graph.AddChild(context);

            // var
            return graph;
        }

        private void TestExpressionGraph(Action<VFXGraph> init, Action<Graphs> test, VFXExpressionContextOption option)
        {
            var vfxGraph = ScriptableObject.CreateInstance<VFXGraph>();

            var context = ScriptableObject.CreateInstance<ContextTest>();
            var block = ScriptableObject.CreateInstance<BlockTest>();

            context.AddChild(block);
            vfxGraph.AddChild(context);

            if (init != null)
                init(vfxGraph);

            var expressionGraph = new VFXExpressionGraph();
            expressionGraph.CompileExpressions(vfxGraph, option, false);

            var graphs = new Graphs();
            graphs.vfx = vfxGraph;
            graphs.exp = expressionGraph;
            test(graphs);
        }

        private void BasicTest(VFXExpressionGraph graph, int ExpectedNbSlots, int ExpectedNbExpressions, int ExpectedNbFlattened)
        {
            Assert.AreEqual(ExpectedNbSlots, graph.GPUExpressionsToReduced.Count);
            Assert.AreEqual(ExpectedNbExpressions, graph.Expressions.Count);
            Assert.AreEqual(ExpectedNbFlattened, graph.FlattenedExpressions.Count);
        }

        private void CheckExpressionValue<T>(VFXExpressionGraph graph, VFXSlot slot, T value)
        {
            var exp = slot.GetExpression();
            Assert.IsTrue(graph.GPUExpressionsToReduced.ContainsKey(exp));
            var expression = graph.GPUExpressionsToReduced[exp];
            Assert.IsTrue(expression.Is(VFXExpression.Flags.Value));
            Assert.AreEqual(value, expression.Get<T>());
        }

        private void CheckFlattenedIndex(VFXExpressionGraph graph, VFXSlot slot)
        {
            int index = graph.GetFlattenedIndex(graph.GPUExpressionsToReduced[slot.GetExpression()]);
            Assert.Greater(graph.FlattenedExpressions.Count, index);
            Assert.Less(-1, index);
        }

        [Test]
        public void SimpleTest()
        {
            TestExpressionGraph(
                null,
                (g) => BasicTest(g.exp, 3, 5, 5),
                VFXExpressionContextOption.None
            );
        }

        [Test]
        public void SimpleConstantFoldingTest()
        {
            TestExpressionGraph(
                null,
                (g) => {
                    BasicTest(g.exp, 3, 3, 3);
                    var context = (VFXContext)g.vfx[0];
                    CheckExpressionValue(g.exp, context.GetInputSlot(0), 1.0f);
                    CheckExpressionValue(g.exp, context[0].GetInputSlot(0), 2.0f);
                    CheckExpressionValue(g.exp, context[0].GetInputSlot(1), new Vector2(3.0f, 4.0f));
                },
                VFXExpressionContextOption.ConstantFolding
            );
        }

        [Test]
        public void SimpleFlattenedTest()
        {
            TestExpressionGraph(
                null,
                (g) =>
                {
                    var context = (VFXContext)g.vfx[0];
                    CheckFlattenedIndex(g.exp, context.GetInputSlot(0));
                    CheckFlattenedIndex(g.exp, context[0].GetInputSlot(0));
                    CheckFlattenedIndex(g.exp, context[0].GetInputSlot(1));
                },
                VFXExpressionContextOption.Reduction
            );
        }

        [Test]
        public void PerElementTest()
        {
            TestExpressionGraph(
                (g) =>
                {
                    var context = (VFXContext)g[0];
                    var attrib = ScriptableObject.CreateInstance<VFXAttributeParameter>();
                    attrib.SetSettingValue("attribute", "age");

                    g.AddChild(attrib);
                    attrib.GetOutputSlot(0).Link(context.GetInputSlot(0));
                },
                (g) =>
                {
                    BasicTest(g.exp, 3, 5, 4);

                    var context = (VFXContext)g.vfx[0];
                    var slot = context.GetInputSlot(0);
                    var exp = g.exp.GPUExpressionsToReduced[slot.GetExpression()];

                    Assert.IsTrue(exp.Is(VFXExpression.Flags.PerElement));
                    Assert.AreEqual(-1, g.exp.GetFlattenedIndex(exp));
                },
                VFXExpressionContextOption.Reduction
            );
        }
    }
}
#endif

using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXCloneTests
    {
        [Test]
        public void CloneOperator()
        {
            Action<VFXOperator> fnVerifyCrossIntegrety = delegate(VFXOperator op)
                {
                    Assert.IsInstanceOf<VFXOperatorCrossProduct>(op);
                    var crossProduct = op as VFXOperatorCrossProduct;

                    Assert.AreEqual(2, crossProduct.inputSlots.Count);
                    Assert.AreEqual(1, crossProduct.outputSlots.Count);
                    foreach (var slot in crossProduct.inputSlots.Concat(crossProduct.outputSlots))
                    {
                        Assert.IsInstanceOf<VFXSlotFloat3>(slot);
                        Assert.AreEqual(3, slot.GetNbChildren());
                        foreach (var child in slot.children)
                        {
                            Assert.IsInstanceOf<VFXSlotFloat>(child);
                        }
                    }
                };

            Action<VFXOperator, VFXOperator> fnVerifyIndependence = delegate(VFXOperator a, VFXOperator b)
                {
                    Assert.IsFalse(ReferenceEquals(a, b));
                    Assert.IsFalse(ReferenceEquals(a.inputSlots, b.inputSlots));
                    Assert.IsFalse(ReferenceEquals(a.children, b.children));
                    Assert.IsFalse(ReferenceEquals(a.children, b.children));

                    var slotsA = a.inputSlots.Concat(a.outputSlots).ToArray();
                    var slotsB = b.inputSlots.Concat(b.outputSlots).ToArray();
                    for (int i = 0; i < slotsA.Length; i++)
                    {
                        var slotA = slotsA[i];
                        var slotB = slotsB[i];
                        Assert.IsFalse(ReferenceEquals(slotA, slotB));
                        for (int j = 0; j < slotA.GetNbChildren(); j++)
                        {
                            var childA = slotA.children.ElementAt(j);
                            var childB = slotB.children.ElementAt(j);
                            Assert.IsFalse(ReferenceEquals(childA, childB));
                        }
                    }
                };

            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cross Product");
            var crossArray = new VFXOperator[] { null, null, null };
            crossArray[0] = crossDesc.CreateInstance();
            fnVerifyCrossIntegrety(crossArray[0]);
            for (int i = 1; i < crossArray.Length; ++i)
            {
                crossArray[i] = crossArray[i - 1].Clone<VFXOperator>();
                fnVerifyCrossIntegrety(crossArray[i]);
                fnVerifyIndependence(crossArray[i - 1], crossArray[i]);
            }
        }

        [Test]
        public void CloneLinkedOperators()
        {
            Action<VFXGraph> fnVerifyGraphIntegrity = delegate(VFXGraph g)
                {
                    Assert.AreEqual(2, g.GetNbChildren());

                    var sinRef = g.children.ElementAt(0) as VFXOperatorSine;
                    var crossRef = g.children.ElementAt(1) as VFXOperatorCrossProduct;
                    Assert.IsNotNull(sinRef);
                    Assert.IsNotNull(crossRef);

                    Assert.AreEqual(1, sinRef.outputSlots[0].LinkedSlots.Count());
                    Assert.AreEqual(1, crossRef.inputSlots[0].LinkedSlots.Count());
                    Assert.AreEqual(sinRef.outputSlots[0].LinkedSlots.ElementAt(0), crossRef.inputSlots[0]);
                    Assert.AreEqual(crossRef.inputSlots[0].LinkedSlots.ElementAt(0), sinRef.outputSlots[0]);
                };

            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sine");
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cross Product");

            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var sin = sinDesc.CreateInstance();
            var cross = crossDesc.CreateInstance();
            graph.AddChild(sin);
            graph.AddChild(cross);
            cross.inputSlots[0].Link(sin.outputSlots[0]);
            fnVerifyGraphIntegrity(graph);

            var copy = new VFXGraph[] { graph, null, null, null };
            for (int i = 1; i < copy.Length; ++i)
            {
                copy[i] = copy[i - 1].Clone<VFXGraph>();
                fnVerifyGraphIntegrity(copy[i]);
            }
        }

        [Test]
        public void CloneOperatorWithSettings()
        {
            var componentMask = ScriptableObject.CreateInstance<VFXOperatorComponentMask>();
            componentMask.x = componentMask.y = componentMask.z = componentMask.w = VFXOperatorComponentMask.Component.W;

            var copy = componentMask.Clone<VFXOperatorComponentMask>();
            Assert.AreEqual(copy.x, VFXOperatorComponentMask.Component.W);
            Assert.AreEqual(copy.y, VFXOperatorComponentMask.Component.W);
            Assert.AreEqual(copy.z, VFXOperatorComponentMask.Component.W);
            Assert.AreEqual(copy.w, VFXOperatorComponentMask.Component.W);
        }

        [Test]
        public void CloneBuiltInAttribute()
        {
            var builtIn = ScriptableObject.CreateInstance<VFXBuiltInParameter>();
            builtIn.SetSettingValue("m_expressionOp", UnityEngine.VFX.VFXExpressionOp.kVFXTotalTimeOp);
            Assert.AreEqual(UnityEngine.VFX.VFXExpressionOp.kVFXTotalTimeOp, builtIn.outputSlots[0].GetExpression().operation);
            var copy = builtIn.Clone<VFXBuiltInParameter>();
            Assert.AreEqual(UnityEngine.VFX.VFXExpressionOp.kVFXTotalTimeOp, copy.outputSlots[0].GetExpression().operation);
        }

        [Test]
        public void CloneContextWithData()
        {
            var graphA = ScriptableObject.CreateInstance<VFXGraph>();
            var contextA = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            contextA.space = UnityEngine.VFX.CoordinateSpace.Global;
            (contextA.GetData() as VFXDataParticle).capacity = 256;
            graphA.AddChild(contextA);

            var graphB = graphA.Clone<VFXGraph>();
            var contextB = graphB.children.First() as VFXContext;
            Assert.AreEqual(contextA.space, contextB.space);
            Assert.AreEqual((contextA.GetData() as VFXDataParticle).capacity, (contextB.GetData() as VFXDataParticle).capacity);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.Graphing;
using Object = UnityEngine.Object;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXOperatorNewTests
    {
        [Test]
        public void CascadedAddNewOperatorCascadedBehavior()
        {
            var one = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var add = ScriptableObject.CreateInstance<Operator.AddNew>();

            one.SetSettingValue("m_Type", (SerializableType)typeof(float));
            one.inputSlots[0].value = 1.0f;

            var count = 8.0f;
            for (int i = 0; i < (int)count; i++)
            {
                add.AddOperand();
            }

            for (int i = 0; i < (int)count; i++)
            {
                var inputSlots = add.inputSlots.ToArray();
                var emptySlot = inputSlots.First(s => !s.HasLink());
                emptySlot.Link(one.outputSlots.First());
            }

            var finalExpr = add.outputSlots.First().GetExpression();

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(finalExpr);
            var eight = result.Get<float>();
            Assert.AreEqual(count, eight);

            //< Process some remove
            var removeCount = 4.0f;
            for (int i = 0; i < (int)removeCount; ++i)
            {
                var linkedSlots = add.inputSlots.Where(o => o.HasLink());
                VFXSlot removeSlot = null;
                if (i % 2 == 0)
                {
                    removeSlot = linkedSlots.First();
                }
                else
                {
                    removeSlot = linkedSlots.Last();
                }

                //Check expected link count
                var linkCount = add.inputSlots.Where(o => o.HasLink()).Count();
                Assert.AreEqual((int)count - i, linkCount);

                var index = add.inputSlots.IndexOf(removeSlot);
                add.RemoveOperand(index);

                //Check expected link count
                linkCount = add.inputSlots.Where(o => o.HasLink()).Count();
                Assert.AreEqual((int)count - i - 1, linkCount);

                //Check if all input slot are still with a default value
                foreach (var slot in add.inputSlots.Where(o => o.HasLink()))
                {
                    Assert.AreEqual(0.0f, slot.value);
                }

                //Check computed result
                finalExpr = add.outputSlots.First().GetExpression();
                result = context.Compile(finalExpr);
                var res = result.Get<float>();
                Assert.AreEqual(count - (float)i - 1.0f, res);
            }
        }

        [Test]
        public void CascadedMulNewOperatorDifferentFloatSizes()
        {
            var vec2_Two = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var vec3_Two = ScriptableObject.CreateInstance<VFXInlineOperator>();

            vec2_Two.SetSettingValue("m_Type", (SerializableType)typeof(Vector2));
            vec2_Two.inputSlots[0].value = Vector2.one * 2.0f;

            vec3_Two.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
            vec3_Two.inputSlots[0].value = Vector3.one * 2.0f;

            var mul = ScriptableObject.CreateInstance<Operator.MultiplyNew>();
            mul.SetOperandType(0, typeof(Vector2));
            mul.inputSlots[0].Link(vec2_Two.outputSlots[0]);
            mul.SetOperandType(1, typeof(Vector3));
            mul.inputSlots[1].Link(vec3_Two.outputSlots[0]);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(mul.outputSlots[0].GetExpression());
            var final = result.Get<Vector3>();

            Assert.AreEqual(new Vector3(4, 4, 2), final);
        }

        [Test]
        public void CascadedSubstractIntegerBehavior()
        {
            var subtract = ScriptableObject.CreateInstance<Operator.SubtractNew>();
            subtract.SetOperandType(0, typeof(float));
            subtract.SetOperandType(1, typeof(float));

            Assert.AreEqual(VFXValueType.Float, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(float));
            subtract.SetOperandType(1, typeof(int));
            Assert.AreEqual(VFXValueType.Float, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(int));
            subtract.SetOperandType(1, typeof(int));
            Assert.AreEqual(VFXValueType.Int32, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(int));
            subtract.SetOperandType(1, typeof(uint));
            Assert.AreEqual(VFXValueType.Uint32, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(uint));
            subtract.SetOperandType(1, typeof(uint));
            Assert.AreEqual(VFXValueType.Uint32, subtract.outputSlots[0].GetExpression().valueType);

            //Finally, do some simple math integer (TODOPAUL : are incorrect mathematically but another PR is coming for this support)
            subtract.SetOperandType(0, typeof(int));
            subtract.SetOperandType(1, typeof(uint));

            var a = 9;
            var b = 5;
            var res = a - b; //remark : here, in C#, if b is an uint, result is a long not an uint

            subtract.inputSlots[0].value = (int)a;
            subtract.inputSlots[1].value = (uint)b;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(subtract.outputSlots[0].GetExpression());
            var final = result.Get<uint>();
            Assert.AreEqual(res, final);
        }

        [Test]
        public void ClampNewBehavior()
        {
            var clamp = ScriptableObject.CreateInstance<Operator.ClampNew>();
            clamp.SetOperandType(typeof(int));
            Assert.AreEqual(VFXValueType.Int32, clamp.outputSlots[0].GetExpression().valueType);

            clamp.inputSlots[0].value = -6;
            clamp.inputSlots[1].value = -3;
            clamp.inputSlots[2].value = 4;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(clamp.outputSlots[0].GetExpression());
            var final = result.Get<int>();
            Assert.AreEqual(-3, final);
        }

        [Test]
        public void LengthNewBehavior()
        {
            var length = ScriptableObject.CreateInstance<Operator.LengthNew>();
            length.SetOperandType(typeof(Vector2));
            Assert.AreEqual(VFXValueType.Float, length.outputSlots[0].GetExpression().valueType);

            var vec2 = Vector2.one * 3;

            length.inputSlots[0].value = vec2;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(length.outputSlots[0].GetExpression());
            var final = result.Get<float>();
            Assert.AreEqual(vec2.magnitude, final);
        }

        [Test]
        public void DotProductNewBehavior()
        {
            var dot = ScriptableObject.CreateInstance<Operator.DotProductNew>();
            dot.SetOperandType(0, typeof(Vector2));
            dot.SetOperandType(1, typeof(Vector3));

            Assert.AreEqual(VFXValueType.Float, dot.outputSlots[0].GetExpression().valueType);

            var a = new Vector2(6, 7);
            var b = new Vector3(2, 3, 4);

            dot.inputSlots[0].value = a;
            dot.inputSlots[1].value = b;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(dot.outputSlots[0].GetExpression());
            var final = result.Get<float>();

            Assert.AreEqual(Vector3.Dot(b, new Vector3(a.x, a.y, 0)), final);
        }

        [Test]
        public void CosineNewBehavior()
        {
            var cos = ScriptableObject.CreateInstance<Operator.CosineNew>();
            cos.SetOperandType(typeof(Vector2));

            Assert.AreEqual(VFXValueType.Float2, cos.outputSlots[0].GetExpression().valueType);
            var a = new Vector2(12, 89);

            cos.inputSlots[0].value = a;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(cos.outputSlots[0].GetExpression());
            var final = result.Get<Vector2>();

            Assert.AreEqual(Mathf.Cos(a.x), final.x);
            Assert.AreEqual(Mathf.Cos(a.y), final.y);
        }

        [Test]
        public void KeepConnectionNewBehavior()
        {
            var cos_A = ScriptableObject.CreateInstance<Operator.CosineNew>();
            cos_A.SetOperandType(typeof(Vector3));
            var cos_B = ScriptableObject.CreateInstance<Operator.CosineNew>();
            cos_B.SetOperandType(typeof(float));

            Assert.AreEqual(VFXValueType.Float3, cos_A.outputSlots[0].GetExpression().valueType);
            Assert.AreEqual(VFXValueType.Float, cos_B.outputSlots[0].GetExpression().valueType);

            cos_A.inputSlots[0].Link(cos_B.outputSlots[0]);

            float r = 5;
            cos_B.inputSlots[0].value = r;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(cos_A.outputSlots[0].GetExpression());
            var finalv3 = result.Get<Vector3>();

            var expected = Mathf.Cos(Mathf.Cos(r));
            Assert.AreEqual(expected, finalv3.x);
            Assert.AreEqual(expected, finalv3.y);
            Assert.AreEqual(expected, finalv3.z);

            cos_A.SetOperandType(typeof(Vector2));
            Assert.AreEqual(VFXValueType.Float2, cos_A.outputSlots[0].GetExpression().valueType);

            result = context.Compile(cos_A.outputSlots[0].GetExpression());
            var finalv2 = result.Get<Vector2>();
            Assert.AreEqual(expected, finalv2.x);
            Assert.AreEqual(expected, finalv2.y);
        }

        [Test]
        public void AppendNewBehavior()
        {
            var append = ScriptableObject.CreateInstance<Operator.AppendVectorNew>();
            Assert.AreEqual(VFXValueType.Float2, append.outputSlots[0].GetExpression().valueType);
            append.AddOperand();
            Assert.AreEqual(VFXValueType.Float3, append.outputSlots[0].GetExpression().valueType);
            append.AddOperand();
            Assert.AreEqual(VFXValueType.Float4, append.outputSlots[0].GetExpression().valueType);

            append.RemoveOperand();
            append.RemoveOperand();

            Assert.AreEqual(VFXValueType.Float2, append.outputSlots[0].GetExpression().valueType);
            append.SetOperandType(0, typeof(Vector2));
            Assert.AreEqual(VFXValueType.Float3, append.outputSlots[0].GetExpression().valueType);
            append.SetOperandType(1, typeof(Vector2));
            Assert.AreEqual(VFXValueType.Float4, append.outputSlots[0].GetExpression().valueType);
        }

        [Test]
        public void BranchNewBehavior()
        {
            var branch = ScriptableObject.CreateInstance<Operator.BranchNew>();
            branch.SetOperandType(typeof(Sphere));

            var sphereA = new Sphere() { center = new Vector3(1.0f, 2.0f, 3.0f), radius = 4.0f };
            var sphereB = new Sphere() { center = new Vector3(1.0f, 2.0f, 3.0f), radius = 4.0f };

            branch.inputSlots[0].value = false;
            branch.inputSlots[1].value = sphereA;
            branch.inputSlots[2].value = sphereB;

            Func<Sphere, Sphere, bool> fnCompareSphere = delegate(Sphere aS, Sphere bS)
                {
                    if (aS.center.x != bS.center.x) return false;
                    if (aS.center.y != bS.center.y) return false;
                    if (aS.center.z != bS.center.z) return false;
                    if (aS.radius != bS.radius) return false;
                    return true;
                };

            Func<VFXSlot, Sphere> fnSlotToSphere = delegate(VFXSlot slot)
                {
                    var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                    var center = context.Compile(slot[0].GetExpression());
                    var radius = context.Compile(slot[1].GetExpression());
                    return new Sphere()
                    {
                        center = center.Get<Vector3>(),
                        radius = radius.Get<float>()
                    };
                };

            Assert.IsTrue(fnCompareSphere(fnSlotToSphere(branch.outputSlots[0]), sphereB));

            branch.inputSlots[0].value = true;
            Assert.IsTrue(fnCompareSphere(fnSlotToSphere(branch.outputSlots[0]), sphereA));
        }

        [Test]
        public void VerifyTypeCompatibility()
        {
            var typeAffinity = VFXOperatorDynamicOperand.kTypeAffinity;
            foreach (var type in typeAffinity)
            {
                var slotBase = VFXSlot.Create(new VFXProperty(type.Key, "o"), VFXSlot.Direction.kOutput);
                foreach (var affinity in type.Value)
                {
                    var slotAffinity = VFXSlot.Create(new VFXProperty(affinity, "i"), VFXSlot.Direction.kInput);
                    if (!slotBase.CanLink(slotAffinity))
                    {
                        Assert.Fail(string.Format("VFXSlot of {1} CanConvertFrom({0}) excepts return true", slotBase.property.type.UserFriendlyName(), slotAffinity.property.type.UserFriendlyName()));
                    }
                }
            }
        }
    }
}

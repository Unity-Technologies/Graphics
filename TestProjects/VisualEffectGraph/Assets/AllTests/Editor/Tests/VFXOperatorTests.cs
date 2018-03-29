using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXOperatorTests
    {
        [Test]
        public void CascadedAddOperator()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();
            var add = ScriptableObject.CreateInstance<Operator.Add>();

            var count = 8.0f;
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
        }

        [Test]
        public void CascadedMulOperator()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();
            var two = ScriptableObject.CreateInstance<Operator.Add>();

            two.inputSlots[0].Link(one.outputSlots[0]);
            two.inputSlots[1].Link(one.outputSlots[0]);

            var vec2_Two = ScriptableObject.CreateInstance<Operator.AppendVector>();
            vec2_Two.inputSlots[0].Link(two.outputSlots[0]);
            vec2_Two.inputSlots[1].Link(two.outputSlots[0]);

            var vec3_Two = ScriptableObject.CreateInstance<Operator.AppendVector>();
            vec3_Two.inputSlots[0].Link(two.outputSlots[0]);
            vec3_Two.inputSlots[1].Link(two.outputSlots[0]);
            vec3_Two.inputSlots[2].Link(two.outputSlots[0]);

            var mul = ScriptableObject.CreateInstance<Operator.Multiply>();
            mul.inputSlots[0].Link(vec2_Two.outputSlots[0]);
            mul.inputSlots[1].Link(vec3_Two.outputSlots[0]);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(mul.outputSlots[0].GetExpression());
            var final = result.Get<Vector3>();

            Assert.AreEqual(final, new Vector3(4, 4, 2));
        }

        [Test]
        public void ChangeTypeInCascade()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();

            var vec2_One = ScriptableObject.CreateInstance<Operator.AppendVector>();
            vec2_One.inputSlots[0].Link(one.outputSlots[0]);
            vec2_One.inputSlots[1].Link(one.outputSlots[0]);
            Assert.AreEqual(vec2_One.outputSlots[0].GetExpression().valueType, VFXValueType.Float2);

            var vec3_One = ScriptableObject.CreateInstance<Operator.AppendVector>();
            vec3_One.inputSlots[0].Link(vec2_One.outputSlots[0]);
            vec3_One.inputSlots[1].Link(one.outputSlots[0]);
            Assert.AreEqual(vec3_One.outputSlots[0].GetExpression().valueType, VFXValueType.Float3);

            var cos = ScriptableObject.CreateInstance<Operator.Cosine>();
            cos.inputSlots[0].Link(vec2_One.outputSlots[0]);
            Assert.AreEqual(cos.outputSlots[0].GetExpression().valueType, VFXValueType.Float2);

            var sin = ScriptableObject.CreateInstance<Operator.Sine>();
            sin.inputSlots[0].Link(cos.outputSlots[0]);
            Assert.AreEqual(sin.outputSlots[0].GetExpression().valueType, VFXValueType.Float2);

            var abs = ScriptableObject.CreateInstance<Operator.Absolute>();
            abs.inputSlots[0].Link(sin.outputSlots[0]);
            Assert.AreEqual(abs.outputSlots[0].GetExpression().valueType, VFXValueType.Float2);

            //Cascaded invalidation should occurs
            cos.inputSlots[0].Link(vec3_One.outputSlots[0]);
            Assert.AreEqual(abs.outputSlots[0].GetExpression().valueType, VFXValueType.Float3);
        }

        [Test]
        public void AutoDisconnectInvalid()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();

            var append = ScriptableObject.CreateInstance<Operator.AppendVector>();
            append.inputSlots[0].Link(one.outputSlots[0]);
            append.inputSlots[1].Link(one.outputSlots[0]);
            append.inputSlots[2].Link(one.outputSlots[0]);

            var cross = ScriptableObject.CreateInstance<Operator.CrossProduct>();
            cross.inputSlots[0].Link(append.outputSlots[0]);
            Assert.IsTrue(cross.inputSlots[0].HasLink());

            append.inputSlots[2].UnlinkAll();
            Assert.IsFalse(cross.inputSlots[0].HasLink());
        }

        [Test]
        public void Append()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();
            var append = ScriptableObject.CreateInstance<Operator.AppendVector>();
            append.inputSlots[0].Link(one.outputSlots[0]);
            append.inputSlots[1].Link(one.outputSlots[0]);
            append.inputSlots[2].Link(one.outputSlots[0]);
            append.inputSlots[3].Link(one.outputSlots[0]);

            var expression = append.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.Float4, expression.valueType);
        }

        [Test]
        public void NoErrorWhenExtractTRSFromMatrix4x4()
        {
            var matrixOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
            matrixOperator.SetSettingValue("m_Type", (SerializableType)typeof(Matrix4x4));

            var matrix = Matrix4x4.identity;
            matrix[3, 3] = 0.0f; // voluntary incorrect value in TRS matrix
            matrixOperator.inputSlots[0].value = matrix;

            var transformOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
            transformOperator.SetSettingValue("m_Type", (SerializableType)typeof(Transform));
            matrixOperator.outputSlots[0].Link(transformOperator.inputSlots[0]);

            var expressionSlot = transformOperator.outputSlots[0][0].GetExpressionSlots(); //position
            expressionSlot = expressionSlot.Concat(transformOperator.outputSlots[0][1].GetExpressionSlots()); //angle
            expressionSlot = expressionSlot.Concat(transformOperator.outputSlots[0][2].GetExpressionSlots()); //scale
            var slotArray = expressionSlot.ToArray();

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var expected = new[] { Vector3.zero, Vector3.zero, Vector3.one };
            for (int i = 0; i < 3; ++i)
            {
                var value = context.Compile(slotArray[i].GetExpression()).Get<Vector3>();
                Assert.IsTrue(expected[i].Equals(value));
            }
            Assert.IsTrue(transformOperator.inputSlots[0].HasLink());
        }

        [Test]
        public void ComponentMaskAndAppend()
        {
            /*var componentMask = ScriptableObject.CreateInstance<VFXOperatorComponentMask>();
            componentMask.settings = new VFXOperatorComponentMask.Settings() { mask = "xy" };
            var expression = componentMask.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.Float2, expression.ValueType);

            var append = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            append.inputSlots[0].Link(componentMask.outputSlots[0]);
            append.inputSlots[1].Link(componentMask.outputSlots[0]);
            expression = append.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.Float4, expression.ValueType);

            componentMask.settings = new VFXOperatorComponentMask.Settings() { mask = "x" };
            expression = append.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.Float2, expression.ValueType);*/
        }

        [Test]
        public void AppendOperator()
        {
            var absOperator = ScriptableObject.CreateInstance<Operator.Absolute>();
            var appendOperator = ScriptableObject.CreateInstance<Operator.AppendVector>();
            var cosOperator = ScriptableObject.CreateInstance<Operator.Cosine>();

            Assert.AreEqual(VFXValueType.Float, cosOperator.outputSlots[0].GetExpression().valueType);
            Assert.AreEqual(0, appendOperator.outputSlots.Count);
            Assert.AreEqual(1, appendOperator.inputSlots.Count);

            appendOperator.inputSlots[0].Link(absOperator.outputSlots[0]);
            Assert.AreEqual(1, appendOperator.outputSlots.Count);
            Assert.AreEqual(VFXValueType.Float, appendOperator.outputSlots[0].GetExpression().valueType);

            cosOperator.inputSlots[0].Link(appendOperator.outputSlots[0]);
            Assert.AreEqual(2, appendOperator.inputSlots.Count);
            Assert.AreEqual(VFXValueType.Float, cosOperator.outputSlots[0].GetExpression().valueType);

            appendOperator.inputSlots[1].Link(absOperator.outputSlots[0]);
            Assert.AreEqual(3, appendOperator.inputSlots.Count);
            Assert.AreEqual(VFXValueType.Float2, cosOperator.outputSlots[0].GetExpression().valueType);

            appendOperator.inputSlots[2].Link(absOperator.outputSlots[0]);
            Assert.AreEqual(4, appendOperator.inputSlots.Count);
            Assert.AreEqual(VFXValueType.Float3, cosOperator.outputSlots[0].GetExpression().valueType);

            appendOperator.inputSlots[3].Link(absOperator.outputSlots[0]);
            Assert.AreEqual(4, appendOperator.inputSlots.Count);
            Assert.AreEqual(VFXValueType.Float4, cosOperator.outputSlots[0].GetExpression().valueType);
        }

        [Test]
        public void AttributeEquality()
        {
            foreach (var attribute in VFXAttribute.AllExpectLocalOnly)
            {
                var desc = VFXLibrary.GetOperators().First(p => p.name.Contains(attribute) && p.modelType == typeof(VFXAttributeParameter));
                var a = desc.CreateInstance();
                var b = desc.CreateInstance();
                Assert.IsNotNull(a);
                Assert.IsNotNull(b);
                Assert.AreNotEqual(a, b);

                var referenceAttribute = VFXAttribute.Find(attribute);
                var reference = new VFXAttributeExpression(referenceAttribute);
                Assert.AreEqual(reference, a.outputSlots[0].GetExpression());
                Assert.AreEqual(reference, b.outputSlots[0].GetExpression());
            }
        }

        [Test]
        public void ReferenceOfBuiltInEquality()
        {
            foreach (var operation in VFXBuiltInExpression.All)
            {
                var desc = VFXLibrary.GetOperators().First(p => p.name == operation.ToString());
                var a = desc.CreateInstance();
                var b = desc.CreateInstance();
                Assert.IsNotNull(a);
                Assert.IsNotNull(b);
                Assert.AreNotEqual(a, b);

                var reference = VFXBuiltInExpression.Find(operation);
                Assert.IsTrue(ReferenceEquals(reference, a.outputSlots[0].GetExpression()));
                Assert.IsTrue(ReferenceEquals(reference, b.outputSlots[0].GetExpression()));
            }
        }

        [Test]
        public void SwizzleOperator()
        {
            // check basic swizzle
            {
                var inputVec = ScriptableObject.CreateInstance<VFXOperatorVector2>();
                var swizzle = ScriptableObject.CreateInstance<Operator.Swizzle>();
                swizzle.inputSlots[0].Link(inputVec.outputSlots.First());
                swizzle.SetSettingValue("mask", "xxy");

                var finalExpr = swizzle.outputSlots.First().GetExpression();

                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var result = context.Compile(finalExpr);
                var vec = result.Get<Vector3>();

                Assert.AreEqual(new Vector3(1.0f, 1.0f, 2.0f), vec);
            }

            // check out of bounds mask is clamped correctly
            {
                var inputVec = ScriptableObject.CreateInstance<VFXOperatorVector2>();
                var swizzle = ScriptableObject.CreateInstance<Operator.Swizzle>();
                swizzle.inputSlots[0].Link(inputVec.outputSlots.First());
                swizzle.SetSettingValue("mask", "yzx");

                var finalExpr = swizzle.outputSlots.First().GetExpression();

                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var result = context.Compile(finalExpr);
                var vec = result.Get<Vector3>();

                Assert.AreEqual(new Vector3(2.0f, 2.0f, 1.0f), vec);
            }
        }
    }
}

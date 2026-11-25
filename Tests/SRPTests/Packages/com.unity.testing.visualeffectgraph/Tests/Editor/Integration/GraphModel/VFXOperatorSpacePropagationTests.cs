using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Operator;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.GraphModel
{
    [TestFixture]
    internal class VFXOperatorSpacePropagationTests
    {
        [Test]
        public void SpaceUniformisation_Between_World_And_Local()
        {
            var add = ScriptableObject.CreateInstance<Add>();
            add.SetOperandType(0, typeof(Position));
            add.SetOperandType(1, typeof(Position));

            Assert.AreEqual(add.outputSlots[0].property.type, typeof(Position));
            Assert.AreEqual(add.outputSlots[0].space, VFXSpace.Local);

            add.inputSlots[0].space = VFXSpace.World;
            Assert.AreEqual(add.inputSlots[0].space, VFXSpace.World);
            Assert.AreEqual(add.inputSlots[1].space, VFXSpace.Local);
            Assert.AreEqual(add.outputSlots[0].space, VFXSpace.World);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(add.outputSlots[0].GetExpression());

            var allExpr = VFXTestCommon.CollectParentExpression(result).ToArray();
            Assert.IsTrue(allExpr.Count(o =>
            {
                return o.operation == VFXExpressionOperation.LocalToWorld;
            }) == 1);
        }

        public class SpaceConversionCase
        {
            public override string ToString()
            {
                return inputs.Select(o => o.ToString()).Aggregate((a, b) => string.Format("{0}, {1}", a, b));
            }

            internal VFXSpace[] inputs;
            internal VFXSpace output;
            internal bool expectLocalToWorld;
            internal bool expectWorldToLocal;
        }

        static private SpaceConversionCase[] SpaceConversions = new SpaceConversionCase[]
        {
            new SpaceConversionCase()
            {
                inputs = new VFXSpace[] { VFXSpace.Local, VFXSpace.Local },
                output = VFXSpace.Local,
                expectLocalToWorld = false,
                expectWorldToLocal = false
            },
            new SpaceConversionCase()
            {
                inputs = new VFXSpace[] { VFXSpace.Local, VFXSpace.World },
                output = VFXSpace.World,
                expectLocalToWorld = true,
                expectWorldToLocal = false
            },
            new SpaceConversionCase()
            {
                inputs = new VFXSpace[] { VFXSpace.World, VFXSpace.Local },
                output = VFXSpace.World,
                expectLocalToWorld = true,
                expectWorldToLocal = false
            },
            new SpaceConversionCase()
            {
                inputs = new VFXSpace[] { VFXSpace.Local, VFXSpace.Local, VFXSpace.None },
                output = VFXSpace.Local,
                expectLocalToWorld = false,
                expectWorldToLocal = false
            },
            new SpaceConversionCase()
            {
                inputs = new VFXSpace[] { VFXSpace.Local, VFXSpace.World, VFXSpace.None },
                output = VFXSpace.World,
                expectLocalToWorld = true,
                expectWorldToLocal = false
            },
            new SpaceConversionCase()
            {
                inputs = new VFXSpace[] { VFXSpace.World, VFXSpace.Local, VFXSpace.None },
                output = VFXSpace.World,
                expectLocalToWorld = true,
                expectWorldToLocal = false
            },
            new SpaceConversionCase()
            {
                inputs = new VFXSpace[] { VFXSpace.None, VFXSpace.None, VFXSpace.None },
                output = VFXSpace.None,
                expectLocalToWorld = false,
                expectWorldToLocal = false
            }
        };

        [Test]
        public void Space_Between([ValueSource("SpaceConversions")] SpaceConversionCase spaceConversion)
        {
            var add = ScriptableObject.CreateInstance<Add>();

            add.SetOperandType(0, typeof(Position));
            add.SetOperandType(1, typeof(Position));
            while (add.inputSlots.Count < spaceConversion.inputs.Length)
            {
                add.AddOperand(typeof(Position));
            }

            for (int i = 0; i < spaceConversion.inputs.Length; ++i)
            {
                Assert.AreEqual(add.inputSlots[i].property.type, typeof(Position));
                Assert.AreEqual(add.inputSlots[i].space, VFXSpace.Local);

                add.inputSlots[i].space = spaceConversion.inputs[i];

                Assert.AreEqual(add.inputSlots[i].space, spaceConversion.inputs[i]);
            }

            Assert.AreEqual(add.outputSlots[0].space, spaceConversion.output);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(add.outputSlots[0].GetExpression());

            var allExpr = VFXTestCommon.CollectParentExpression(result).ToArray();

            Assert.AreEqual(spaceConversion.expectLocalToWorld, allExpr.Any(o =>
            {
                return o.operation == VFXExpressionOperation.LocalToWorld;
            }));

            Assert.AreEqual(spaceConversion.expectWorldToLocal, allExpr.Any(o =>
            {
                return o.operation == VFXExpressionOperation.WorldToLocal;
            }));
        }

#pragma warning disable 0414
        private static Type[] SpaceTransmissionType = { typeof(Position), typeof(Sphere) };

        #pragma warning restore 0414
        [Test]
        public void SpaceTransmission_From_An_Operator_To_Another([ValueSource("SpaceTransmissionType")] Type type)
        {
            var position_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var position_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            position_A.SetSettingValue("m_Type", (SerializableType)type);
            position_B.SetSettingValue("m_Type", (SerializableType)type);

            position_A.inputSlots[0].space = VFXSpace.World;
            Assert.AreEqual(VFXSpace.World, position_A.outputSlots[0].space);

            position_B.inputSlots[0].space = VFXSpace.Local;
            Assert.AreEqual(VFXSpace.Local, position_B.outputSlots[0].space);

            position_B.inputSlots[0].Link(position_A.outputSlots[0]);
            Assert.AreEqual(VFXSpace.World, position_B.outputSlots[0].space);

            position_A.inputSlots[0].space = VFXSpace.Local;
            Assert.AreEqual(VFXSpace.Local, position_B.outputSlots[0].space);
        }

        [Test]
        public void SpaceConversion_Verify_Expected_Invalidation_Of_Space()
        {
            var inlineVector = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineVector.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            var transformSpace = ScriptableObject.CreateInstance<ChangeSpace>();
            transformSpace.SetOperandType(typeof(Position));
            transformSpace.outputSlots[0].Link(inlineVector.inputSlots[0]);

            //Local => Local
            var slotVectorExpression = VFXTestCommon.CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.AreEqual(VFXSpace.Local, transformSpace.inputSlots[0].space);
            Assert.IsFalse(slotVectorExpression.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //World => Local
            transformSpace.inputSlots[0].space = VFXSpace.World;
            slotVectorExpression = VFXTestCommon.CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 0);
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);

            //World => World
            transformSpace.SetSettingValue("m_targetSpace", VFXSpace.World);
            slotVectorExpression = VFXTestCommon.CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsFalse(slotVectorExpression.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //Local => World
            transformSpace.inputSlots[0].space = VFXSpace.Local;
            slotVectorExpression = VFXTestCommon.CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 0);
        }

        [Test]
        public void SpaceConversion_Verify_Direction_Use_TransformDir()
        {
            var transformSpace = ScriptableObject.CreateInstance<ChangeSpace>();
            transformSpace.SetOperandType(typeof(DirectionType));
            transformSpace.SetSettingValue("m_targetSpace", VFXSpace.World);
            var expressions = VFXTestCommon.CollectParentExpression(transformSpace.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(expressions.Count(o => o is VFXExpressionTransformDirection) == 1);
        }

        [Test]
        public void SpaceConversion_Verify_Vector_Use_TransformVec()
        {
            var transformSpace = ScriptableObject.CreateInstance<ChangeSpace>();
            transformSpace.SetOperandType(typeof(Vector));
            transformSpace.SetSettingValue("m_targetSpace", VFXSpace.World);
            var expressions = VFXTestCommon.CollectParentExpression(transformSpace.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(expressions.Count(o => o is VFXExpressionTransformVector) == 1);
        }

        [Test]
        public void SpaceConversion_Propagation_Of_Space_With_Different_Type()
        {
            var position = ScriptableObject.CreateInstance<VFXInlineOperator>();
            position.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            var vector = ScriptableObject.CreateInstance<VFXInlineOperator>();
            vector.SetSettingValue("m_Type", (SerializableType)typeof(Vector));

            var direction = ScriptableObject.CreateInstance<VFXInlineOperator>();
            direction.SetSettingValue("m_Type", (SerializableType)typeof(DirectionType));

            position.outputSlots[0].Link(vector.inputSlots[0]);
            vector.outputSlots[0].Link(direction.inputSlots[0]);

            Assert.AreEqual(VFXSpace.Local, direction.outputSlots[0].space);
            position.inputSlots[0].space = VFXSpace.World;
            Assert.AreEqual(VFXSpace.World, direction.outputSlots[0].space);
        }

        [Test]
        public void Space_Main_Camera()
        {
            var mainCamera = ScriptableObject.CreateInstance<MainCamera>();
            mainCamera.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            Assert.AreEqual(VFXSpace.World, mainCamera.outputSlots[0].space);
            foreach (var slot in mainCamera.outputSlots[0].GetExpressionSlots())
            {
                var expressions = VFXTestCommon.CollectParentExpression(slot.GetExpression()).ToArray();
                Assert.IsFalse(expressions.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            }
        }

        [Test]
        public void SpaceConversion_With_SwitchOperator()
        {
            var switchOp = ScriptableObject.CreateInstance<Operator.Switch>();
            switchOp.SetSettingValue("m_Type", (SerializableType)typeof(Vector));
            Assert.AreEqual(4, switchOp.inputSlots.Count);

            float trackValue = 1.234f;

            switchOp.inputSlots[1].space = VFXSpace.Local;
            switchOp.inputSlots[1].value = new Vector() {vector = new Vector3(trackValue, trackValue, trackValue) };
            switchOp.inputSlots[2].space = VFXSpace.World;
            switchOp.inputSlots[2].value = new Vector() {vector = new Vector3(2.456f, 2.456f, 2.456f)};
            switchOp.inputSlots[3].space = VFXSpace.None;
            switchOp.inputSlots[3].value = new Vector() {vector = new Vector3(3.789f, 3.789f, 3.789f)};

            Assert.AreEqual(VFXSpace.World, switchOp.outputSlots[0].space);

            var expressions = VFXTestCommon.CollectParentExpression(switchOp.outputSlots[0].GetExpression()).ToArray();
            Assert.AreEqual(1, expressions.Count(o => o.operation == VFXExpressionOperation.LocalToWorld));
            Assert.AreEqual(0, expressions.Count(o => o.operation == VFXExpressionOperation.WorldToLocal));

            var conversion = expressions.First(o => o.parents.Any(p => p.operation == VFXExpressionOperation.LocalToWorld));
            Assert.AreEqual(VFXExpressionOperation.TransformVec, conversion.operation);

            var parentFromFloat3 = conversion.parents[1];
            var expressionFromFloat3 = VFXTestCommon.CollectParentExpression(parentFromFloat3).ToArray();

            var float3Component = expressionFromFloat3.OfType<VFXValue<float>>().ToArray();
            Assert.AreEqual(3, float3Component.Length);
            Assert.AreEqual(trackValue, float3Component[0].Get<float>());
            Assert.AreEqual(trackValue, float3Component[1].Get<float>());
            Assert.AreEqual(trackValue, float3Component[2].Get<float>());
        }
    }
}

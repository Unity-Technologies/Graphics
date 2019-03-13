#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.VFX.Operator;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    internal class VFXSpacePropagationTest
    {
        public static IEnumerable<VFXExpression> CollectParentExpression(VFXExpression expression, HashSet<VFXExpression> hashSet = null)
        {
            if (expression != null)
            {
                if (hashSet == null)
                {
                    hashSet = new HashSet<VFXExpression>();
                }

                if (!hashSet.Contains(expression))
                {
                    hashSet.Add(expression);
                    yield return expression;
                    foreach (var parent in expression.parents)
                    {
                        var parents = CollectParentExpression(parent, hashSet);
                        foreach (var exp in parents)
                        {
                            yield return exp;
                        }
                    }
                }
            }
        }

        [Test]
        public void Sphere_Type_Should_Be_Spaceable()
        {
            var inline = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inline.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            Assert.IsTrue(inline.inputSlots[0].spaceable);
        }

        [Test]
        public void SpaceUniformisation_Between_World_And_Local()
        {
            var add = ScriptableObject.CreateInstance<Add>();
            add.SetOperandType(0, typeof(Position));
            add.SetOperandType(1, typeof(Position));

            Assert.AreEqual(add.outputSlots[0].property.type, typeof(Position));
            Assert.AreEqual(add.outputSlots[0].space, VFXCoordinateSpace.Local);

            add.inputSlots[0].space = VFXCoordinateSpace.World;
            Assert.AreEqual(add.inputSlots[0].space, VFXCoordinateSpace.World);
            Assert.AreEqual(add.inputSlots[1].space, VFXCoordinateSpace.Local);
            Assert.AreEqual(add.outputSlots[0].space, VFXCoordinateSpace.World);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(add.outputSlots[0].GetExpression());

            var allExpr = CollectParentExpression(result).ToArray();
            Assert.IsTrue(allExpr.Count(o =>
            {
                return o.operation == VFXExpressionOperation.LocalToWorld;
            }) == 1);
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

            position_A.inputSlots[0].space = VFXCoordinateSpace.World;
            Assert.AreEqual(VFXCoordinateSpace.World, position_A.outputSlots[0].space);

            position_B.inputSlots[0].space = VFXCoordinateSpace.Local;
            Assert.AreEqual(VFXCoordinateSpace.Local, position_B.outputSlots[0].space);

            position_B.inputSlots[0].Link(position_A.outputSlots[0]);
            Assert.AreEqual(VFXCoordinateSpace.World, position_B.outputSlots[0].space);

            position_A.inputSlots[0].space = VFXCoordinateSpace.Local;
            Assert.AreEqual(VFXCoordinateSpace.Local, position_B.outputSlots[0].space);
        }

        [Test]
        public void SpaceConversion_Vector3_To_ArcSphere_Center_DoesntExcept_Conversion()
        {
            var rotate3D = ScriptableObject.CreateInstance<Rotate3D>();
            var arcSphere = ScriptableObject.CreateInstance<VFXInlineOperator>();

            arcSphere.SetSettingValue("m_Type", (SerializableType)typeof(ArcSphere));

            arcSphere.inputSlots[0][0][0].Link(rotate3D.outputSlots[0]); //link result of rotate3D to center of arcSphere

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultCenter = context.Compile(arcSphere.inputSlots[0][0][0].GetExpression());

            var allExprCenter = CollectParentExpression(resultCenter).ToArray();
            Assert.IsFalse(allExprCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal)); //everything is within the same space by default

            arcSphere.inputSlots[0].space = VFXCoordinateSpace.World;

            resultCenter = context.Compile(arcSphere.inputSlots[0][0][0].GetExpression());
            allExprCenter = CollectParentExpression(resultCenter).ToArray();

            Assert.IsTrue(allExprCenter.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }

        [Test]
        public void SpaceConversion_Sphere_Unexpected_Linking_MasterSlot()
        {
            var sphere_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var sphere_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            sphere_A.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            sphere_B.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            sphere_A.inputSlots[0].space = VFXCoordinateSpace.World;
            sphere_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            Assert.AreEqual(VFXCoordinateSpace.World, sphere_A.outputSlots[0].space);
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][1].Link(sphere_A.outputSlots[0][1]); //link radius to other radius
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultCenter = context.Compile(sphere_B.outputSlots[0][0].GetExpression());
            var resultRadius = context.Compile(sphere_B.outputSlots[0][1].GetExpression());

            var allExprCenter = CollectParentExpression(resultCenter).ToArray();
            var allExprRadius = CollectParentExpression(resultRadius).ToArray();

            Assert.IsFalse(allExprCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsFalse(allExprRadius.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
        }

        [Test]
        public void SpaceConversion_Sphere_Expected_Linking_Subslot()
        {
            var sphere_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var sphere_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            sphere_A.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            sphere_B.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            sphere_A.inputSlots[0][0].value = new Vector3(1, 2, 3);
            sphere_A.inputSlots[0].space = VFXCoordinateSpace.World;
            sphere_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            sphere_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            Assert.AreEqual(VFXCoordinateSpace.World, sphere_A.outputSlots[0].space);
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][0].Link(sphere_A.outputSlots[0][0]); //link sphere center to other sphere center
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            var allExprCenter = CollectParentExpression(sphere_B.outputSlots[0][0].GetExpression()).ToArray();
            var allExprRadius = CollectParentExpression(sphere_B.outputSlots[0][1].GetExpression()).ToArray();

            Assert.IsTrue(allExprCenter.Count(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal) == 1);
            Assert.IsFalse(allExprRadius.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
        }

        [Test]
        public void SpaceConversion_Position_Unexpected_Linking_Subslot()
        {
            var position_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var position_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            position_A.SetSettingValue("m_Type", (SerializableType)typeof(Position));
            position_B.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            position_A.inputSlots[0][0].value = new Vector3(1, 2, 3);
            position_A.inputSlots[0].space = VFXCoordinateSpace.World;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            for (int i = 0; i < 3; ++i)
            {
                position_A.outputSlots[0][0][i].Link(position_B.inputSlots[0][0][i]);
            }

            var allExprPosition = CollectParentExpression(position_B.outputSlots[0][0].GetExpression()).ToArray();

            Assert.AreEqual(VFXCoordinateSpace.World, position_A.outputSlots[0].space);
            Assert.AreEqual(VFXCoordinateSpace.Local, position_B.outputSlots[0].space);
            Assert.IsFalse(allExprPosition.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
        }

        [Test]
        public void SpaceConversion_Conversion_Expected_Linking_Position_To_Line()
        {
            var position_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var position_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            position_A.SetSettingValue("m_Type", (SerializableType)typeof(Position));
            position_B.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            position_A.inputSlots[0][0].value = new Vector3(1, 2, 3);
            position_A.inputSlots[0].space = VFXCoordinateSpace.World;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            var line = ScriptableObject.CreateInstance<VFXInlineOperator>();
            line.SetSettingValue("m_Type", (SerializableType)typeof(Line));

            line.inputSlots[0].space = VFXCoordinateSpace.Local;
            line.inputSlots[0][0].Link(position_A.outputSlots[0]);
            line.inputSlots[0][1].Link(position_B.outputSlots[0]);

            var lineOutputSlotA = CollectParentExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            var lineOutputSlotB = CollectParentExpression(line.outputSlots[0][1].GetExpression()).ToArray();

            Assert.AreEqual(line.inputSlots[0].space, VFXCoordinateSpace.Local);
            Assert.IsTrue(lineOutputSlotA.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);
            Assert.IsFalse(lineOutputSlotB.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            line.inputSlots[0].space = VFXCoordinateSpace.World;
            lineOutputSlotA = CollectParentExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            lineOutputSlotB = CollectParentExpression(line.outputSlots[0][1].GetExpression()).ToArray();
            Assert.AreEqual(line.inputSlots[0].space, VFXCoordinateSpace.World);
            Assert.IsFalse(lineOutputSlotA.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsTrue(lineOutputSlotB.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }

        [Test]
        public void SpaceConversion_Conversion_Expected_Between_Slot_Block_And_Context()
        {
            var initializeContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var positionSphere = ScriptableObject.CreateInstance<PositionSphere>();
            initializeContext.AddChild(positionSphere);

            //Default is expected to be in same space between block & context
            var slotSpherePositionExpressions = CollectParentExpression(positionSphere.inputSlots[0][0][0].GetExpression()).ToArray();
            Assert.IsTrue(slotSpherePositionExpressions.Any());
            Assert.IsFalse(slotSpherePositionExpressions.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //Now switch space of block
            initializeContext.space = VFXCoordinateSpace.World;
            slotSpherePositionExpressions = CollectParentExpression(positionSphere.inputSlots[0][0][0].GetExpression()).ToArray();
            Assert.IsTrue(slotSpherePositionExpressions.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
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
            var slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.AreEqual(VFXCoordinateSpace.Local, transformSpace.inputSlots[0].space);
            Assert.IsFalse(slotVectorExpression.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //World => Local
            transformSpace.inputSlots[0].space = VFXCoordinateSpace.World;
            slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 0);
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);

            //World => World
            transformSpace.SetSettingValue("m_targetSpace", VFXCoordinateSpace.World);
            slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsFalse(slotVectorExpression.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //Local => World
            transformSpace.inputSlots[0].space = VFXCoordinateSpace.Local;
            slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 0);
        }

        [Test]
        public void SpaceConversion_Verify_Direction_Use_TransformDir()
        {
            var transformSpace = ScriptableObject.CreateInstance<ChangeSpace>();
            transformSpace.SetOperandType(typeof(DirectionType));
            transformSpace.SetSettingValue("m_targetSpace", VFXCoordinateSpace.World);
            var expressions = CollectParentExpression(transformSpace.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(expressions.Count(o => o is VFXExpressionTransformDirection) == 1);
        }

        [Test]
        public void SpaceConversion_Verify_Vector_Use_TransformVec()
        {
            var transformSpace = ScriptableObject.CreateInstance<ChangeSpace>();
            transformSpace.SetOperandType(typeof(Vector));
            transformSpace.SetSettingValue("m_targetSpace", VFXCoordinateSpace.World);
            var expressions = CollectParentExpression(transformSpace.outputSlots[0].GetExpression()).ToArray();
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

            Assert.AreEqual(VFXCoordinateSpace.Local, direction.outputSlots[0].space);
            position.inputSlots[0].space = VFXCoordinateSpace.World;
            Assert.AreEqual(VFXCoordinateSpace.World, direction.outputSlots[0].space);
        }

        [Test]
        public void Space_Main_Camera()
        {
            var mainCamera = ScriptableObject.CreateInstance<MainCamera>();
            mainCamera.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            Assert.AreEqual(VFXCoordinateSpace.World, mainCamera.outputSlots[0].space);
            foreach (var slot in mainCamera.outputSlots[0].GetExpressionSlots())
            {
                var expressions = CollectParentExpression(slot.GetExpression()).ToArray();
                Assert.IsFalse(expressions.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            }
        }

        [Test]
        public void Space_MainCamera_To_Block_ProjectOnDepth()
        {
            var mainCamera = ScriptableObject.CreateInstance<MainCamera>();
            var initialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var projectOnDepth = ScriptableObject.CreateInstance<PositionDepth>();
            projectOnDepth.SetSettingValue("camera", CameraMode.Custom);

            initialize.space = VFXCoordinateSpace.World;
            mainCamera.outputSlots[0].Link(projectOnDepth.inputSlots[0]);
            initialize.AddChild(projectOnDepth);

            foreach (var slot in mainCamera.outputSlots[0].GetExpressionSlots())
            {
                var expressions = CollectParentExpression(slot.GetExpression()).ToArray();
                Assert.IsFalse(expressions.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            }

            var slotMatrixMainCameraOutput = mainCamera.outputSlots[0][0];
            var slotMatrixProjectInput = projectOnDepth.inputSlots[0][0];

            var expressionSlotMatrix = CollectParentExpression(slotMatrixMainCameraOutput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsFalse(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal || o.operation == VFXExpressionOperation.LocalToWorld));

            expressionSlotMatrix = CollectParentExpression(slotMatrixProjectInput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsFalse(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal || o.operation == VFXExpressionOperation.LocalToWorld));

            initialize.space = VFXCoordinateSpace.Local;

            expressionSlotMatrix = CollectParentExpression(slotMatrixMainCameraOutput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsFalse(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal || o.operation == VFXExpressionOperation.LocalToWorld)); //never at this stage

            expressionSlotMatrix = CollectParentExpression(slotMatrixProjectInput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal)); //context in local, transform World to Local is excpected
        }

        [Test]
        public void Space_Slot_Sanitize_Still_Possible_Simple_Sphere()
        {
            var branch = ScriptableObject.CreateInstance<Operator.Branch>();
            branch.SetOperandType(typeof(Sphere));

            var slot = branch.inputSlots[1];
            Assert.AreEqual(typeof(Sphere), slot.property.type);

            slot.space = VFXCoordinateSpace.World;
            Assert.AreEqual(VFXCoordinateSpace.World, slot.space);
            slot.AddChild(VFXSlot.Create(new VFXProperty(typeof(float), "hacked"), VFXSlot.Direction.kInput), -1, false);
            slot.Sanitize(-1);
            Assert.AreEqual(VFXCoordinateSpace.World, branch.inputSlots[1].space);
        }

        #pragma warning disable 0414
        private static bool[] trueOrFalse = { true, false };
        #pragma warning restore 0414

        [Test]
        public void Space_Slot_Sanitize_Still_Possible_ArcSphere([ValueSource("trueOrFalse")] bool fromParentToChildSanitize, [ValueSource("trueOrFalse")] bool hackChildSphere)
        {
            var branch = ScriptableObject.CreateInstance<Operator.Branch>();
            branch.SetOperandType(typeof(ArcSphere));

            var slot = branch.inputSlots[1];
            Assert.AreEqual(typeof(ArcSphere), slot.property.type);

            slot.space = VFXCoordinateSpace.World;
            Assert.AreEqual(VFXCoordinateSpace.World, slot.space);

            var slotToHack = hackChildSphere ? slot : slot.children.First();
            slotToHack.AddChild(VFXSlot.Create(new VFXProperty(typeof(float), "hacked"), VFXSlot.Direction.kInput), -1, false);
            if (fromParentToChildSanitize)
            {
                slot.Sanitize(-1);
                slot.children.First().Sanitize(-1);
            }
            else
            {
                slot.children.First().Sanitize(-1);
                slot.Sanitize(-1);
            }
            Assert.AreEqual(VFXCoordinateSpace.World, branch.inputSlots[1].space);
        }

        [Test]
        public void Space_Slot_Sanitize_Still_Possible_Even_With_Linked_Slot([ValueSource("trueOrFalse")] bool reverseSanitizeOrdering, [ValueSource("trueOrFalse")] bool hackArcSphere, [ValueSource("trueOrFalse")] bool hackSphere)
        {
            var inlineOpSphere = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineOpSphere.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            inlineOpSphere.inputSlots[0].space = VFXCoordinateSpace.World;

            var inlineOpArcSphere = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineOpArcSphere.SetSettingValue("m_Type", (SerializableType)typeof(ArcSphere));
            inlineOpArcSphere.inputSlots[0].space = VFXCoordinateSpace.Local;

            inlineOpSphere.outputSlots[0].Link(inlineOpArcSphere.inputSlots[0][0]);
            Assert.IsTrue(inlineOpSphere.outputSlots[0].HasLink());
            {
                var allExpr = CollectParentExpression(inlineOpArcSphere.outputSlots[0][0][0].GetExpression()).ToArray();
                Assert.IsTrue(allExpr.Count(o =>
                {
                    return o.operation == VFXExpressionOperation.WorldToLocal;
                }) == 1);
            }

            var objs = new HashSet<ScriptableObject>();
            inlineOpSphere.CollectDependencies(objs);
            inlineOpArcSphere.CollectDependencies(objs);

            //Hacking type to simulation a change of type description
            var allSlot = objs.OfType<VFXSlot>();
            var hackedSlot = Enumerable.Empty<VFXSlot>();
            if (hackArcSphere)
            {
                hackedSlot = hackedSlot.Concat(allSlot.Where(o => o.property.type == typeof(ArcSphere)));
            }

            if (hackSphere)
            {
                hackedSlot = hackedSlot.Concat(allSlot.Where(o => o.property.type == typeof(Sphere)));
            }

            foreach (var slotToHack in hackedSlot)
            {
                slotToHack.AddChild(VFXSlot.Create(new VFXProperty(typeof(float), "hacked"), VFXSlot.Direction.kInput), -1, false);
            }

            //Apply Sanitize
            var objsEnumerable = objs.AsEnumerable<ScriptableObject>();
            if (reverseSanitizeOrdering)
            {
                objsEnumerable = objsEnumerable.Reverse();
            }

            foreach (var obj in objsEnumerable.OfType<VFXModel>())
            {
                obj.Sanitize(-1);
            }

            if (!hackArcSphere)
            {
                Assert.IsTrue(inlineOpSphere.outputSlots[0].HasLink());
            } // else, expected disconnection, parent has changed (log a message like this " didnt match the type layout. It is recreated and all links are lost.")

            if (inlineOpSphere.outputSlots[0].HasLink())
            {
                var allExpr = CollectParentExpression(inlineOpArcSphere.outputSlots[0][0][0].GetExpression()).ToArray();
                Assert.IsTrue(allExpr.Count(o =>
                {
                    return o.operation == VFXExpressionOperation.WorldToLocal;
                }) == 1);
            }
        }
    }
}
#endif

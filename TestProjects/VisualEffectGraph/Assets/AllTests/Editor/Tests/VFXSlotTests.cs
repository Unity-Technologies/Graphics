#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSlotTests
    {
        class VFXDummySlot : VFXSlot
        {
        }

        [Test]
        public void Link()
        {
            VFXSlot input = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);

            input.Link(output);

            Assert.AreEqual(1, input.GetNbLinks());
            Assert.AreEqual(1, output.GetNbLinks());
            Assert.AreEqual(output, input.refSlot);
            Assert.AreEqual(output, output.refSlot);
        }

        [Test]
        public void Unlink()
        {
            VFXSlot input = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);

            input.Link(output);
            input.Unlink(output);

            Assert.AreEqual(0, input.GetNbLinks());
            Assert.AreEqual(0, output.GetNbLinks());
            Assert.AreEqual(input, input.refSlot);
            Assert.AreEqual(output, output.refSlot);
        }

        [Test]
        public void Link_Multiple()
        {
            VFXSlot input0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot input1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);
            VFXSlot output1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);

            output0.Link(input0);
            output0.Link(input1);

            Assert.AreEqual(2, output0.GetNbLinks());

            output1.Link(input0);

            Assert.AreEqual(1, input0.GetNbLinks());
            Assert.AreEqual(1, input1.GetNbLinks());
            Assert.AreEqual(1, output0.GetNbLinks());
            Assert.AreEqual(1, output1.GetNbLinks());
            Assert.AreEqual(output1, input0.refSlot);
            Assert.AreEqual(output0, input1.refSlot);
        }

        [Test]
        public void UnlinkAll()
        {
            const int NB_INPUTS = 10;

            VFXSlot output = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);
            for (int i = 0; i < NB_INPUTS; ++i)
                output.Link(VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput));

            Assert.AreEqual(NB_INPUTS, output.GetNbLinks());

            output.UnlinkAll();

            Assert.AreEqual(0, output.GetNbLinks());
        }

        [Test]
        public void Link_Fail()
        {
            VFXSlot input0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot input1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);

            VFXSlot output0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);

            input0.Link(input1);
            output0.Link(output1);

            Assert.AreEqual(0, input0.GetNbLinks());
            Assert.AreEqual(0, input1.GetNbLinks());
            Assert.AreEqual(0, output0.GetNbLinks());
            Assert.AreEqual(0, output1.GetNbLinks());
        }

        private void CheckVectorSlotCreation(Type type, VFXSlot.Direction direction, int expectionChildrenNb)
        {
            VFXSlot slot = VFXSlot.Create(new VFXProperty(type, "test"), direction);

            Assert.IsNotNull(slot);
            Assert.AreEqual(expectionChildrenNb, slot.GetNbChildren());
            Assert.IsInstanceOf<VFXExpressionCombine>(slot.GetExpression());

            foreach (var child in slot.children)
            {
                Assert.IsNotNull(child);
                Assert.AreEqual(0, child.GetNbChildren());
                Assert.IsInstanceOf<VFXExpressionExtractComponent>(child.GetExpression());
            }
        }

        [Test]
        public void Create()
        {
            CheckVectorSlotCreation(typeof(Vector2), VFXSlot.Direction.kInput, 2);
            CheckVectorSlotCreation(typeof(Vector3), VFXSlot.Direction.kInput, 3);
            CheckVectorSlotCreation(typeof(Vector4), VFXSlot.Direction.kInput, 4);

            CheckVectorSlotCreation(typeof(Vector2), VFXSlot.Direction.kOutput, 2);
            CheckVectorSlotCreation(typeof(Vector3), VFXSlot.Direction.kOutput, 3);
            CheckVectorSlotCreation(typeof(Vector4), VFXSlot.Direction.kOutput, 4);
        }

        [Test]
        public void CheckExpression()
        {
            VFXSlot sphereSlot = VFXSlot.Create(new VFXProperty(typeof(Sphere), "sphere"), VFXSlot.Direction.kInput);
            VFXSlot floatSlot = VFXSlot.Create(new VFXProperty(typeof(float), "float"), VFXSlot.Direction.kOutput);

            sphereSlot[0][0].Link(floatSlot);
            sphereSlot[1].Link(floatSlot);

            var expr = sphereSlot[0][0].GetExpression();
            Assert.IsInstanceOf<VFXExpressionExtractComponent>(expr);
            Assert.AreEqual(floatSlot.GetExpression(), expr.parents[0].parents[0]);
            Assert.AreEqual(floatSlot.GetExpression(), sphereSlot[1].GetExpression());

            floatSlot.UnlinkAll();
            expr = sphereSlot[0][0].GetExpression();
            Assert.IsInstanceOf<VFXExpressionExtractComponent>(expr);
            Assert.AreNotEqual(floatSlot.GetExpression(), expr.parents[0].parents[0]);
            Assert.AreNotEqual(floatSlot.GetExpression(), sphereSlot[1].GetExpression());
        }

        static void CollectExpression(VFXExpression expression, List<VFXExpression> collected)
        {
            if (collected.Contains(expression))
                return;

            collected.Add(expression);
            foreach (var parent in expression.parents)
                CollectExpression(parent, collected);
        }

        static string DumpCheckDeterministicBehaviorFromLazyGetExpression(bool linkOnlySubSlot, bool linkToDirection, int[] invalidation)
        {
            var vec3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var direction = ScriptableObject.CreateInstance<VFXInlineOperator>();

            vec3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
            direction.SetSettingValue("m_Type", (SerializableType)typeof(DirectionType));

            if (linkToDirection)
            {
                if (linkOnlySubSlot)
                    vec3.outputSlots[0][1].Link(direction.inputSlots[0][0][1]);
                else
                    vec3.outputSlots[0].Link(direction.inputSlots[0]);
            }
            else
            {
                if (linkOnlySubSlot)
                    direction.outputSlots[0][0][1].Link(vec3.inputSlots[0][1]);

                direction.outputSlots[0].Link(vec3.inputSlots[0]);
            }

            Action<int> Invalidate = delegate(int i)
            {
                switch (i)
                {
                    case 0: direction.outputSlots[0].GetExpression(); break;
                    case 1: direction.inputSlots[0].GetExpression(); break;
                    case 2: vec3.inputSlots[0].GetExpression(); break;
                    case 3: vec3.outputSlots[0].GetExpression(); break;
                    default: break;
                }
            };

            foreach (var i in invalidation)
                Invalidate(i);

            var allParentExpr = new List<VFXExpression>();
            if (linkToDirection)
            {
                CollectExpression(direction.outputSlots[0].GetExpression(), allParentExpr);
            }
            else
            {
                CollectExpression(vec3.outputSlots[0].GetExpression(), allParentExpr);
            }
            string dump = allParentExpr.Select(o => o.GetType().Name).Aggregate((a, b) => a + ", " + b);
            vec3.outputSlots[0].UnlinkAll();
            ScriptableObject.DestroyImmediate(vec3);
            ScriptableObject.DestroyImmediate(direction);
            return dump;
        }

        #pragma warning disable 0414
        private static bool[] linkSubSlotOnly = new[] { true, false };
        private static bool[] linkToDirection = new[] { true, false };

        #pragma warning restore 0414

        [Test]
        public void Check_Deterministic_Behavior_From_Lazy_Get_Expression_At_Several_Place([ValueSource("linkSubSlotOnly")] bool linkSubSlotOnly, [ValueSource("linkToDirection")] bool linkToDirection)
        {
            var arrVariants = Enumerable.Range(0, 4).Select(o => Enumerable.Range(0, 4));
            IEnumerable<IEnumerable<int>> empty = new[] { Enumerable.Empty<int>() };
            var combinations = arrVariants.Aggregate(empty, (x, y) => x.SelectMany(accSeq => y.Select(item => accSeq.Concat(new[] { item }))))
                                           .Select(o => o.ToArray()).ToArray();

            //Cartesian product in combinations of [0,1,2,3]x[0,1,2,3] => {0,0,0,0}, {0,0,0,1}, ...
            //We create a simple graph node_A (Vector3) and node_B (Vector3 or Direction)
            //A link is done (to a subslot or directly to the masterslot)
            //Then, we invalidate input/output slots following order provided by combination
            //Finally, we compute expression tree from last output slot & dump it : all invalidation order should provide the same graph
            //N.B.: This is a regression test, fixing an issue introduced rarely reproduced with spaceable slots

            var result = combinations.Select(o =>
            {
                return new
                {
                    combination = o,
                    res = DumpCheckDeterministicBehaviorFromLazyGetExpression(linkSubSlotOnly, linkToDirection, o),
                };
            });

            var resultGroup = result.GroupBy(o => o.res);
            Assert.AreEqual(1, resultGroup.Count());
        }

        [Test]
        public void Slot_Copy_Value_Vector3_To_Position()
        {
            var vector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            vector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            var position = ScriptableObject.CreateInstance<VFXInlineOperator>();
            position.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            var v3_Ref = new Vector3(1, 2, 3);
            vector3.inputSlots[0].value = v3_Ref;

            VFXSlot.CopyLinksAndValue(position.inputSlots[0], vector3.inputSlots[0], true);

            var positionValue = (Position)position.inputSlots[0].value;

            Assert.AreEqual(v3_Ref.x, positionValue.position.x);
            Assert.AreEqual(v3_Ref.y, positionValue.position.y);
            Assert.AreEqual(v3_Ref.y, positionValue.position.y);
        }

        [Test]
        public void Slot_Copy_Value_Position_To_Vector3()
        {
            var vector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            vector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            var position = ScriptableObject.CreateInstance<VFXInlineOperator>();
            position.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            var v3_Ref = new Vector3(1, 2, 3);
            position.inputSlots[0].value = (Position)v3_Ref;

            VFXSlot.CopyLinksAndValue(vector3.inputSlots[0], position.inputSlots[0], true);

            var vector3Value = (Vector3)vector3.inputSlots[0].value;

            Assert.AreEqual(v3_Ref.x, vector3Value.x);
            Assert.AreEqual(v3_Ref.y, vector3Value.y);
            Assert.AreEqual(v3_Ref.y, vector3Value.y);
        }


        [Test]
        public void Slot_Copy_Link_Transform_To_OrientedBox()
        {
            var sourceOfLink = ScriptableObject.CreateInstance<VFXInlineOperator>();
            sourceOfLink.SetSettingValue("m_Type", (SerializableType)typeof(float));

            var transform = ScriptableObject.CreateInstance<VFXInlineOperator>();
            transform.SetSettingValue("m_Type", (SerializableType)typeof(Transform));

            var orientedBox = ScriptableObject.CreateInstance<VFXInlineOperator>();
            orientedBox.SetSettingValue("m_Type", (SerializableType)typeof(OrientedBox));

            Assert.IsTrue(transform.inputSlots[0][0][0].Link(sourceOfLink.outputSlots[0]));
            VFXSlot.CopyLinksAndValue(orientedBox.inputSlots[0], transform.inputSlots[0], true);
            Assert.IsTrue(orientedBox.inputSlots[0][0][0].HasLink());
        }

        [Test]
        public void Slot_Copy_Link_OrientedBox_To_Transform()
        {
            var sourceOfLink = ScriptableObject.CreateInstance<VFXInlineOperator>();
            sourceOfLink.SetSettingValue("m_Type", (SerializableType)typeof(float));

            var transform = ScriptableObject.CreateInstance<VFXInlineOperator>();
            transform.SetSettingValue("m_Type", (SerializableType)typeof(Transform));

            var orientedBox = ScriptableObject.CreateInstance<VFXInlineOperator>();
            orientedBox.SetSettingValue("m_Type", (SerializableType)typeof(OrientedBox));

            Assert.IsTrue(orientedBox.inputSlots[0][0][0].Link(sourceOfLink.outputSlots[0]));
            VFXSlot.CopyLinksAndValue(transform.inputSlots[0], orientedBox.inputSlots[0], true);
            Assert.IsTrue(transform.inputSlots[0][0][0].HasLink());
        }

        [Test]
        public void Slot_Copy_Link_Vector3_To_Position([ValueSource("linkSubSlotOnly")] bool linkSubSlotOnly)
        {
            var sourceOfLink = ScriptableObject.CreateInstance<VFXInlineOperator>();
            sourceOfLink.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            var vector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            vector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            bool linkSucceed;
            if (linkSubSlotOnly)
            {
                linkSucceed = vector3.inputSlots[0][0].Link(sourceOfLink.outputSlots[0][0]);
            }
            else
            {
                linkSucceed = vector3.inputSlots[0].Link(sourceOfLink.outputSlots[0]);
            }
            Assert.IsTrue(linkSucceed);

            var position = ScriptableObject.CreateInstance<VFXInlineOperator>();
            position.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            VFXSlot.CopyLinksAndValue(position.inputSlots[0], vector3.inputSlots[0], true);

            if (linkSubSlotOnly)
            {
                Assert.IsTrue(position.inputSlots[0][0][0].HasLink());
            }
            else
            {
                Assert.IsTrue(position.inputSlots[0].HasLink());
            }
        }

        [Test]
        public void Slot_Copy_Link_Position_To_Vector3([ValueSource("linkSubSlotOnly")] bool linkSubSlotOnly)
        {
            var sourceOfLink = ScriptableObject.CreateInstance<VFXInlineOperator>();
            sourceOfLink.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            var position = ScriptableObject.CreateInstance<VFXInlineOperator>();
            position.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            bool linkSucceed;
            if (linkSubSlotOnly)
            {
                linkSucceed = position.inputSlots[0][0][0].Link(sourceOfLink.outputSlots[0][0]);
            }
            else
            {
                linkSucceed = position.inputSlots[0].Link(sourceOfLink.outputSlots[0]);
            }
            Assert.IsTrue(linkSucceed);

            var vector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            vector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            VFXSlot.CopyLinksAndValue(vector3.inputSlots[0], position.inputSlots[0], true);

            if (linkSubSlotOnly)
            {
                Assert.IsTrue(vector3.inputSlots[0][0].HasLink());
            }
            else
            {
                Assert.IsTrue(vector3.inputSlots[0].HasLink());
            }
        }
    }
}
#endif

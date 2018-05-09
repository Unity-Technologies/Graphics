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
using System.Text;
using UnityEditor.VFX.Operator;

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
            clamp.SetOperandType(0, typeof(int));
            clamp.SetOperandType(1, typeof(int));
            clamp.SetOperandType(2, typeof(int));
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
            dot.SetOperandType(typeof(Vector2));

            Assert.AreEqual(VFXValueType.Float, dot.outputSlots[0].GetExpression().valueType);

            var a = new Vector2(6, 7);
            var b = new Vector2(2, 3);

            dot.inputSlots[0].value = a;
            dot.inputSlots[1].value = b;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(dot.outputSlots[0].GetExpression());
            var final = result.Get<float>();

            Assert.AreEqual(Vector2.Dot(a, b), final);
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

        private static bool IsSlotCompatible(Type output, Type input)
        {
            var slotOutput = VFXSlot.Create(new VFXProperty(output, "o"), VFXSlot.Direction.kOutput);
            var slotInput = VFXSlot.Create(new VFXProperty(input, "i"), VFXSlot.Direction.kInput);
            return slotOutput.CanLink(slotInput);
        }

        private static Dictionary<Type, Type[]> ComputeHeuristcalAffinity()
        {
            var inputType = new[]
            {
                typeof(Vector4),
                typeof(Color),
                typeof(Vector3),
                typeof(Position),
                typeof(DirectionType),
                typeof(Vector),
                typeof(Vector2),
                typeof(float),
                typeof(int),
                typeof(uint),
            };

            var inputTypeHeurisicalDict = inputType.Select(o =>
                {
                    var baseValueType = VFXSlot.Create(new VFXProperty(o, "temp1"), VFXSlot.Direction.kOutput).DefaultExpr.valueType;
                    var baseChannelCount = VFXExpression.TypeToSize(baseValueType);
                    var baseIsKindOfInteger = o == typeof(uint) || o == typeof(int);

                    var compatibleSlotMetaData = inputType
                        .Where(s => s != o && IsSlotCompatible(o, s))
                        .Select(s =>
                    {
                        var otherValueType = VFXSlot.Create(new VFXProperty(s, "temp2"), VFXSlot.Direction.kOutput).DefaultExpr.valueType;
                        var otherChannelCount = VFXExpression.TypeToSize(otherValueType);
                        var otherIsKindOfInteger = s == typeof(uint) || s == typeof(int);

                        return new
                        {
                            type = s,
                            diffType = otherValueType == baseValueType ? 0 : 1,
                            diffChannelCount = Mathf.Abs(baseChannelCount - otherChannelCount),
                            diffPreferInteger = baseIsKindOfInteger == otherIsKindOfInteger ? 0 : 1
                        };
                    }).OrderBy(s => s.diffType)
                        .ThenBy(s => s.diffChannelCount)
                        .ThenBy(s => s.diffPreferInteger)
                        .ToArray();

                    return new KeyValuePair<Type, Type[]>
                    (
                        o,
                        compatibleSlotMetaData.Select(s => s.type).ToArray()
                    );
                }).ToDictionary(x => x.Key, x => x.Value);
            return inputTypeHeurisicalDict;
        }

        private static string DumpAffinityDictionnary(Dictionary<Type, Type[]> typeAffiny)
        {
            var dump = new StringBuilder();
            foreach (var type in typeAffiny)
            {
                dump.AppendFormat("{{ typeof({0}), new[] {{", type.Key.UserFriendlyName());
                dump.Append(type.Value.Select(o => string.Format("typeof({0})", o.UserFriendlyName())).Aggregate((a, b) => string.Format("{0}, {1}", a, b)));
                dump.Append("} },");
                dump.AppendLine();
            }
            return dump.ToString();
        }

        [Test]
        public void VerifyTypeCompatibility()
        {
            var affinityHeurisitic = ComputeHeuristcalAffinity();
            var dumpAffinityHeuristic = DumpAffinityDictionnary(affinityHeurisitic);
            var dumpAffinityCurrent = DumpAffinityDictionnary(VFXOperatorDynamicOperand.kTypeAffinity);
            Assert.AreEqual(dumpAffinityHeuristic, dumpAffinityCurrent, "kTypeAffinity or CanConvertFrom has been changed, it's not necessary an error, but consider it carefully and update kTypeAffinity");
        }

        [Test]
        public void ModuloNewWithInteger()
        {
            var a = 1610612737u;
            var b = 805306361u;

            var moduloUInt = ScriptableObject.CreateInstance<Operator.ModuloNew>();
            moduloUInt.SetOperandType(typeof(uint));

            var moduloFloat = ScriptableObject.CreateInstance<Operator.ModuloNew>();
            moduloFloat.SetOperandType(typeof(float));

            var aOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
            aOperator.SetSettingValue("m_Type", (SerializableType)typeof(uint));
            var bOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
            bOperator.SetSettingValue("m_Type", (SerializableType)typeof(uint));

            aOperator.inputSlots[0].value = a;
            bOperator.inputSlots[0].value = b;

            moduloUInt.inputSlots[0].Link(aOperator.outputSlots.First());
            moduloUInt.inputSlots[1].Link(bOperator.outputSlots.First());

            moduloFloat.inputSlots[0].Link(aOperator.outputSlots.First());
            moduloFloat.inputSlots[1].Link(bOperator.outputSlots.First());

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultUInt = context.Compile(moduloUInt.outputSlots[0].GetExpression());
            var resultFloat = context.Compile(moduloFloat.outputSlots[0].GetExpression());

            Assert.AreEqual(a % b, resultUInt.Get<uint>());
            Assert.AreEqual(Mathf.Repeat(a, b), resultFloat.Get<float>());
        }

        [Test]
        public void MinimumNewWithIdenityValue()
        {
            var a = new Vector2(2, 2);
            var b = new Vector3(3, 3, 3);
            var e = new Vector3(2, 2, 3);

            var min = ScriptableObject.CreateInstance<Operator.MinimumNew>();
            min.SetOperandType(0, a.GetType());
            min.SetOperandType(1, b.GetType());

            min.inputSlots[0].value = a;
            min.inputSlots[1].value = b;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultVector3Expression = context.Compile(min.outputSlots[0].GetExpression());
            var r = resultVector3Expression.Get<Vector3>();

            Assert.AreEqual(e.x, r.x);
            Assert.AreEqual(e.y, r.y);
            Assert.AreEqual(e.z, r.z);
        }

        private static KeyValuePair<Type, int>[] allOperatorUsingFloatN = new KeyValuePair<Type, int>[] {
            new KeyValuePair<Type, int>(typeof(Absolute), 1),
            new KeyValuePair<Type, int>(typeof(Add), 2),
            new KeyValuePair<Type, int>(typeof(AppendVector), 1),
            new KeyValuePair<Type, int>(typeof(Branch), 2),
            new KeyValuePair<Type, int>(typeof(Ceiling), 1),
            new KeyValuePair<Type, int>(typeof(Clamp), 3),
            new KeyValuePair<Type, int>(typeof(Cosine), 1),
            new KeyValuePair<Type, int>(typeof(ComponentMask), 1),
            new KeyValuePair<Type, int>(typeof(FitClamped), 5),
            new KeyValuePair<Type, int>(typeof(Discretize), 2),
            new KeyValuePair<Type, int>(typeof(Distance), 2),
            new KeyValuePair<Type, int>(typeof(Divide), 2),
            new KeyValuePair<Type, int>(typeof(DotProduct), 2),
            new KeyValuePair<Type, int>(typeof(Floor), 1),
            new KeyValuePair<Type, int>(typeof(Fraction), 1),
            new KeyValuePair<Type, int>(typeof(Length), 1),
            new KeyValuePair<Type, int>(typeof(Lerp), 3),
            new KeyValuePair<Type, int>(typeof(Maximum), 2),
            new KeyValuePair<Type, int>(typeof(Minimum), 2),
            new KeyValuePair<Type, int>(typeof(Modulo), 2),
            new KeyValuePair<Type, int>(typeof(Multiply), 2),
            new KeyValuePair<Type, int>(typeof(Normalize), 1),
            new KeyValuePair<Type, int>(typeof(OneMinus), 1),
            new KeyValuePair<Type, int>(typeof(Power), 2),
            new KeyValuePair<Type, int>(typeof(Operator.Random), 2),
            new KeyValuePair<Type, int>(typeof(Reciprocal), 1),
            new KeyValuePair<Type, int>(typeof(Remap), 5),
            new KeyValuePair<Type, int>(typeof(RemapToNegOnePosOne), 1),
            new KeyValuePair<Type, int>(typeof(RemapToZeroOne), 1),
            new KeyValuePair<Type, int>(typeof(Round), 1),
            new KeyValuePair<Type, int>(typeof(Saturate), 1),
            new KeyValuePair<Type, int>(typeof(SawtoothWave), 2),
            new KeyValuePair<Type, int>(typeof(Sign), 1),
            new KeyValuePair<Type, int>(typeof(Sine), 1),
            new KeyValuePair<Type, int>(typeof(SineWave), 2),
            new KeyValuePair<Type, int>(typeof(Smoothstep), 3),
            new KeyValuePair<Type, int>(typeof(SquaredDistance), 2),
            new KeyValuePair<Type, int>(typeof(SquaredLength), 1),
            new KeyValuePair<Type, int>(typeof(SquareRoot), 1),
            new KeyValuePair<Type, int>(typeof(SquareWave), 2),
            new KeyValuePair<Type, int>(typeof(Step), 2),
            new KeyValuePair<Type, int>(typeof(Subtract), 2),
            new KeyValuePair<Type, int>(typeof(Swizzle), 1),
            new KeyValuePair<Type, int>(typeof(Tangent), 1),
            new KeyValuePair<Type, int>(typeof(TriangleWave), 2)
        };

        [Test]
        public void VerifyAllOperatorUsingFloatNAreRegistered()
        {
            //Use reflexion only in test to avoid slowing down domain reload due to huge reflection
            var allClasses = VFXLibrary.FindConcreteSubclasses(typeof(VFXOperator)); //even without attribute

            var typeWithFloatN = new List<KeyValuePair<Type, int>>();
            foreach (var op in allClasses)
            {
                var currentOp = ScriptableObject.CreateInstance(op) as VFXOperator;
                var floatNCount = currentOp.inputSlots.Where(o => o.property.type == typeof(FloatN)).Count();
                if (floatNCount > 0)
                {
                    typeWithFloatN.Add(new KeyValuePair<Type, int>(op, floatNCount));
                }
            }

            Func<IEnumerable<KeyValuePair<Type, int>>, string> fnDumpList = delegate(IEnumerable<KeyValuePair<Type, int>> input)
                {
                    if (!input.Any())
                        return "new KeyValuePair<Type, int>[] { }";

                    return "new KeyValuePair<Type, int>[] { " + input.Select(o => string.Format("new KeyValuePair<Type, int>(typeof({0}), {1})", o.Key.Name, o.Value)).Aggregate((a, b) => a + ",\n " + b) + " };";
                };

            var expected = fnDumpList(typeWithFloatN);
            var current = fnDumpList(allOperatorUsingFloatN);

            var test = allSanitizeTest;

            Assert.AreEqual(expected, current);
        }

        public static bool[] linkedSlot = new bool[] { true, false };

        public class SanitizeParam
        {
            public Type type;
            public Type[] inputSlotType;

            public override string ToString()
            {
                var list = inputSlotType == null || inputSlotType.Length == 0 ? string.Empty : inputSlotType.Select(o => o == null ? "None" : o.UserFriendlyName()).Aggregate((a, b) => a + ", " + b);
                return string.Format("{0} with {1}", type.Name, list);
            }
        }

        private static SanitizeParam[] s_allSanitizeTest;
        public static SanitizeParam[] allSanitizeTest
        {
            get
            {
                if (s_allSanitizeTest != null)
                    return s_allSanitizeTest;

                var availableInputType = new Type[] { null, typeof(float), typeof(Vector2), typeof(Vector3), typeof(Vector4) };
                var maxShuffleCount = 3; //actual max : maxShuffleCount * maxShuffleCount

                UnityEngine.Random.InitState(42);
                var sanitizeTest = new List<SanitizeParam>();
                foreach (var type in allOperatorUsingFloatN)
                {
                    for (int i = 0; i < maxShuffleCount * Mathf.Min(type.Value, maxShuffleCount); ++i)
                    {
                        var param = new SanitizeParam()
                        {
                            type = type.Key,
                            inputSlotType = Enumerable.Repeat((Type)null, type.Value).ToArray()
                        };

                        for (int j = 0; j < type.Value; ++j)
                        {
                            param.inputSlotType[j] = availableInputType[UnityEngine.Random.Range(0, availableInputType.Length)];
                        }
                        sanitizeTest.Add(param);
                    }
                }
                s_allSanitizeTest = sanitizeTest.GroupBy(o => o.ToString()).Select(o => o.First()).ToArray();
                return s_allSanitizeTest;
            }
        }

        [Test]
        public void SanitizeBehavior([ValueSource("allSanitizeTest")] SanitizeParam op)
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var currentOperator = ScriptableObject.CreateInstance(op.type) as VFXOperator;

            graph.AddChild(currentOperator);

            var slotInput = currentOperator.inputSlots.Where(o => o.property.type == typeof(FloatN));

            for (int i = 0; i < op.inputSlotType.Length; ++i)
            {
                Type type = op.inputSlotType[i];
                if (type == null)
                    continue;

                var inlineOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
                inlineOperator.SetSettingValue("m_Type", (SerializableType)type);
                graph.AddChild(inlineOperator);

                slotInput.ElementAt(i).Link(inlineOperator.outputSlots.FirstOrDefault());
            }

            //Always connect output slot
            foreach (var slot in currentOperator.outputSlots)
            {
                var inlineOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
                inlineOperator.SetSettingValue("m_Type", (SerializableType)currentOperator.outputSlots[0].property.type);
                graph.AddChild(inlineOperator);

                slot.Link(inlineOperator.inputSlots.FirstOrDefault());
            }

            foreach (var slot in currentOperator.inputSlots.Where(o => o.property.type == typeof(FloatN) && !o.HasLink()))
            {
                slot.value = UnityEngine.Random.Range(-10, 10);
            }

            //Let's do it !
            graph.SanitizeGraph();

            //And verify equivalence...
            var newOperator = graph.children.Where(o => !(o is VFXInlineOperator)).FirstOrDefault() as VFXOperator;
            Assert.AreNotEqual(currentOperator, newOperator);
            Assert.IsNotNull(newOperator);

            for (int i = 0; i < currentOperator.inputSlots.Count; ++i)
            {
                var currentInputSlot = currentOperator.inputSlots[0];
                var newInputSlot = newOperator.inputSlots[0];
                if (currentInputSlot.HasLink())
                {
                    Assert.IsTrue(newInputSlot.HasLink());
                }
                else
                {
                    if (currentInputSlot.property.type == typeof(FloatN))
                    {
                        object value = null;
                        var floatN = (FloatN)currentInputSlot.value;
                        var newType = newInputSlot.property.type;
                        if (newType == typeof(float))
                        {
                            value = (float)floatN;
                        }
                        else if (newType == typeof(Vector2))
                        {
                            value = (Vector2)floatN;
                        }
                        else if (newType == typeof(Vector3))
                        {
                            value = (Vector3)floatN;
                        }
                        else if (newType == typeof(Vector4))
                        {
                            value = (Vector4)floatN;
                        }
                        Assert.AreEqual(value, newInputSlot.value);
                    }
                    else
                    {
                        Assert.AreEqual(currentInputSlot.value, newInputSlot.value);
                    }
                }
            }

            for (int i = 0; i < currentOperator.outputSlots.Count; ++i)
            {
                Assert.IsTrue(newOperator.outputSlots[i].HasLink());
            }
        }
    }
}

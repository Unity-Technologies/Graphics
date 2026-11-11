using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.VFX.Operator;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    public class VFXOperatorTypeCompatibilityTests
    {
        [Test]
        public void Verify_TypeChange_Keep_Connection_When_Still_Compatible()
        {
            var cos_A = ScriptableObject.CreateInstance<Operator.Cosine>();
            cos_A.SetOperandType(typeof(Vector3));
            var cos_B = ScriptableObject.CreateInstance<Operator.Cosine>();
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

        private static bool IsSlotCompatible(Type output, Type input)
        {
            var slotOutput = VFXSlot.Create(new VFXProperty(output, "o"), VFXSlot.Direction.kOutput);
            var slotInput = VFXSlot.Create(new VFXProperty(input, "i"), VFXSlot.Direction.kInput);
            return slotOutput.CanLink(slotInput);
        }

        private static Dictionary<Type, Type[]> ComputeHeuristcalAffinity()
        {
            /* Heuristical function which is a bit expensive but expects the same result as kTypeAffinity */
            var inputType = new[]
            {
                typeof(Matrix4x4),
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
                var affinity = type.Value.Select(o => string.Format("typeof({0})", o.UserFriendlyName()));
                if (affinity.Any())
                    dump.Append(affinity.Aggregate((a, b) => string.Format("{0}, {1}", a, b)));
                dump.Append("} },");
                dump.AppendLine();
            }
            return dump.ToString();
        }

        [Test]
        public void Verify_Type_Compatibility_Hasnt_Been_Changed()
        {
            var affinityHeurisitic = ComputeHeuristcalAffinity();
            var dumpAffinityHeuristic = DumpAffinityDictionnary(affinityHeurisitic);
            var dumpAffinityCurrent = DumpAffinityDictionnary(VFXOperatorDynamicOperand.kTypeAffinity);
            Assert.AreEqual(dumpAffinityHeuristic, dumpAffinityCurrent, "kTypeAffinity or CanConvertFrom has been changed, it's not necessary an error, but consider it carefully and update kTypeAffinity");
        }

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

#pragma warning disable 0414
        private static Type[] k_Completly_free_operator = new[] { typeof(Operator.Subtract), typeof(Operator.Add), typeof(Operator.Multiply), typeof(Operator.Divide) };
#pragma warning restore 0414
        [Test]
        public void Compute_Output_Type_All_Combinaison_And_Compare_With_Reference([ValueSource("k_Completly_free_operator")] Type concernedOperator)
        {
            var inputOrdering = VFXOperatorDynamicOperand.kExpectedTypeOrdering.Reverse().ToArray();

            var mapOfCombinaison = new Type[inputOrdering.Length, inputOrdering.Length];
            var obj = ScriptableObject.CreateInstance(concernedOperator);
            var op = obj as VFXOperatorNumericCascadedUnified;
            for (int i = 0; i < inputOrdering.Length; ++i)
            {
                for (int j = 0; j < inputOrdering.Length; ++j)
                {
                    op.SetOperandType(0, inputOrdering[i]);
                    op.SetOperandType(1, inputOrdering[j]);
                    mapOfCombinaison[i, j] = op.outputSlots[0].property.type;
                }
            }


            Func<IEnumerable<Type>, string> dumpArray = delegate(IEnumerable<Type> input)
            {
                return input.Select(o => o == null ? string.Empty : o.UserFriendlyName())
                    .Select((o, index) =>
                    {
                        var r = o;
                        if (index != input.Count() - 1)
                            for (var i = 0; i < 4 - o.Length / 4; ++i) r += "\t";
                        return r;
                    })
                    .Aggregate((a, b) => a + "|\t" + b);
            };

            Func<Type[, ], string> dumpRectangular = delegate(Type[,] intput)
            {
                var dump = "\n";
                dump += dumpArray(Enumerable.Repeat<Type>(null, 1).Concat(inputOrdering).ToArray());
                dump += "\n";
                for (int i = 0; i < inputOrdering.Length; i++)
                {
                    var current = mapOfCombinaison.Cast<Type>()
                        .Skip(inputOrdering.Length * i)
                        .Take(inputOrdering.Length)
                        .Reverse();
                    dump += dumpArray(Enumerable.Repeat<Type>(inputOrdering[i], 1).Concat(current));
                    dump += "\n";
                }
                return dump;
            };

            var reference = new Type[, ] //If this change, be careful with compatibility of visual effect
            {
                {typeof(int), typeof(uint), typeof(float), typeof(Vector2), typeof(Vector3), typeof(DirectionType), typeof(Vector), typeof(Position), typeof(Vector4)},
                {typeof(uint), typeof(uint), typeof(float), typeof(Vector2), typeof(Vector3), typeof(DirectionType), typeof(Vector), typeof(Position), typeof(Vector4)},
                {typeof(float), typeof(float), typeof(float), typeof(Vector2), typeof(Vector3), typeof(DirectionType), typeof(Vector), typeof(Position), typeof(Vector4)},
                {typeof(Vector2), typeof(Vector2), typeof(Vector2), typeof(Vector2), typeof(Vector3), typeof(DirectionType), typeof(Vector), typeof(Position), typeof(Vector4)},
                {typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(DirectionType), typeof(Vector), typeof(Position), typeof(Vector4)},
                {typeof(DirectionType), typeof(DirectionType), typeof(DirectionType), typeof(DirectionType), typeof(DirectionType), typeof(DirectionType), typeof(Vector), typeof(Position), typeof(Vector4)},
                {typeof(Vector), typeof(Vector), typeof(Vector), typeof(Vector), typeof(Vector), typeof(Vector), typeof(Vector), typeof(Position), typeof(Vector4)},
                {typeof(Position), typeof(Position), typeof(Position), typeof(Position), typeof(Position), typeof(Position), typeof(Position), typeof(Position), typeof(Vector4)},
                {typeof(Vector4), typeof(Vector4), typeof(Vector4), typeof(Vector4), typeof(Vector4), typeof(Vector4), typeof(Vector4), typeof(Vector4), typeof(Vector4)},
            };

            var referenceDump = dumpRectangular(reference);
            var currentDump = dumpRectangular(mapOfCombinaison);
            Assert.AreEqual(referenceDump, currentDump);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    abstract class VFXOperatorDynamicBranch : VFXOperatorDynamicType
    {
        public override IEnumerable<Type> validTypes
        {
            get
            {
                var outputTypes = new List<Type>();
                foreach (var slotType in VFXLibrary.GetSlotsType())
                {
                    var typesAttributes = slotType.GetCustomAttributes(typeof(VFXTypeAttribute), false);
                    if (typesAttributes.Length > 0)
                    {
                        var typeAttribute = typesAttributes[0] as VFXTypeAttribute;
                        if (typeAttribute != null && typeAttribute.usages.HasFlag(VFXTypeAttribute.Usage.ExcludeFromProperty))
                            continue;
                    }

                    outputTypes.Add(slotType);
                }
                outputTypes.Sort((i, j) => String.Compare(i.ToString(), j.ToString(), StringComparison.Ordinal));
                return outputTypes;
            }
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(m_Type, string.Empty));
            }
        }

        static Dictionary<Type, int> s_ExpressionCountPerType;
        private static int FindOrComputeExpressionCountPerType(Type type)
        {
            s_ExpressionCountPerType ??= new Dictionary<Type, int>();
            if (!s_ExpressionCountPerType.TryGetValue(type, out var count))
            {
                var tempInstance = VFXSlot.Create(new VFXPropertyWithValue(new VFXProperty(type, "temp")), VFXSlot.Direction.kInput);
                count = 0;
                foreach (var slot in tempInstance.GetExpressionSlots())
                {
                    count++;
                }
                s_ExpressionCountPerType.Add(type, count);
            }
            return count;
        }

        protected int expressionCountPerUniqueSlot
        {
            get
            {
                return FindOrComputeExpressionCountPerType(GetOperandType());
            }
        }

        protected VFXExpression[] ChainedBranchResult(VFXExpression[] compare, VFXExpression[] expressions, int[] valueStartIndex)
        {
            var expressionCountPerUniqueSlot = this.expressionCountPerUniqueSlot;
            var branchResult = new VFXExpression[expressionCountPerUniqueSlot];
            for (int subExpression = 0; subExpression < expressionCountPerUniqueSlot; ++subExpression)
            {
                var branch = new VFXExpression[valueStartIndex.Length];
                branch[^1] = expressions[valueStartIndex[^1] + subExpression]; //Last entry always is a fallback
                for (int i = valueStartIndex.Length - 2; i >= 0; i--)
                {
                    branch[i] = new VFXExpressionBranch(compare[i], expressions[valueStartIndex[i] + subExpression], branch[i + 1]);
                }
                branchResult[subExpression] = branch[0];
            }
            return branchResult;
        }
    }

    [VFXHelpURL("Operator-Branch")]
    [VFXInfo(category = "Logic", synonyms = new []{ "Boolean" })]
    class Branch : VFXOperatorDynamicBranch
    {
        public class InputProperties
        {
            [Tooltip("Sets the boolean whose state determines the branch output.")]
            public bool predicate = true;
            [Tooltip("Sets the value which will be returned if the predicate is true.")]
            public float True = 0.0f;
            [Tooltip("Sets the value which will be returned if the predicate is false.")]
            public float False = 1.0f;
        }

        public sealed override string name { get { return "Branch"; } }


        public sealed override IEnumerable<int> staticSlotIndex
        {
            get
            {
                yield return 0;
            }
        }

        protected override Type defaultValueType
        {
            get
            {
                return typeof(float);
            }
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var baseInputProperties = base.inputProperties;
                foreach (var property in baseInputProperties)
                {
                    if (property.property.name == "predicate")
                        yield return property;
                    else
                        yield return new VFXPropertyWithValue(new VFXProperty((Type)GetOperandType(), property.property.name), GetDefaultValueForType(GetOperandType()));
                }
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var valueStartIndex = new[] { 1, expressionCountPerUniqueSlot + 1 };
            return ChainedBranchResult(new ArraySegment<VFXExpression>(inputExpression, 0, 1).ToArray(), inputExpression, valueStartIndex);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    abstract class VFXOperatorDynamicBranch : VFXOperatorDynamicType
    {
        public override sealed IEnumerable<Type> validTypes
        {
            get
            {
                var exclude = new[] { typeof(GPUEvent) };
                return VFXLibrary.GetSlotsType().Except(exclude).Where(o => !o.IsSubclassOf(typeof(Texture)));
            }
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(m_Type, string.Empty));
            }
        }

        static Dictionary<Type, int> s_ExpressionCountPerType = new Dictionary<Type, int>();
        private static int FindOrComputeExpressionCountPerType(Type type)
        {
            int count = -1;
            if (!s_ExpressionCountPerType.TryGetValue(type, out count))
            {
                var tempInstance = VFXSlot.Create(new VFXPropertyWithValue(new VFXProperty(type, "temp")), VFXSlot.Direction.kInput);
                count = tempInstance.GetExpressionSlots().Count();
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
                branch[valueStartIndex.Length - 1] = expressions[valueStartIndex.Last() + subExpression]; //Last entry always is a fallback
                for (int i = valueStartIndex.Length - 2; i >= 0; i--)
                {
                    branch[i] = new VFXExpressionBranch(compare[i], expressions[valueStartIndex[i] + subExpression], branch[i + 1]);
                }
                branchResult[subExpression] = branch[0];
            }
            return branchResult;
        }
    }

    [VFXInfo(category = "Logic")]
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


        public override sealed IEnumerable<int> staticSlotIndex
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
            return ChainedBranchResult(inputExpression.Take(1).ToArray(), inputExpression, valueStartIndex);
        }
    }
}

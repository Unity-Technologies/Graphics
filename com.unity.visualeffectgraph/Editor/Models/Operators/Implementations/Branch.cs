using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    abstract class VFXOperatorDynamicBranch : VFXOperatorDynamicOperand, IVFXOperatorUniform
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        SerializableType m_Type;

        public Type GetOperandType()
        {
            return m_Type;
        }

        public void SetOperandType(Type type)
        {
            if (!validTypes.Contains(type))
                throw new InvalidOperationException();

            m_Type = type;
            Invalidate(InvalidationCause.kSettingChanged);
        }

        public override sealed IEnumerable<Type> validTypes
        {
            get
            {
                var exclude = new[] { typeof(FloatN), typeof(GPUEvent) };
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

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_Type == null) // Lazy init at this stage is suitable because inputProperties access is done with SyncSlot
                {
                    m_Type = defaultValueType;
                }
                return base.inputProperties;
            }
        }

        static Dictionary<Type, int> s_ExpressionCountPerType = new Dictionary<Type, int>();
        private static int FindOrComputeExpressionCountPerType(Type type)
        {
            int count = -1;
            if (!s_ExpressionCountPerType.TryGetValue(type, out count))
            {
                var tempInstance = VFXSlot.Create(new VFXPropertyWithValue(new VFXProperty(type, "temp")), VFXSlot.Direction.kInput);
                count = tempInstance.GetVFXValueTypeSlots().Count();
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

        private static int ComputeAbsoluteIndex(int entryIndex, int subExpressionIndex, int stride)
        {
            return entryIndex * stride + subExpressionIndex;
        }

        protected VFXExpression[] ChainedBranchResult(VFXExpression[] compare, VFXExpression[] expressions, int entryCount, int stride)
        {
            var expressionCountPerUniqueSlot = this.expressionCountPerUniqueSlot;
            var branchResult = new VFXExpression[expressionCountPerUniqueSlot];
            for (int subExpression = 0; subExpression < expressionCountPerUniqueSlot; ++subExpression)
            {
                var branch = new VFXExpression[entryCount];
                branch[entryCount - 1] = expressions[ComputeAbsoluteIndex(entryCount - 1, subExpression, stride)]; //Last entry always is a fallback
                for (int i = entryCount - 2; i >= 0; i--)
                {
                    branch[i] = new VFXExpressionBranch(compare[i], expressions[ComputeAbsoluteIndex(i, subExpression, stride)], branch[i + 1]);
                }
                branchResult[subExpression] = branch[0];
            }
            return branchResult;
        }

        abstract public IEnumerable<int> staticSlotIndex { get; }
    }

    [VFXInfo(category = "Logic")]
    class Branch : VFXOperatorDynamicBranch
    {
        public class InputProperties
        {
            [Tooltip("The predicate")]
            public bool predicate = true;
            [Tooltip("The true branch")]
            public float True = 0.0f;
            [Tooltip("The false branch")]
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
            return ChainedBranchResult(inputExpression.Take(1).ToArray(), inputExpression.Skip(1).ToArray(), 2, expressionCountPerUniqueSlot);
        }
    }
}

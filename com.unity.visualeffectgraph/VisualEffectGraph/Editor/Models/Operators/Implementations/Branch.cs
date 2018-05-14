using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    class BranchNewTypeProvider : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "m_Type", validTypes.Select(o => new SerializableType(o)).ToArray() }
                };
            }
        }

        static public IEnumerable<Type> validTypes
        {
            get
            {
                var exclude = new[] { typeof(FloatN), typeof(GPUEvent) };
                return VFXLibrary.GetSlotsType().Except(exclude).Where(o => !o.IsSubclassOf(typeof(Texture)));
            }
        }
    }

    [VFXInfo(category = "Logic")]
    class Branch : VFXOperatorDynamicOperand, IVFXOperatorUniform
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        SerializableType m_Type;

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

        public IEnumerable<int> staticSlotIndex
        {
            get
            {
                yield return 0;
            }
        }

        public override IEnumerable<Type> validTypes
        {
            get
            {
                return BranchNewTypeProvider.validTypes;
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
                if (m_Type == null) // Lazy init at this stage is suitable because inputProperties access is done with SyncSlot
                {
                    m_Type = defaultValueType;
                }

                foreach (var property in baseInputProperties)
                {
                    if (property.property.name == "predicate")
                        yield return property;
                    else
                        yield return new VFXPropertyWithValue(new VFXProperty((Type)m_Type, property.property.name), GetDefaultValueForType(m_Type));
                }
            }
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(m_Type, string.Empty));
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var nbExpressionPerSlot = (inputExpression.Length - 1) / 2;

            var branches = inputExpression.Skip(1);
            var trueList = branches.Take(nbExpressionPerSlot);
            var falseList = branches.Skip(nbExpressionPerSlot).Take(nbExpressionPerSlot);

            var pred = inputExpression[0];

            var itTrue = trueList.GetEnumerator();
            var itFalse = falseList.GetEnumerator();
            var result = new List<VFXExpression>(nbExpressionPerSlot);
            while (itTrue.MoveNext() && itFalse.MoveNext())
            {
                result.Add(new VFXExpressionBranch(pred, itTrue.Current, itFalse.Current));
            }

            return result.ToArray();
        }
    }
}

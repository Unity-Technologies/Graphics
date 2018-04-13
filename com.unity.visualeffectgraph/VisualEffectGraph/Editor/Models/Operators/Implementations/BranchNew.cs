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
                return VFXLibrary.GetSlotsType().Where(o => !o.IsSubclassOf(typeof(Texture)));
            }
        }
    }

    [VFXInfo(category = "Math", variantProvider = typeof(BranchNewTypeProvider), experimental = true)] //This provider is only a test waiting a real interface
    class BranchNew : VFXOperatorDynamicOperand, IVFXOperatorUniform
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        SerializableType m_Type;

        public class InputProperties
        {
            [Tooltip("The predicate")]
            public bool predicate = true;
            [Tooltip("The true branch")]
            public Sphere True = Sphere.defaultValue;
            [Tooltip("The false branch")]
            public Sphere False = Sphere.defaultValue;
        }

        public sealed override string name { get { return "BranchNew " + ((Type)m_Type).UserFriendlyName(); } }

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
                return typeof(Sphere);
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
                const string outputName = "o";
                yield return new VFXPropertyWithValue(new VFXProperty(m_Type, outputName));
            }
        }

        public sealed override void UpdateOutputExpressions()
        {
            var TrueList = new List<VFXExpression>();
            var FalseList = new List<VFXExpression>();

            GetInputExpressionsRecursive(TrueList, Enumerable.Repeat(inputSlots[1], 1));
            GetInputExpressionsRecursive(FalseList, Enumerable.Repeat(inputSlots[2], 1));

            var result = new List<VFXExpression>();
            var itTrue = TrueList.GetEnumerator();
            var itFalse = FalseList.GetEnumerator();

            var pred = inputSlots[0].GetExpression();

            while (itTrue.MoveNext() && itFalse.MoveNext())
            {
                result.Add(new VFXExpressionBranch(pred, itTrue.Current, itFalse.Current));
            }

            SetOutputExpressions(result.ToArray());
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            throw new NotImplementedException();
        }
    }
}

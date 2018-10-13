using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class Switch : VFXOperatorDynamicBranch
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default), SerializeField]
        uint m_EntryCount = 2u;

        public class TestInputProperties
        {
            [Tooltip("An optional additional hash.")]
            public int testInput = 0;
        }

        public class ManualRandom
        {
            [Tooltip("Random Value")]
            public float rand = 0.0f;
        }

        public sealed override string name { get { return "Switch"; } }

        public override sealed IEnumerable<int> staticSlotIndex
        {
            get
            {
                var stride = expressionCountPerUniqueSlot + 1;
                for (int i = 0; i < m_EntryCount; ++i)
                    for (int j = 0; j < stride; ++j)
                        yield return i * stride + j;
                yield return stride * (int)m_EntryCount + expressionCountPerUniqueSlot /* default value */;
            }
        }

        protected override Type defaultValueType
        {
            get
            {
                return typeof(Color);
            }
        }


        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            if (m_EntryCount < 1) m_EntryCount = 1;
            if (m_EntryCount > 32) m_EntryCount = 32;
            base.Invalidate(model, cause);
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var baseInputProperties = base.inputProperties;
                var defaultValue = GetDefaultValueForType(GetOperandType());
                for (uint i = 0; i < m_EntryCount + 1; ++i)
                {
                    var name = (i == m_EntryCount) ? "default" : "V" + i;
                    yield return new VFXPropertyWithValue(new VFXProperty((Type)GetOperandType(), name), defaultValue);
                    if (i != m_EntryCount)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(int), "Case " + i), (int)i);
                }

                var manualRandomProperties = PropertiesFromType("TestInputProperties");
                foreach (var property in manualRandomProperties)
                    yield return property;
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var referenceValue = inputExpression.Last();
            referenceValue = new VFXExpressionCastIntToFloat(referenceValue);

            var expressionCountPerUniqueSlot = this.expressionCountPerUniqueSlot;

            var stride = expressionCountPerUniqueSlot + 1;
            var compare = new VFXExpression[m_EntryCount];
            int offsetCase = expressionCountPerUniqueSlot;
            for (uint i = 0; i < m_EntryCount; i++)
            {
                offsetCase += stride;
                compare[i] = new VFXExpressionCondition(VFXCondition.Equal, referenceValue, new VFXExpressionCastIntToFloat(inputExpression[offsetCase]));
            }

            return ChainedBranchResult(compare, inputExpression, ((int)m_EntryCount + 1), stride);
        }
    }
}

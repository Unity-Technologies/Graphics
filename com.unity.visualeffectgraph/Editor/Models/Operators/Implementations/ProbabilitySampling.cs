using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class ProbabilitySampling : VFXOperatorDynamicBranch
    {
        [VFXSetting, Tooltip("Generate a random number for each particle, or one that is shared by the whole system.")]
        public Random.SeedMode seed = Random.SeedMode.PerParticle;

        [VFXSetting, Tooltip("The random number may either remain constant, or change every time it is evaluated.")]
        public bool constant = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default), SerializeField]
        uint m_EntryCount = 3u;

        public class ConstantInputProperties
        {
            [Tooltip("An optional additional hash.")]
            public uint hash = 0u;
        }

        public sealed override string name { get { return "Probability Sampling"; } }

        public override sealed IEnumerable<int> staticSlotIndex
        {
            get
            {
                for (uint i = 0; i < m_EntryCount; ++i)
                    yield return (int)(i + m_EntryCount);
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
            if (m_EntryCount < 2) m_EntryCount = 2;
            if (m_EntryCount > 32) m_EntryCount = 32;
            base.Invalidate(model, cause);
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var baseInputProperties = base.inputProperties;
                var defaultValue = GetDefaultValueForType(GetOperandType());
                for (uint i = 0; i < m_EntryCount; ++i)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty((Type)GetOperandType(), "V" + i), defaultValue);
                }

                for (uint i = 0; i < m_EntryCount; ++i)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "P" + i), 0.0f);
                }

                if (constant)
                {
                    var constantProperties = PropertiesFromType("ConstantInputProperties");
                    foreach (var property in constantProperties)
                        yield return property;
                }
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression rand = null;
            if (constant)
                rand = VFXOperatorUtility.FixedRandom(inputExpression.Last(), seed == Random.SeedMode.PerParticle);
            else
                rand = new VFXExpressionRandom(seed == Random.SeedMode.PerParticle);

            var prefixedProbablities = new VFXExpression[m_EntryCount];
            prefixedProbablities[0] = inputExpression[m_EntryCount];
            for (uint i = 1; i < m_EntryCount; i++)
            {
                prefixedProbablities[i] = prefixedProbablities[i - 1] + inputExpression[i + m_EntryCount];
            }
            rand = rand * prefixedProbablities.Last();

            var compare = new VFXExpression[m_EntryCount - 1];
            for (int i = 0; i < m_EntryCount - 1; i++)
            {
                compare[i] = new VFXExpressionCondition(VFXCondition.GreaterOrEqual, prefixedProbablities[i], rand);
            };

            var branch = new VFXExpression[m_EntryCount];
            branch[m_EntryCount - 1] = inputExpression[m_EntryCount - 1]; //Last entry is the fallback
            for (int i = (int)m_EntryCount - 2; i >= 0; i--)
            {
                branch[i] = new VFXExpressionBranch(compare[i], inputExpression[i], branch[i + 1]);
            }

            return new[] { branch[0] };
        }
    }
}

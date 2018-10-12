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
        [VFXSetting, Tooltip("Use integrated random function"), SerializeField]
        private bool m_IntegratedRandom = true;

        [VFXSetting, Tooltip("Generate a random number for each particle, or one that is shared by the whole system."), SerializeField]
        public Random.SeedMode m_Seed = Random.SeedMode.PerParticle;

        [VFXSetting, Tooltip("The random number may either remain constant, or change every time it is evaluated."), SerializeField]
        public bool m_Constant = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default), SerializeField]
        uint m_EntryCount = 3u;

        public class ConstantInputProperties
        {
            [Tooltip("An optional additional hash.")]
            public uint hash = 0u;
        }

        public class ManualRandom
        {
            [Tooltip("Random Value")]
            public float rand = 0.0f;
        }

        public sealed override string name { get { return "Probability Sampling"; } }

        private int expressionCountPerUniqueSlot
        {
            get
            {
                /* TODO, find better method, without huge garbage */
                var tempInstance = VFXSlot.Create(new VFXPropertyWithValue(new VFXProperty(GetOperandType(), "temp")), VFXSlot.Direction.kInput);
                var r = tempInstance.GetVFXValueTypeSlots().Count();
                ScriptableObject.DestroyImmediate(tempInstance);
                return r;
            }
        }

        public override sealed IEnumerable<int> staticSlotIndex
        {
            get
            {
                var expressionCountPerSlot = expressionCountPerUniqueSlot + 1;
                for (int i = 0; i < m_EntryCount; ++i)
                    for (int j = 0; j < expressionCountPerSlot; ++j)
                        yield return i * expressionCountPerSlot + j;

                if (m_Constant || !m_IntegratedRandom)
                    yield return expressionCountPerSlot * (int)m_EntryCount;
            }
        }

        protected override Type defaultValueType
        {
            get
            {
                return typeof(Color);
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!m_IntegratedRandom)
                {
                    yield return "m_Seed";
                    yield return "m_Constant";
                }
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
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "P" + i), 0.0f);
                }

                if (m_IntegratedRandom)
                {
                    if (m_Constant)
                    {
                        var constantProperties = PropertiesFromType("ConstantInputProperties");
                        foreach (var property in constantProperties)
                            yield return property;
                    }
                }
                else
                {
                    var manualRandomProperties = PropertiesFromType("ManualRandom");
                    foreach (var property in manualRandomProperties)
                        yield return property;
                }
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression rand = null;
            if (m_IntegratedRandom)
            {
                if (m_Constant)
                    rand = VFXOperatorUtility.FixedRandom(inputExpression.Last(), m_Seed == Random.SeedMode.PerParticle);
                else
                    rand = new VFXExpressionRandom(m_Seed == Random.SeedMode.PerParticle);
            }
            else
            {
                rand = inputExpression.Last();
            }

            var expressionCountPerSlot = expressionCountPerUniqueSlot;
            var probabilitiesExpression = new VFXExpression[m_EntryCount]; //TODO : avoid useless copy -_-'
            for (int i = 0; i < m_EntryCount; ++i)
            {
                probabilitiesExpression[i] = inputExpression[i * (expressionCountPerSlot + 1) + expressionCountPerSlot];
            }

            var prefixedProbablities = new VFXExpression[m_EntryCount];
            prefixedProbablities[0] = probabilitiesExpression[0];
            for (uint i = 1; i < m_EntryCount; i++)
            {
                prefixedProbablities[i] = prefixedProbablities[i - 1] + probabilitiesExpression[i];
            }
            rand = rand * prefixedProbablities.Last();

            var compare = new VFXExpression[m_EntryCount - 1];
            for (int i = 0; i < m_EntryCount - 1; i++)
            {
                compare[i] = new VFXExpressionCondition(VFXCondition.GreaterOrEqual, prefixedProbablities[i], rand);
            };

            var branchResult = new VFXExpression[expressionCountPerSlot];

            Func<int, int, int> fnActualExpressionIndex = delegate (int index, int subExpression)
            {
                return index * (expressionCountPerSlot + 1) + subExpression;
            };

            for (int subExpression = 0; subExpression < expressionCountPerSlot; ++subExpression)
            {
                var branch = new VFXExpression[m_EntryCount];
                branch[m_EntryCount - 1] = inputExpression[fnActualExpressionIndex((int)m_EntryCount - 1, subExpression)]; //Last entry is the fallback
                for (int i = (int)m_EntryCount - 2; i >= 0; i--)
                {
                    branch[i] = new VFXExpressionBranch(compare[i], inputExpression[fnActualExpressionIndex(i, subExpression)], branch[i + 1]);
                }
                branchResult[subExpression] = branch[0];
            }

            return branchResult;
        }
    }
}

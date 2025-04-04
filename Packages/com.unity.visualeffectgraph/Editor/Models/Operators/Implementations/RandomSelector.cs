using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    sealed class RandomSelectorProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Random Selector",
                "Random",
                typeof(RandomSelector),
                new[]
                {
                    new KeyValuePair<string, object>("m_Weighted", false),
                    new KeyValuePair<string, object>("m_ClampWeight", true)
                });

            yield return new Variant(
                "Random Selector Weighted",
                "Random",
                typeof(RandomSelector),
                new[]
                {
                    new KeyValuePair<string, object>("m_Weighted", true),
                    new KeyValuePair<string, object>("m_ClampWeight", true)
                });
        }
    }

    [VFXHelpURL("Operator-RandomSelectorWeighted")]
    [VFXInfo(category = "Random", synonyms = new [] { "probability", "sampling" }, variantProvider = typeof(RandomSelectorProvider))]
    class RandomSelector : VFXOperatorDynamicBranch
    {
        enum Mode
        {
            Random,
            Custom
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField, FormerlySerializedAs("m_IntegratedRandom")]
        private bool m_IntegratedRandomDeprecated = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private Mode m_Mode = Mode.Random;

        [VFXSetting, Tooltip("Generate a random number for each particle, or one that is shared by the whole system."), SerializeField]
        private VFXSeedMode m_Seed = VFXSeedMode.PerParticle;

        [VFXSetting, Tooltip("The random number may either remain constant, or change every time it is evaluated."), SerializeField]
        private bool m_Constant = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip(" When enabled, it reveals the weights of values, influencing their likelihood of being randomly selected."), SerializeField]
        private bool m_Weighted = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, clamps weights to a defined 0-1 range."), SerializeField]
        private bool m_ClampWeight = false;

        [VFXSetting, SerializeField, Tooltip("Set the number of possible entries to be sampled. The maximum count is 32 entries.")]
        private uint m_EntryCount = 3u;

        public class ConstantInputProperties
        {
            [Tooltip("The seed this operator uses to create a constant random value.")]
            public uint seed = 0u;
        }

        public class ManualRandomProperties
        {
            [Tooltip("Value used to sample the weighted curve on a 0 to 1 range.")]
            public float s = 0.0f;
        }

        public override IEnumerable<Type> validTypes
        {
            get
            {
                foreach (var validType in base.validTypes)
                {
                    bool isPerParticleRandom = false;
                    if (m_Mode == Mode.Random)
                    {
                        isPerParticleRandom = m_Seed != VFXSeedMode.PerVFXComponent;
                    }

                    //Avoid listing of mesh or texture base switch in case of random per particle
                    if (isPerParticleRandom &&
                        (validType.IsSubclassOf(typeof(Texture))
                         || validType == typeof(Mesh)
                         || validType == typeof(SkinnedMeshRenderer)
                         || validType == typeof(GraphicsBuffer)))
                        continue;
                    yield return validType;
                }
            }
        }

        public override string name
        {
            get
            {
                var current = string.Empty;
                if (m_Mode == Mode.Random)
                    current += "Random ";
                current += "Selector";
                if (m_Weighted)
                    current += " Weighted";
                return current;
            }
        }

        public override IEnumerable<int> staticSlotIndex
        {
            get
            {
                var stride = ComputeStride();
                for (int i = 0; i < m_EntryCount; ++i)
                    yield return i * stride + 1;

                if (m_Constant || m_Mode == Mode.Custom)
                    yield return stride * (int)m_EntryCount;
            }
        }

        protected override Type defaultValueType => typeof(float);

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (m_Mode == Mode.Custom)
                {
                    yield return nameof(m_Seed);
                    yield return nameof(m_Constant);
                }

                if (!m_Weighted)
                {
                    yield return nameof(m_ClampWeight);
                }
            }
        }

        public sealed override void Sanitize(int version)
        {
            if (version < 18)
                m_Mode = m_IntegratedRandomDeprecated ? Mode.Random : Mode.Custom;

            base.Sanitize(version);
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (m_EntryCount < 2) m_EntryCount = 2;
            if (m_EntryCount > 32) m_EntryCount = 32;
            base.OnInvalidate(model, cause);
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var type = (Type)GetOperandType();
                var defaultValue = GetDefaultValueForType(type);

                for (uint i = 0; i < m_EntryCount; ++i)
                {
                    var prefix = i.ToString();

                    var currentValue = defaultValue;
                    if (type == typeof(float))
                    {
                        //Handle default values from variant provider, we are displaying different entries for each slot.
                        currentValue = (float)i;
                    }

                    yield return new VFXPropertyWithValue(new VFXProperty((Type)GetOperandType(), $"Value {prefix}"), currentValue);
                    if (m_Weighted)
                    {
                        if (m_ClampWeight)
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), $"Weight {prefix}", new RangeAttribute(0.0f, 1.0f)), 1.0f);
                        else
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), $"Weight {prefix}"), 1.0f);
                    }
                }

                if (m_Mode == Mode.Random)
                {
                    if (m_Constant)
                    {
                        var constantProperties = PropertiesFromType(nameof(ConstantInputProperties));
                        foreach (var property in constantProperties)
                            yield return property;
                    }
                }
                else
                {
                    var manualRandomProperties = PropertiesFromType(nameof(ManualRandomProperties));
                    foreach (var property in manualRandomProperties)
                        yield return property;
                }
            }
        }

        int ComputeStride()
        {
            if (m_Weighted)
                return expressionCountPerUniqueSlot + 1;
            return expressionCountPerUniqueSlot;
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression rand = null;
            if (m_Mode == Mode.Random)
            {
                if (m_Constant)
                    rand = VFXOperatorUtility.FixedRandom(inputExpression[^1], m_Seed);
                else
                    rand = new VFXExpressionRandom(m_Seed == VFXSeedMode.PerParticle, new RandId(this));
            }
            else
            {
                rand = inputExpression[^1];
            }

            int stride = ComputeStride();

            var prefixedProbabilities = new VFXExpression[m_EntryCount];
            if (m_Weighted)
            {
                var expressionCountPerUniqueSlot = this.expressionCountPerUniqueSlot;
                int offsetProbabilities = expressionCountPerUniqueSlot;
                prefixedProbabilities[0] = inputExpression[offsetProbabilities];
                for (uint i = 1; i < m_EntryCount; i++)
                {
                    offsetProbabilities += stride;
                    prefixedProbabilities[i] = prefixedProbabilities[i - 1] + inputExpression[offsetProbabilities];
                }
            }
            else
            {
                for (uint i = 0; i < m_EntryCount; i++)
                {
                    prefixedProbabilities[i] = VFXValue.Constant(i + 1.0f);
                }
            }

            rand = rand * prefixedProbabilities[^1];
            var compare = new VFXExpression[m_EntryCount - 1];
            for (int i = 0; i < m_EntryCount - 1; i++)
            {
                compare[i] = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.GreaterOrEqual, prefixedProbabilities[i], rand);
            }

            var startValueIndex = new int[m_EntryCount];
            for (int i = 0; i < m_EntryCount; ++i)
            {
                startValueIndex[i] = i * stride;
            }

            return ChainedBranchResult(compare, inputExpression, startValueIndex);
        }
    }
}

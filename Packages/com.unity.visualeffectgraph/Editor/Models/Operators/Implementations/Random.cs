using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    enum VFXSeedMode
    {
        PerParticle,
        PerVFXComponent,
        PerParticleStrip,
    }
}

namespace UnityEditor.VFX.Operator
{
    class RandomProvider : VariantProvider
    {
        readonly Type[] m_SupportedType;
        bool isSubvariant;

        public RandomProvider() : this(false) { }

        public RandomProvider(bool subVariant)
        {
            isSubvariant = subVariant;
            if (isSubvariant)
            {
                m_SupportedType = new[]
                {
                    typeof(Vector2),
                    typeof(Vector4),
                    typeof(Color),
                    typeof(uint),
                    typeof(bool)
                };
            }
            else
            {
                m_SupportedType = new[]
                {
                    typeof(float),
                    typeof(Vector3),
                    typeof(int)
                };
            }
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (var type in m_SupportedType)
            {
                yield return new Variant(
                    "Random".AppendLabel(type.UserFriendlyName()),
                    isSubvariant ? null : "Random",
                    typeof(Random),
                    new[]
                    {
                        new KeyValuePair<string, object>("m_Type", (SerializableType)type)
                    },
                    type == typeof(float) ? () => new RandomProvider(true) : null);
            }
        }
    }

    [VFXHelpURL("Operator-RandomNumber")]
    [VFXInfo(synonyms = new[] { "Probability", "Aleatory" }, variantProvider = typeof(RandomProvider))]
    sealed class Random : VFXOperatorDynamicType
    {
        public static readonly Type[] kSupportedTypes = new[]
        {
            typeof(float),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Color),
            typeof(uint),
            typeof(int),
            typeof(bool)
        };

        [VFXSetting, Tooltip("Specifies whether the random number is generated for each particle, each particle strip, or is shared by the whole system.")]
        public VFXSeedMode seed = VFXSeedMode.PerParticle;
        [VFXSetting, Tooltip("When enabled, the random number will remain constant. Otherwise, it will change every time it is evaluated.")]
        public bool constant = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, you can customize Seed per channel, otherwise Seed is randomly generated for each channel.")]
        public bool independentSeed = false;

        public override string name => $"Random".AppendLabel(((Type)m_Type).UserFriendlyName());

        public override IEnumerable<Type> validTypes => kSupportedTypes;

        protected override Type defaultValueType => typeof(float);

        public override IEnumerable<int> staticSlotIndex
        {
            get
            {
                yield return 2;
                yield return 3;
                yield return 4;
                yield return 5;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var type = (Type)m_Type;
                object min, max;
                if (type == typeof(float))
                {
                    min = 0.0f;
                    max = 1.0f;
                }
                else if (type == typeof(Vector2))
                {
                    min = Vector2.zero;
                    max = Vector2.one;
                }
                else if (type == typeof(Vector3))
                {
                    min = Vector3.zero;
                    max = Vector3.one;
                }
                else if (type == typeof(Vector4))
                {
                    min = Vector4.zero;
                    max = Vector4.one;
                }
                else if (type == typeof(Color))
                {
                    min = Color.clear;
                    max = Color.white;
                }
                else if (type == typeof(uint))
                {
                    min = 0u;
                    max = 6u;
                }
                else if (type == typeof(int))
                {
                    min = 0;
                    max = 6;
                }
                else if (type == typeof(bool))
                {
                    min = max = null;
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (min != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(type, "Min", new TooltipAttribute("Sets the minimum range of the random value.")), min);

                if (max != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(type, "Max", new TooltipAttribute("Sets the maximum range of the random value.")), max);

                if (constant || seed == VFXSeedMode.PerParticleStrip)
                {
                    var randomCount = VFXExpression.TypeToSize(VFXExpression.GetVFXValueTypeFromType(m_Type));
                    if (!independentSeed || randomCount == 1)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "seed", new TooltipAttribute("Specifies a seed that the Operator uses to generate random values.")), 0u);
                    else
                    {
                        var channelName = new[] { 'X', 'Y', 'Z', 'W' };
                        for (uint randIndex = 0u; randIndex < randomCount; ++randIndex)
                        {
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "seed" + channelName[randIndex], new TooltipAttribute("Specifies a seed that the Operator uses to generate random values.")), randIndex);
                        }
                    }
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(m_Type, "r", new TooltipAttribute("Outputs a random number between the min and max range.")));
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var s in base.filteredOutSettings)
                    yield return s;

                var valueType = VFXExpression.GetVFXValueTypeFromType(m_Type);
                var randomCount = VFXExpression.TypeToSize(valueType);

                if (seed == VFXSeedMode.PerParticleStrip)
                    yield return nameof(constant);
                else if (!constant || randomCount == 1)
                    yield return nameof(independentSeed);
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var valueType = VFXExpression.GetVFXValueTypeFromType(m_Type);
            var randomCount = VFXExpression.TypeToSize(valueType);

            var rands = new VFXExpression[randomCount];

            var startSeedIndex = valueType == VFXValueType.Boolean ? 0 : 2;
            var currentSeed = inputExpression.Length > startSeedIndex ? inputExpression[startSeedIndex] : null;

            for (int randIndex = 0; randIndex < randomCount; ++randIndex)
            {
                rands[randIndex] = VFXOperatorUtility.BuildRandom(seed, constant, new RandId(this, randIndex), currentSeed);

                if (currentSeed != null && randIndex != randomCount - 1)
                {
                    startSeedIndex++;
                    if (inputExpression.Length > startSeedIndex)
                    {
                        currentSeed = inputExpression[startSeedIndex];
                    }
                    else
                    {
                        //Fallback for single seed mode
                        currentSeed = new VFXExpressionMul(currentSeed, VFXValue.Constant<uint>(214013));
                        currentSeed = new VFXExpressionAdd(currentSeed, VFXValue.Constant<uint>(2531011));
                    }
                }
            }

            VFXExpression rand;
            if (randomCount == 1)
            {
                rand = rands[0];
            }
            else
            {
                rand = new VFXExpressionCombine(rands);
            }

            if (valueType == VFXValueType.Boolean)
            {
                VFXExpression cond = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Greater, rand, VFXOperatorUtility.HalfExpression[VFXValueType.Float]);
                return new[] { cond };
            }

            var min = inputExpression[0];
            var max = inputExpression[1];

            if (min.valueType != max.valueType)
                throw new InvalidOperationException();

            if (valueType == VFXValueType.Uint32)
            {
                var range = max - min;
                range = new VFXExpressionCastUintToFloat(range);
                var randUint = new VFXExpressionCastFloatToUint(range * rand);
                return new[] { min + randUint };
            }

            if (valueType == VFXValueType.Int32)
            {
                var range = max - min;
                range = new VFXExpressionCastIntToFloat(range);
                var randInt = new VFXExpressionCastFloatToInt(range * rand);
                return new[] { min + randInt };
            }

            if (VFXExpression.IsFloatValueType(valueType))
            {
                return new[] { VFXOperatorUtility.Lerp(min, max, rand) };
            }

            throw new NotImplementedException();
        }
    }
}

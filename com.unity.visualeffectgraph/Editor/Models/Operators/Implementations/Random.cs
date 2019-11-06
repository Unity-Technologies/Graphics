using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    public enum VFXSeedMode
    {
        PerParticle,
        PerComponent,
        PerParticleStrip,
    }
}

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Random")]
    class Random : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the minimum range of the random value.")]
            public float min = 0.0f;
            [Tooltip("Sets the maximum range of the random value.")]
            public float max = 1.0f;
        }

        public class ConstantInputProperties
        {
            [Tooltip("Sets the value used when determining the random number. Using the same seed results in the same random number every time.")]
            public uint seed = 0u;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs a random number between the min and max range.")]
            public float r;
        }

        [VFXSetting, Tooltip("Specifies whether the random number is generated for each particle, each particle strip, or is shared by the whole system.")]
        public VFXSeedMode seed = VFXSeedMode.PerParticle;
        [VFXSetting, Tooltip("When enabled, the random number will remain constant. Otherwise, it will change every time it is evaluated.")]
        public bool constant = true;

        override public string name { get { return "Random Number"; } }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var props = PropertiesFromType("InputProperties");
                if (constant || seed == VFXSeedMode.PerParticleStrip)
                    props = props.Concat(PropertiesFromType("ConstantInputProperties"));
                return props;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var s in base.filteredOutSettings)
                    yield return s;
                if (seed == VFXSeedMode.PerParticleStrip)
                    yield return "constant";
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression rand = null;
            if (seed == VFXSeedMode.PerParticleStrip || constant)
                rand = VFXOperatorUtility.FixedRandom(inputExpression[2], seed);
            else
                rand = new VFXExpressionRandom(seed == VFXSeedMode.PerParticle);

            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], rand) };
        }
    }
}

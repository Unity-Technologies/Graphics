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
            [Tooltip("The minimum value to be generated.")]
            public float min = 0.0f;
            [Tooltip("The maximum value to be generated.")]
            public float max = 1.0f;
        }

        public class ConstantInputProperties
        {
            [Tooltip("Seed to compute the constant random")]
            public uint seed = 0u;
        }

        public class OutputProperties
        {
            [Tooltip("A random number between 0 and 1.")]
            public float r;
        }

        [VFXSetting, Tooltip("Generate a random number for each particle, particle strip, or one that is shared by the whole system.")]
        public VFXSeedMode seed = VFXSeedMode.PerParticle;
        [VFXSetting, Tooltip("The random number may either remain constant, or change every time it is evaluated.")]
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

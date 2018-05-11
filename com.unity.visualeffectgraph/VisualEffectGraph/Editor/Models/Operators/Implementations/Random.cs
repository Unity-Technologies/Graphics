using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Random")]
    class Random : VFXOperator
    {
        public enum SeedMode
        {
            PerParticle,
            PerComponent,
        }

        public class InputProperties
        {
            [Tooltip("The minimum value to be generated.")]
            public float min = 0.0f;
            [Tooltip("The maximum value to be generated.")]
            public float max = 1.0f;
        }

        public class ConstantInputProperties
        {
            [Tooltip("An optional additional hash.")]
            public uint hash = 0u;
        }

        public class OutputProperties
        {
            [Tooltip("A random number between 0 and 1.")]
            public float r;
        }

        [VFXSetting, Tooltip("Generate a random number for each particle, or one that is shared by the whole system.")]
        public SeedMode seed = SeedMode.PerParticle;
        [VFXSetting, Tooltip("The random number may either remain constant, or change every time it is evaluated.")]
        public bool constant = true;

        override public string name { get { return "Random Number"; } }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var props = PropertiesFromType("InputProperties");
                if (constant)
                    props = props.Concat(PropertiesFromType("ConstantInputProperties"));
                return props;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression rand = null;
            if (constant)
                rand = VFXOperatorUtility.FixedRandom(inputExpression[2], seed == SeedMode.PerParticle);
            else
                rand = new VFXExpressionRandom(seed == SeedMode.PerParticle);

            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], rand) };
        }

        public sealed override void Sanitize()
        {
            //This operator was based on FloatN for min/max
            float[] valueToRestore = null;
            if (inputSlots[0].property.type == typeof(FloatN) && inputSlots[1].property.type == typeof(FloatN))
            {
                valueToRestore = new float[2];
                for (int i = 0; i < 2; i++)
                {
                    valueToRestore[i] = ((FloatN)inputSlots[i].value);
                }
            }

            base.Sanitize(); //if FloatN, value are reseted with ResyncSlot

            if (valueToRestore != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    inputSlots[i].value = valueToRestore[i];
                }
                Debug.Log(string.Format("Random Operator Sanitize has restored min/max value : {0}, {1}", valueToRestore[0], valueToRestore[1]));
            }
        }
    }
}

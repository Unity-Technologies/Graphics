using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    //[VFXHelpURL("Operator-PerParticleTotalTime")]
    [VFXInfo(category = "Time")]
    class PerParticleTotalTime : VFXOperator
    {
        public class OutputProperties
        {
            [Tooltip("Outputs the delta time with a random per-particle offset. This is useful for preventing particles in fast simulations from receiving similar attributes when they share the same delta time.")]
            public float t = 0;
        }

        public override string name
        {
            get
            {
                return "Total Time (Per-Particle)";
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[]
            {
                VFXBuiltInExpression.TotalTime + (VFXBuiltInExpression.DeltaTime * VFXOperatorUtility.FixedRandom(0xc43388e9, VFXSeedMode.PerParticle)),
            };
            return output;
        }
    }
}

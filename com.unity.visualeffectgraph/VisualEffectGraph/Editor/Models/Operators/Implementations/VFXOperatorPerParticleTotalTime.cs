using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Time")]
    class VFXOperatorPerParticleTotalTime : VFXOperator
    {
        public override string name
        {
            get
            {
                return "Total Time (Per-Particle)";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] {
                VFXBuiltInExpression.TotalTime + (VFXBuiltInExpression.DeltaTime * new VFXAttributeExpression(VFXAttribute.Phase)),
            };
            return output;
        }
    }
}

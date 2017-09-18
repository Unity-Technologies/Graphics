using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorConeVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The cone used for the volume calculation.")]
            public Cone cone = new Cone();
        }

        override public string name { get { return "Volume (Cone)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.ConeVolume(inputExpression[1], inputExpression[2], inputExpression[3]) };
        }
    }
}

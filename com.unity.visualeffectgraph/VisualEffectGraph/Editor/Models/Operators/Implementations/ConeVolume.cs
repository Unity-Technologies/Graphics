using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Geometry")]
    class ConeVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The cone used for the volume calculation.")]
            public Cone cone = new Cone();
        }

        public class OutputProperties
        {
            [Tooltip("The volume of the cone.")]
            public float volume;
        }

        override public string name { get { return "Volume (Cone)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.ConeVolume(inputExpression[1], inputExpression[2], inputExpression[3]) };
        }
    }
}

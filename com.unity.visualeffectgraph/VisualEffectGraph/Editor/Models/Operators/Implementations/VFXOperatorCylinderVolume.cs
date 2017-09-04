using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorCylinderVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The cylinder used for the volume calculation.")]
            public Cylinder cylinder = new Cylinder();
        }

        override public string name { get { return "Volume (Cylinder)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.CylinderVolume(inputExpression[1], inputExpression[2]) };
        }
    }
}

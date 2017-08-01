using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorOrientedBoxVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The box used for the volume calculation.")]
            public OrientedBox box = new OrientedBox();
        }

        override public string name { get { return "Volume (Oriented Box)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.BoxVolume(inputExpression[2]) };
        }
    }
}

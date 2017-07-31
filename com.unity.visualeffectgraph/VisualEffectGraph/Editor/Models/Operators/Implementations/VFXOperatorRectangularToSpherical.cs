using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Utility")]
    class VFXOperatorRectangularToSpherical : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The 3D coordinate to be converted into Spherical space.")]
            public Vector3 coordinate = Vector3.zero;
        }

        override public string name { get { return "Rectangular to Spherical"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var results = VFXOperatorUtility.RectangularToSpherical(inputExpression[0]);
            results[0] = VFXOperatorUtility.RadToDeg(results[0]);
            results[1] = VFXOperatorUtility.RadToDeg(results[1]);
            return results;
        }
    }
}

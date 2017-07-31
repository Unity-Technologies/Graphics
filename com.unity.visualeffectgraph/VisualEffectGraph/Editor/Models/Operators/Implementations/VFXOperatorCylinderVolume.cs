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
			[Tooltip("The radius of the cylinder.")]
			public float radius = 1.0f;
			[Tooltip("The height of the cylinder.")]
			public float height = 1.0f;
        }

        override public string name { get { return "Volume (Sphere)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
			return new VFXExpression[] { VFXOperatorUtility.CylinderVolume(inputExpression[0], inputExpression[1]) };
        }
    }
}

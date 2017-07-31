using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Geometry")]
	class VFXOperatorSphereVolume : VFXOperator
    {
        public class InputProperties
        {
			[Tooltip("The size of the sphere.")]
			public float radius = 1.0f;
        }

        override public string name { get { return "Volume (Sphere)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
			return new VFXExpression[] { VFXOperatorUtility.SphereVolume(inputExpression[0]) };
        }
    }
}

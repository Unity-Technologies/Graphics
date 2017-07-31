using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Geometry")]
	class VFXOperatorBoxVolume : VFXOperator
    {
        public class InputProperties
        {
			[Tooltip("The size of the box.")]
			public Vector3 dimensions = Vector3.one;
        }

        override public string name { get { return "Volume (Box)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
			return new VFXExpression[] { VFXOperatorUtility.BoxVolume(inputExpression[0]) };
        }
    }
}

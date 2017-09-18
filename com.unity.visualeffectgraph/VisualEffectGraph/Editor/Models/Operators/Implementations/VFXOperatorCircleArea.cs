using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorCircleArea : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The circle used for the area calculation.")]
            public Circle circle = new Circle();
        }

        override public string name { get { return "Area (Circle)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.CircleArea(inputExpression[1]) };
        }
    }
}

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    //TODOPAUL : Sanitize this
    class CircleAreaDeprecated : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the circle used for the area calculation.")]
            public Circle circle = new Circle();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the area of the circle.")]
            public float area;
        }

        override public string name { get { return "Area (Circle) (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.CircleArea(inputExpression[1]) };
        }
    }
}

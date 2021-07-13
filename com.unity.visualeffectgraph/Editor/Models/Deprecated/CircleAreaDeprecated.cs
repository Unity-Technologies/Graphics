using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
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

        public override void Sanitize(int version)
        {
            var newArea = ScriptableObject.CreateInstance<Operator.CircleArea>();
            SanitizeHelper.MigrateTCircleFromCircle(newArea.inputSlots[0], inputSlots[0]);
            VFXSlot.CopyLinksAndValue(newArea.outputSlots[0], outputSlots[0], true);
            ReplaceModel(newArea, this);
        }
    }
}

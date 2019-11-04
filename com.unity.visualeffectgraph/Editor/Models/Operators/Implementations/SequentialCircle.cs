using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class SequentialCircle : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Element index used to loop over the sequence")]
            public uint Index = 0u;
            [Tooltip("Element count used to loop over the sequence")]
            public uint Count = 64u;
            [Tooltip("Center of the circle")]
            public Position Center = Position.defaultValue;
            [Tooltip("Rotation Axis")]
            public DirectionType Normal = new DirectionType() { direction = Vector3.forward };
            [Tooltip("Start Angle (Midnight direction)")]
            public DirectionType Up = new DirectionType() { direction = Vector3.up };
            [Tooltip("Radius of the circle")]
            public float Radius = 1.0f;
        }

        public class OutputProperties
        {
            public Position r = Position.defaultValue;
        }

        public override string name
        {
            get
            {
                return "Sequential Circle";
            }
        }

        [SerializeField, VFXSetting]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Wrap;

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var index = inputExpression[0];
            var count = inputExpression[1];
            var center = inputExpression[2];
            var normal = inputExpression[3];
            var up = inputExpression[4];
            var radius = inputExpression[5];

            return new[] { VFXOperatorUtility.SequentialCircle(center, radius, normal, up, index, count, mode) };
        }
    }
}

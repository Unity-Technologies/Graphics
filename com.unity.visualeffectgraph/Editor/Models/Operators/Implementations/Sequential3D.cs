using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Sequential3D : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Element index used to loop over the sequence")]
            public uint Index = 0u;

            [Tooltip("Element X count used to loop over the sequence")]
            public uint CountX = 8u;
            [Tooltip("Element Y count used to loop over the sequence")]
            public uint CountY = 8u;
            [Tooltip("Element Z count used to loop over the sequence")]
            public uint CountZ = 8u;

            public Position Origin = Position.defaultValue;
            public Vector AxisX = Vector3.right;
            public Vector AxisY = Vector3.up;
            public Vector AxisZ = Vector3.forward;
        }

        public class OutputProperties
        {
            public Position r = Position.defaultValue;
        }

        public override string name
        {
            get
            {
                return "Sequential 3D";
            }
        }

        [SerializeField, VFXSetting]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Wrap;

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var index = inputExpression[0];
            var countX = inputExpression[1];
            var countY = inputExpression[2];
            var countZ = inputExpression[3];
            var origin = inputExpression[4];
            var axisX = inputExpression[5];
            var axisY = inputExpression[6];
            var axisZ = inputExpression[7];

            return new[] { VFXOperatorUtility.Sequential3D(origin, axisX, axisY, axisZ, index, countX, countY, countZ, mode) };
        }
    }
}

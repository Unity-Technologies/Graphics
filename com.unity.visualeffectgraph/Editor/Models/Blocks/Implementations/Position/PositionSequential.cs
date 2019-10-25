using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class PositionSequentialVariantProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "shape", Enum.GetValues(typeof(PositionSequential.SequentialShape)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Position", variantProvider = typeof(PositionSequentialVariantProvider))]
    class PositionSequential : VFXBlock
    {
        public enum SequentialShape
        {
            Line,
            Circle,
            ThreeDimensional,
        }

        public enum IndexMode
        {
            ParticleID,
            Custom
        }


        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        [Tooltip("Specifies the type of shape to use for the position sequence.")]
        protected SequentialShape shape = SequentialShape.Line;

        [SerializeField, VFXSetting]
        [Tooltip("Specifies whether to use the Particle ID or a custom index when fetching the progression in the sequence.")]
        protected IndexMode index = IndexMode.ParticleID;

        [SerializeField, VFXSetting]
        [Tooltip("When enabled, the block will write to the particle position attribute.")]
        private bool writePosition = true;

        [SerializeField, VFXSetting]
        [Tooltip("When enabled, the block will write to the particle target position attribute.")]
        private bool writeTargetPosition = false;

        [SerializeField, VFXSetting]
        [Tooltip("Specifies how the sequence should behave at the end. It can either wrap back to the beginning, clamp, or continue in a mirrored direction.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Wrap;

        public override string name { get { return string.Format("Position : Sequential ({0})", shape); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public class InputProperties
        {
        }

        public class InputPropertiesCustomIndex
        {
            [Tooltip("Sets the index used to sample the sequential distribution.")]
            public uint Index = 0;
        }

        public class InputPropertiesWritePosition
        {
            [Tooltip("Sets an offset to the initial index used to compute the position.")]
            public int OffsetIndex = 0;
        }

        public class InputPropertiesWriteTargetPosition
        {
            [Tooltip("Sets an offset to the initial index used to compute the target position.")]
            public int OffsetTargetIndex = 1;
        }

        public class InputPropertiesLine
        {
            [Tooltip("Sets the count used to loop over the entire sequence.")]
            public uint Count = 64;
            [Tooltip("Sets the start position of the sequential line.")]
            public Position Start = Position.defaultValue;
            [Tooltip("Sets the end position of the sequential line.")]
            public Position End = new Position() { position = new Vector3(1, 0, 0) };
        }

        public class InputPropertiesCircle
        {
            [Tooltip("Sets the count used to loop over the entire sequence.")]
            public uint Count = 64;
            [Tooltip("Sets the center of the sequential circle.")]
            public Position Center = Position.defaultValue;
            [Tooltip("Sets the Forward axis of the sequential circle.")]
            public DirectionType Normal = new DirectionType() { direction = Vector3.forward };
            [Tooltip("Sets the Up axis of the sequential circle.")]
            public DirectionType Up = new DirectionType() { direction = Vector3.up };
            [Tooltip("Sets the radius of the sequential circle.")]
            public float Radius = 1.0f;
        }

        public class InputPropertiesThreeDimensional
        {
            [Tooltip("Sets the count on the X axis used to loop over the entire sequence.")]
            public uint CountX = 8;
            [Tooltip("Sets the count on the Y axis used to loop over the entire sequence.")]
            public uint CountY = 8;
            [Tooltip("Sets the count on the Z axis used to loop over the entire sequence.")]
            public uint CountZ = 8;

            [Tooltip("Sets the origin position of the sequence.")]
            public Position Origin = Position.defaultValue;

            [Tooltip("Sets the X axis of the sequence.")]
            public Vector AxisX = Vector3.right;
            [Tooltip("Sets the Y axis of the sequence.")]
            public Vector AxisY = Vector3.up;
            [Tooltip("Sets the Z axis of the sequence.")]
            public Vector AxisZ = Vector3.forward;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var commonProperties = PropertiesFromType("InputProperties");

                if (index == IndexMode.Custom)
                    commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesCustomIndex"));

                if (writePosition)
                    commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesWritePosition"));

                if (writeTargetPosition)
                    commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesWriteTargetPosition"));

                switch (shape)
                {
                    case SequentialShape.Line: commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesLine")); break;
                    case SequentialShape.Circle: commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesCircle")); break;
                    case SequentialShape.ThreeDimensional: commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesThreeDimensional")); break;
                }

                return commonProperties;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (index == IndexMode.ParticleID)
                    yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);

                if (writePosition)
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);

                if (writeTargetPosition)
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.ReadWrite);
            }
        }

        private VFXExpression GetPositionFromIndex(VFXExpression indexExpr, IEnumerable<VFXNamedExpression> expressions)
        {
            if (shape == SequentialShape.Line)
            {
                var start = expressions.First(o => o.name == "Start").exp;
                var end = expressions.First(o => o.name == "End").exp;
                var count = expressions.First(o => o.name == "Count").exp;
                return VFXOperatorUtility.SequentialLine(start, end, indexExpr, count, mode);
            }
            else if (shape == SequentialShape.Circle)
            {
                var center = expressions.First(o => o.name == "Center").exp;
                var normal = expressions.First(o => o.name == "Normal").exp;
                var up = expressions.First(o => o.name == "Up").exp;
                var radius = expressions.First(o => o.name == "Radius").exp;
                var count = expressions.First(o => o.name == "Count").exp;
                return VFXOperatorUtility.SequentialCircle(center, radius, normal, up, indexExpr, count, mode);
            }
            else if (shape == SequentialShape.ThreeDimensional)
            {
                var origin = expressions.First(o => o.name == "Origin").exp;
                var axisX = expressions.First(o => o.name == "AxisX").exp;
                var axisY = expressions.First(o => o.name == "AxisY").exp;
                var axisZ = expressions.First(o => o.name == "AxisZ").exp;
                var countX = expressions.First(o => o.name == "CountX").exp;
                var countY = expressions.First(o => o.name == "CountY").exp;
                var countZ = expressions.First(o => o.name == "CountZ").exp;
                return VFXOperatorUtility.Sequential3D(origin, axisX, axisY, axisZ, indexExpr, countX, countY, countZ, mode);
            }
            throw new NotImplementedException();
        }

        private static readonly string s_computedPosition = "computedPosition";
        private static readonly string s_computedTargetPosition = "computedTargetPosition";

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var expressions = GetExpressionsFromSlots(this);
                var indexExpr = (index == IndexMode.ParticleID) ? new VFXAttributeExpression(VFXAttribute.ParticleId) : expressions.First(o => o.name == "Index").exp;

                if (writePosition)
                {
                    var indexOffsetExpr = indexExpr + new VFXExpressionCastIntToUint(expressions.First(o => o.name == "OffsetIndex").exp);
                    var positionExpr = GetPositionFromIndex(indexOffsetExpr, expressions);
                    yield return new VFXNamedExpression(positionExpr, s_computedPosition);
                }

                if (writeTargetPosition)
                {
                    var indexOffsetExpr = indexExpr + new VFXExpressionCastIntToUint(expressions.First(o => o.name == "OffsetTargetIndex").exp);
                    var positionExpr = GetPositionFromIndex(indexOffsetExpr, expressions);
                    yield return new VFXNamedExpression(positionExpr, s_computedTargetPosition);
                }
            }
        }

        public override string source
        {
            get
            {
                var source = string.Empty;
                if (writePosition)
                {
                    source += string.Format("position += {0};\n", s_computedPosition);
                }

                if (writeTargetPosition)
                {
                    source += string.Format("targetPosition += {0};\n", s_computedTargetPosition);
                }
                return source;
            }
        }
    }
}

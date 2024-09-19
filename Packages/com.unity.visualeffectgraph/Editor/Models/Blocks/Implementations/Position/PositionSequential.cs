using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionSequentialVariantProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            return new[] { PositionSequential.SequentialShape.Circle, PositionSequential.SequentialShape.Line, PositionSequential.SequentialShape.ThreeDimensional }
                .Select(x => new Variant(
                    "Set".Label(false).AppendLiteral("Position Sequential", false).AppendLabel(x.ToString()),
                    "Position Shape/Sequential",
                    typeof(PositionSequential),
                    new[]
                    {
                        new KeyValuePair<string, object>("compositionPosition", AttributeCompositionMode.Overwrite),
                        new KeyValuePair<string, object>("shape", x)
                    }));
        }
    }

    [VFXHelpURL("Block-SetPosition(Sequential)")]
    [VFXInfo(variantProvider = typeof(PositionSequentialVariantProvider))]
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

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Position. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionPosition = AttributeCompositionMode.Add;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Direction. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionDirection = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on TargetPosition. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionTargetPosition = AttributeCompositionMode.Add;

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
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        public override string name => VFXBlockUtility.GetNameString(compositionPosition).Label(false).AppendLiteral("Position Sequential", false).AppendLabel(shape.ToString());
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

        public class InputPropertiesBlendPosition
        {
            [Range(0.0f, 1.0f), Tooltip("Sets the blending value for position attribute.")]
            public float blendPosition = 1.0f;
        }
        public class InputPropertiesBlendTargetPosition
        {
            [Range(0.0f, 1.0f), Tooltip("Sets the blending value for targetPosition attribute.")]
            public float blendTargetPosition = 1.0f;
        }

        public class InputPropertiesBlendDirection
        {
            [Range(0.0f, 1.0f), Tooltip("Set the blending value for direction attribute.")]
            public float blendDirection = 1.0f;
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
                {
                    commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesWritePosition"));
                    if (compositionPosition == AttributeCompositionMode.Blend)
                        commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesBlendPosition"));

                    if (compositionDirection == AttributeCompositionMode.Blend)
                        commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesBlendDirection"));
                }

                if (writeTargetPosition)
                {
                    commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesWriteTargetPosition"));
                    if (compositionTargetPosition == AttributeCompositionMode.Blend)
                        commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesBlendTargetPosition"));
                }

                switch (shape)
                {
                    case SequentialShape.Line: commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesLine")); break;
                    case SequentialShape.Circle: commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesCircle")); break;
                    case SequentialShape.ThreeDimensional: commonProperties = commonProperties.Concat(PropertiesFromType("InputPropertiesThreeDimensional")); break;
                }


                return commonProperties;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!writePosition)
                {
                    yield return "compositionPosition";
                    yield return "compositionDirection";
                }
                if (!writeTargetPosition)
                    yield return "compositionTargetPosition";
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (index == IndexMode.ParticleID)
                    yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);

                if (writePosition)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, compositionPosition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.Direction, compositionDirection == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                }

                if (writeTargetPosition)
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, compositionTargetPosition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
            }
        }

        private void GetPositionAndDirectionFromIndex(VFXExpression indexExpr, IEnumerable<VFXNamedExpression> expressions, out VFXExpression positionExpr, out VFXExpression directionExpr)
        {
            if (shape == SequentialShape.Line)
            {
                var start = expressions.First(o => o.name == "Start").exp;
                var end = expressions.First(o => o.name == "End").exp;
                var count = expressions.First(o => o.name == "Count").exp;
                positionExpr = VFXOperatorUtility.SequentialLine(start, end, indexExpr, count, mode);
                directionExpr = VFXOperatorUtility.SafeNormalize(end - start);
            }
            else if (shape == SequentialShape.Circle)
            {
                var center = expressions.First(o => o.name == "Center").exp;
                var normal = expressions.First(o => o.name == "Normal").exp;
                var up = expressions.First(o => o.name == "Up").exp;
                var radius = expressions.First(o => o.name == "Radius").exp;
                var count = expressions.First(o => o.name == "Count").exp;
                positionExpr = VFXOperatorUtility.SequentialCircle(center, radius, normal, up, indexExpr, count, mode);
                directionExpr = VFXOperatorUtility.SafeNormalize(positionExpr - center);
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
                positionExpr = VFXOperatorUtility.Sequential3D(origin, axisX, axisY, axisZ, indexExpr, countX, countY, countZ, mode);
                directionExpr = VFXOperatorUtility.SafeNormalize(positionExpr - origin);
            }
            else throw new NotImplementedException();
        }

        private static readonly string s_computedPosition = "computedPosition";
        private static readonly string s_computedDirection = "computedDirection";
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

                    GetPositionAndDirectionFromIndex(indexOffsetExpr, expressions, out var positionExpr, out var directionExpr);

                    yield return new VFXNamedExpression(positionExpr, s_computedPosition);
                    yield return new VFXNamedExpression(directionExpr, s_computedDirection);

                    if (compositionPosition == AttributeCompositionMode.Blend)
                        yield return expressions.FirstOrDefault(o => o.name == "blendPosition");

                    if (compositionDirection == AttributeCompositionMode.Blend)
                        yield return expressions.FirstOrDefault(o => o.name == "blendDirection");
                }

                if (writeTargetPosition)
                {
                    var indexOffsetExpr = indexExpr + new VFXExpressionCastIntToUint(expressions.First(o => o.name == "OffsetTargetIndex").exp);
                    GetPositionAndDirectionFromIndex(indexOffsetExpr, expressions, out var targetPositionExpr, out var targetDirectionExpr);
                    yield return new VFXNamedExpression(targetPositionExpr, s_computedTargetPosition);
                    if (compositionTargetPosition == AttributeCompositionMode.Blend)
                        yield return expressions.FirstOrDefault(o => o.name == "blendTargetPosition");
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
                    source += VFXBlockUtility.GetComposeString(compositionPosition, "position", s_computedPosition, "blendPosition");
                    source += "\n";
                    source += VFXBlockUtility.GetComposeString(compositionDirection, "direction", s_computedDirection, "blendDirection");
                }

                if (writeTargetPosition)
                {
                    source += "\n";
                    source += VFXBlockUtility.GetComposeString(compositionTargetPosition, "targetPosition", s_computedTargetPosition, "blendTargetPosition");
                }
                return source;
            }
        }

        public static void GenerateSequentialCircleErrors(IVFXErrorReporter report, string countName, string normalName, string upName, VFXModel model)
        {
            var slotContainer = model as IVFXSlotContainer;
            if (slotContainer == null)
                return;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding);
            var countExpression = slotContainer.inputSlots.Single(x => x.name == countName).GetExpression();
            var normalExpression = slotContainer.inputSlots.Single(x => x.name == normalName).GetExpression();
            var upExpression = slotContainer.inputSlots.Single(x => x.name == upName).GetExpression();
            context.RegisterExpression(countExpression);
            context.RegisterExpression(normalExpression);
            context.RegisterExpression(upExpression);
            context.Compile();

            if (context.GetReduced(countExpression) is var countExpressionReduced &&
                countExpressionReduced.Is(VFXExpression.Flags.Constant) &&
                countExpressionReduced.Get<uint>() == 0)
            {
                report.RegisterError("CircleCountIsZero", VFXErrorType.Warning, "A circle with Count = 0 is not valid", model);
            }

            if (context.GetReduced(normalExpression) is var normalExpressionReduced &&
                context.GetReduced(upExpression) is var upExpressionReduced &&
                normalExpressionReduced.Is(VFXExpression.Flags.Constant) &&
                upExpressionReduced.Is(VFXExpression.Flags.Constant))
            {

                var normal = normalExpressionReduced.Get<Vector3>();
                var up = upExpressionReduced.Get<Vector3>();

                if (float.IsNaN(normal.x) || float.IsNaN(normal.y) || float.IsNaN(normal.z))
                {
                    report.RegisterError("CircleNormalIsInvalid", VFXErrorType.Warning, "Normal vector is invalid.", model);
                }

                if (float.IsNaN(up.x) || float.IsNaN(up.y) || float.IsNaN(up.z))
                {
                    report.RegisterError("CircleUpIsInvalid", VFXErrorType.Warning, "Up vector is invalid.", model);
                }

                if (Math.Abs(Vector3.Cross(normal, up).sqrMagnitude) < 10e-5f)
                {
                    report.RegisterError("CircleNormalAndUpArCollinear", VFXErrorType.Warning, "Normal and Up vectors are collinear, circle orientation cannot be computed.", model);
                }
            }
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            if (shape == SequentialShape.Circle)
            {
                GenerateSequentialCircleErrors(report, nameof(InputPropertiesCircle.Count), nameof(InputPropertiesCircle.Normal), nameof(InputPropertiesCircle.Up), this);
            }
        }
    }
}

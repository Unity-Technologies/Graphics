using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class OrientStripModeProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "mode", Enum.GetValues(typeof(OrientStrip.Mode)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Orientation", variantProvider = typeof(OrientStripModeProvider))]
    class OrientStrip : VFXBlock
    {
        public enum Mode
        {
            FaceCamera,
            CustomZ,
            CustomY,
            FromTargetPosition,
        }

        [VFXSetting, Tooltip("Specifies the orientation mode of the particle. It can face towards the camera or a specific position, orient itself along the velocity or a fixed axis, or use more advanced facing behavior.")]
        public Mode mode;

        public override string name { get { return "Orient Strip : " + ObjectNames.NicifyVariableName(mode.ToString()); } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.Output; } }
        public override VFXDataType compatibleData { get { return VFXDataType.ParticleStrip; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);

                if (mode == Mode.FaceCamera)
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);

                if (mode == Mode.FromTargetPosition)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Write);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Write);
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.CustomZ:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Front"), new DirectionType() { direction = -Vector3.forward });
                        break;

                    case Mode.CustomY:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Up"), new DirectionType() { direction = Vector3.up });
                        break;

                    case Mode.FromTargetPosition:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Position), "TargetPosition"), new Position() { position = Vector3.zero });
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Scale"), 1.0f);
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Pivot"), 0.5f);
                        break;
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var exp in base.parameters)
                    yield return exp;

                yield return new VFXNamedExpression(new VFXExpressionStripTangent(), "stripTangent");
            }
        }

        public override string source
        {
            get
            {
                switch (mode)
                {
                    case Mode.FaceCamera:
                        return
@"axisX = stripTangent;
axisZ = position - GetViewVFXPosition();
axisY = normalize(cross(axisZ, axisX));
axisZ = cross(axisX, axisY);
";

                    case Mode.CustomZ:
                        return
@"axisX = stripTangent;
axisZ = -Front;
axisY = normalize(cross(axisZ, axisX));
axisZ = cross(axisX, axisY);
";

                    case Mode.CustomY:
                        return
@"axisX = stripTangent;
axisY = Up;
axisZ = normalize(cross(axisX, axisY));
axisY = cross(axisZ, axisX);
";

                    case Mode.FromTargetPosition:
                        return
@"axisX = stripTangent;
axisY = TargetPosition - position;
axisZ = normalize(cross(axisX, axisY));
axisX = normalize(cross(axisX, axisZ));
size = Scale;
pivotY = -Pivot / Scale;
";

                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}

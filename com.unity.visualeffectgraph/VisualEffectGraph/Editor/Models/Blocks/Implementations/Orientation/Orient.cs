using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Orientation")]
    class Orient : VFXBlock
    {
        public enum Mode
        {
            FaceCameraPlane,
            FaceCameraPosition,
            LookAtPosition,
            FixedOrientation,
            FixedAxis,
            AlongVelocity,
        }

        [VFXSetting]
        public Mode mode;

        public override string name { get { return "Orient"; } }

        public override VFXContextType compatibleContexts   { get { return VFXContextType.kOutput; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);
                if (mode != Mode.FixedOrientation && mode != Mode.FaceCameraPlane)
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (mode == Mode.AlongVelocity)
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.LookAtPosition:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Position), "Position"));
                        break;

                    case Mode.FixedOrientation:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Front"), new DirectionType() { direction = Vector3.forward });
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Up"), new DirectionType() { direction = Vector3.up });
                        break;

                    case Mode.FixedAxis:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Up"), new DirectionType() { direction = Vector3.up });
                        break;
                }
            }
        }

        public override string source
        {
            get
            {
                switch (mode)
                {
                    case Mode.FaceCameraPlane:
                        return @"
float3x3 viewRot = GetVFXToViewRotMatrix();
axisX = viewRot[0].xyz;
axisY = viewRot[1].xyz;
axisZ = -viewRot[2].xyz;
";

                    case Mode.FaceCameraPosition:
                        return @"
axisZ = normalize(position - GetViewVFXPosition());
axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.LookAtPosition:
                        return @"
axisZ = normalize(position - Position_position);
axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.FixedOrientation:
                        return @"
axisZ = Front;
axisX = normalize(cross(Up,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.FixedAxis:
                        return @"
axisY = Up;
axisZ = position - GetViewVFXPosition();
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);
";

                    case Mode.AlongVelocity:
                        return @"
axisY = normalize(velocity);
axisZ = position - GetViewVFXPosition();
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);
";

                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}

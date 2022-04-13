using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Orientation")]
    class ConnectTarget : VFXBlock
    {
        public enum OrientMode
        {
            Camera,
            Direction,
            LookAtPosition
        }

        [VFXSetting, Tooltip("Specifies where the particle should orient itself to. It can face the camera, a particular direction, or a specific position.")]
        public OrientMode Orientation = OrientMode.Camera;

        public override string name { get { return "Connect Target"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.Output; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public class InputProperties
        {
            [Tooltip("Sets the position the particle aims to connect to. This corresponds to the top end of the particle.")]
            public Position TargetPosition = Position.defaultValue;
            [Tooltip("Sets the direction the particle faces towards.")]
            public DirectionType LookDirection = DirectionType.defaultValue;
            [Tooltip("Sets the position the particle faces towards.")]
            public Position LookAtPosition = Position.defaultValue;
            [Range(0.0f, 1.0f), Tooltip("Sets the position relative to the segment to act as a pivot.")]
            public float PivotShift = 0.5f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in PropertiesFromType(GetInputPropertiesTypeName()))
                {
                    if (Orientation != OrientMode.Direction && property.property.name == "LookDirection") continue;
                    if (Orientation != OrientMode.LookAtPosition && property.property.name == "LookAtPosition") continue;

                    yield return property;
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);

                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Write);
            }
        }

        public override string source
        {
            get
            {
                string orient = string.Empty;

                switch (Orientation)
                {
                    case OrientMode.Camera: orient = "position - GetViewVFXPosition()"; break;
                    case OrientMode.Direction: orient = "LookDirection"; break;
                    case OrientMode.LookAtPosition: orient = "position - LookAtPosition"; break;
                }

                return string.Format(@"
axisY = TargetPosition-position;
float len = length(axisY);
scaleY = len / size;
axisY /= len;
axisZ = {0};
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);

position = lerp(position, TargetPosition, PivotShift);
pivotY = PivotShift - 0.5;
", orient);
            }
        }
    }
}

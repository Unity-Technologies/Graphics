using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity")]
    class VelocityDirection : VFXBlock
    {
        public override string name { get { return "Velocity (Direction)"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(new VFXAttribute("direction", VFXValue.Constant(new Vector3(0.0f, 0.0f, 1.0f))), VFXAttributeMode.ReadWrite);
            }
        }

        public class InputProperties
        {
            [Tooltip("The direction of the velocity to add to the particles.")]
            public DirectionType Direction = new DirectionType() { direction = Vector3.forward };
            [Tooltip("The speed to add to the particles, in the new direction.")]
            public float Speed = 1.0f;
            [Range(0, 1), Tooltip("Blend between the original emission direction and the new direction, based on this value.")]
            public float DirectionBlend = 1.0f;
        }

        public override string source
        {
            get
            {
                return @"
direction = lerp(direction, Direction, DirectionBlend);
velocity += direction * Speed;";
            }
        }
    }
}

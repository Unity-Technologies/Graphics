using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity")]
    class VelocityRandomize : VFXBlock
    {
        public override string name { get { return "Velocity (Randomize)"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWriteCurrent);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWriteCurrent);
                yield return new VFXAttributeInfo(new VFXAttribute("direction", VFXValue.Constant(new Vector3(0.0f, 0.0f, 1.0f))), VFXAttributeMode.ReadWriteCurrent);
            }
        }

        public class InputProperties
        {
            [Tooltip("The speed to add to the particles, in the random direction.")]
            public float Speed = 1.0f;
            [Range(0, 1), Tooltip("Blend between the original emission direction and the new random direction, based on this value.")]
            public float DirectionBlend = 1.0f;
        }

        public override string source
        {
            get
            {
                return @"
float3 randomDirection = normalize(RAND3 * 2.0f - 1.0f);
direction = normalize(lerp(direction, randomDirection, DirectionBlend));
velocity += direction * Speed;";
            }
        }
    }
}

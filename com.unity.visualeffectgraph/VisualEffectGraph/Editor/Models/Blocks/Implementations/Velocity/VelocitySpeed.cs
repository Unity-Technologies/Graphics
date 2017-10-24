using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity")]
    class VelocitySpeed : VFXBlock
    {
        public override string name { get { return "Velocity (Speed)"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(new VFXAttribute("direction", VFXValue.Constant(new Vector3(0.0f, 0.0f, 1.0f))), VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("The speed to add to the particles.")]
            public float Speed = 1.0f;
        }

        public override string source
        {
            get
            {
                return @"velocity += direction * Speed;";
            }
        }
    }
}

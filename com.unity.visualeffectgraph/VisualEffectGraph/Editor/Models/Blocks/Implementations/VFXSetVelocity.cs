using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Tests")]
    class VFXSetVelocity : VFXBlock
    {
        public override string name { get { return "SetVelocity"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                return new List<VFXAttributeInfo>() { new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Write) };
            }
        }

        public class InputProperties
        {
            public Vector3 inputVelocity = new Vector3(0.0f, 1.0f, 0.0f);
        }

        public override string source
        {
            get
            {
                return "velocity = inputVelocity;";
            }
        }
    }
}

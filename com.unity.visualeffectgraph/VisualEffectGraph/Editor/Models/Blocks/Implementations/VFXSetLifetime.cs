using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Tests")]
    class VFXSetLifetime : VFXBlock
    {
        public override string name { get { return "SetLifetime"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                return new List<VFXAttributeInfo>() { new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Write) };
            }
        }

        public class InputProperties
        {
            public float inputLifetime = 1.0f;
        }

        public override string source
        {
            get
            {
                return "lifetime = inputLifetime;";
            }
        }
    }
}

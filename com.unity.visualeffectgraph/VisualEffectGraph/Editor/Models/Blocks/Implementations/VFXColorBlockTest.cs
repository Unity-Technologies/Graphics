using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Tests")]
    class VFXColorBlockTest : VFXBlock
    {
        public override string name { get { return "Color Test"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kAll; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                return new List<VFXAttributeInfo>() { new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Write) };
            }
        }

        public class InputProperties
        {
            public Color Color = Color.red;
        }

        public override string source
        {
            get
            {
                return string.Format("{0}.rgb = Color.rgb;", VFXAttribute.Color.name);
            }
        }
    }
}

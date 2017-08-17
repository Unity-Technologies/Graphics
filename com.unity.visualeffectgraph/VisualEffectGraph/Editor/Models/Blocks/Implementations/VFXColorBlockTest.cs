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
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Write);
            }
        }

        public class InputProperties
        {
            public Color color = Color.red;
        }
    }
}

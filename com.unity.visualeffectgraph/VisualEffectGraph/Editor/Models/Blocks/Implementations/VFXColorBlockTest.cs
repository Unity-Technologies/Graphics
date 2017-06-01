using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXColorBlockTest : VFXBlock
    {
        public override string name { get { return "Color Test"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kAll; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public class InputProperties
        {
            public Color color = Color.red;
        }
    }
}

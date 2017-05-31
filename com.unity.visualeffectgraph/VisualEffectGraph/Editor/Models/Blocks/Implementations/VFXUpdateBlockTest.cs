using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXUpdateBlockTest : VFXBlock
    {
        public override string name                         { get { return "Update Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.kParticle; } }
    }
}

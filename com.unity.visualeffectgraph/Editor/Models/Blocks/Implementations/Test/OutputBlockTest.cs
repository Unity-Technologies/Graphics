using System;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    [VFXInfo(category = "Tests")]
    class OutputBlockTest : VFXBlock
    {
        public override string name                         { get { return "Output Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kOutput; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.kParticle; } }
    }
}

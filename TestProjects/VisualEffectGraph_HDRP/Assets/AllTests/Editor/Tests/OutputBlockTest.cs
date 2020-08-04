using System;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    class OutputBlockTest : VFXBlock
    {
        public override string name                         { get { return "Output Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.Output; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.Particle; } }
    }
}

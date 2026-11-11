using System;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    class InitBlockTest : VFXBlock
    {
        public override string name                         { get { return "Init Block"; }}
        public override VFXContextType compatibleContexts   { get { return VFXContextType.Init; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.Particle; } }
    }
}

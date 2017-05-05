using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOutputBlockTest : VFXBlock
    {
        public override string name                         { get { return "Output Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kOutput; } }
    }
}

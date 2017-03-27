using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXInitBlockTest : VFXBlock
    {
        public override string name                         { get { return "Init Block"; }}
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInit; } }
    }
}

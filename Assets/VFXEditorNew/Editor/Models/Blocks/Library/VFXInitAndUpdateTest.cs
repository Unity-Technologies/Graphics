using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "test")]
    class VFXInitAndUpdateTest : VFXBlock
    {
        public override string name                         { get { return "Init And Update Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInitAndUpdate; } }
    }
}

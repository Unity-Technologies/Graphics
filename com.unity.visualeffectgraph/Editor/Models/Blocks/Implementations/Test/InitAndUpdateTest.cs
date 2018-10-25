using System;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    [VFXInfo(category = "Tests")]
    class InitAndUpdateTest : VFXBlock
    {
        public override string name                         { get { return "Init And Update Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInitAndUpdate; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.kParticle; } }
    }
}

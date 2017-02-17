using System;

namespace UnityEditor.VFX
{
	// TODO temp
	// Remove that!

    [VFXInfo]
    class VFXBasicInitialize : VFXContext
    {
        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kNone, VFXDataType.kParticle) { }
        public override string name { get { return "Initialize"; } }
    }

    [VFXInfo]
    class VFXBasicUpdate : VFXContext
    {
        public VFXBasicUpdate() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) { }
        public override string name { get { return "Update"; } }
    }

    [VFXInfo]
    class VFXBasicOutput : VFXContext
    {
        public VFXBasicOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) { }
        public override string name { get { return "Output"; } }
    }
}
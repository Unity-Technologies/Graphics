namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicInitialize : VFXContext
    {
        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kNone, VFXDataType.kParticle) { }
        public override string name { get { return "Initialize"; } }
    }
}
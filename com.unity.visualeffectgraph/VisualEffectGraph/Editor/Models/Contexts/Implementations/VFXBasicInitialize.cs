namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicInitialize : VFXContext
    {
        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kSpawnEvent, VFXDataType.kParticle) {}
        public override string name { get { return "Initialize"; } }
        public override string codeGeneratorTemplate { get { return "VFXInit"; } }
        public override bool codeGeneratorCompute { get { return true; } }
    }
}

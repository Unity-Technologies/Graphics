namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicInitialize : VFXContext
    {
        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kSpawnEvent, VFXDataType.kParticle) {}
        public override string name { get { return "Initialize"; } }

        public override VFXCodeGenerator codeGenerator
        {
            get
            {
                return new VFXCodeGenerator("VFXInit.template");
            }
        }
    }
}

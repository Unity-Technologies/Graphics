namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicOutput : VFXContext
    {
        public VFXBasicOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) { }
        public override string name { get { return "Output"; } }
    }
}
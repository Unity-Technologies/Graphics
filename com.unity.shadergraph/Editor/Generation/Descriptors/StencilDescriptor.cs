namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct StencilDescriptor
    {
        public string WriteMask;
        public string Ref;
        public string Comp;
        public string ZFail;
        public string Fail;
        public string Pass;
        public string CompBack;
        public string ZFailBack;
        public string FailBack;
        public string PassBack;
    }
}

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct StencilDescriptor
    {
        public string WriteMask;
        public string Ref;
        public string CompFront;
        public string ZFailFront;
        public string FailFront;
        public string PassFront;
        public string CompBack;
        public string ZFailBack;
        public string FailBack;
        public string PassBack;
    }
}

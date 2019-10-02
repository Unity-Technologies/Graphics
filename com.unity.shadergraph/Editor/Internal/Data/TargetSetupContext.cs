namespace UnityEditor.ShaderGraph.Internal
{
    public struct TargetSetupContext
    {
        public IMasterNode masterNode { get; private set; }
        public SubShaderDescriptor descriptor { get; private set; }

        public void SetMasterNode(IMasterNode masterNode)
        {
            this.masterNode = masterNode;
        }

        public void SetupSubShader(SubShaderDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }
    }
}

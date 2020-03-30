namespace UnityEditor.ShaderGraph
{
    internal class BlockFieldDescriptor : FieldDescriptor
    {
        public IControl control { get; }
        public ShaderStage shaderStage { get; }

        public BlockFieldDescriptor(string tag, string name, string define, IControl control, ShaderStage shaderStage)
            : base (tag, name, define)
        {
            this.control = control;
            this.shaderStage = shaderStage;
        }
    }
}

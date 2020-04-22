namespace UnityEditor.ShaderGraph
{
    internal class BlockFieldDescriptor : FieldDescriptor
    {
        public IControl control { get; }
        public ShaderStage shaderStage { get; }
        public bool isHidden { get; }

        public BlockFieldDescriptor(string tag, string name, string define, IControl control, ShaderStage shaderStage, bool isHidden = false)
            : base (tag, name, define)
        {
            this.control = control;
            this.shaderStage = shaderStage;
            this.isHidden = isHidden;
        }
    }

    // TODO: This exposes the MaterialSlot API
    // TODO: This needs to be removed but is currently required by HDRP for DiffusionProfileInputMaterialSlot
    internal class CustomSlotBlockFieldDescriptor : BlockFieldDescriptor
    {
        public MaterialSlot slot { get; }

        public CustomSlotBlockFieldDescriptor(string tag, string name, string define, MaterialSlot slot)
            : base (tag, name, define, null, ShaderStage.Fragment)
        {
            this.slot = slot;
        }
    }
}

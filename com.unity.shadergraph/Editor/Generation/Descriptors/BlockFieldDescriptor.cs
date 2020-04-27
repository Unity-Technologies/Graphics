namespace UnityEditor.ShaderGraph
{
    internal class BlockFieldDescriptor : FieldDescriptor
    {
        public string displayName { get; }
        public IControl control { get; }
        public ShaderStage shaderStage { get; }
        public bool isHidden { get; }

        public BlockFieldDescriptor(string tag, string referenceName, string define, IControl control, ShaderStage shaderStage, bool isHidden = false)
            : base (tag, referenceName, define)
        {
            this.displayName = referenceName;
            this.control = control;
            this.shaderStage = shaderStage;
            this.isHidden = isHidden;
        }

        public BlockFieldDescriptor(string tag, string referenceName, string displayName, string define, IControl control, ShaderStage shaderStage, bool isHidden = false)
            : base (tag, referenceName, define)
        {
            this.displayName = displayName;
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

        public CustomSlotBlockFieldDescriptor(string tag, string referenceName, string define, MaterialSlot slot)
            : base (tag, referenceName, define, null, ShaderStage.Fragment)
        {
            this.slot = slot;
        }

        public CustomSlotBlockFieldDescriptor(string tag, string referenceName, string displayName, string define, MaterialSlot slot)
            : base (tag, referenceName, displayName, define, null, ShaderStage.Fragment)
        {
            this.slot = slot;
        }
    }
}

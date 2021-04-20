using System;
namespace UnityEditor.ShaderGraph
{
    internal class BlockFieldDescriptor : FieldDescriptor
    {
        public string displayName { get; }
        public IControl control { get; }
        public ShaderStage shaderStage { get; }
        public bool isHidden { get; }
        public bool isUnknown { get; }
        public bool isCustom { get; }

        internal string path { get; set; }

        public BlockFieldDescriptor(string tag, string referenceName, string define, IControl control, ShaderStage shaderStage, bool isHidden = false, bool isUnknown = false, bool isCustom = false)
            : base(tag, referenceName, define)
        {
            this.displayName = referenceName;
            this.control = control;
            this.shaderStage = shaderStage;
            this.isHidden = isHidden;
            this.isUnknown = isUnknown;
            this.isCustom = isCustom;
        }

        public BlockFieldDescriptor(string tag, string referenceName, string displayName, string define, IControl control, ShaderStage shaderStage, bool isHidden = false, bool isUnknown = false, bool isCustom = false)
            : base(tag, referenceName, define)
        {
            this.displayName = displayName;
            this.control = control;
            this.shaderStage = shaderStage;
            this.isHidden = isHidden;
            this.isUnknown = isUnknown;
            this.isCustom = isCustom;
        }
    }

    // TODO: This exposes the MaterialSlot API
    // TODO: This needs to be removed but is currently required by HDRP for DiffusionProfileInputMaterialSlot
    internal class CustomSlotBlockFieldDescriptor : BlockFieldDescriptor
    {
        public Func<MaterialSlot> createSlot;

        public CustomSlotBlockFieldDescriptor(string tag, string referenceName, string define, Func<MaterialSlot> createSlot)
            : base(tag, referenceName, define, null, ShaderStage.Fragment)
        {
            this.createSlot = createSlot;
        }

        public CustomSlotBlockFieldDescriptor(string tag, string referenceName, string displayName, string define, Func<MaterialSlot> createSlot)
            : base(tag, referenceName, displayName, define, null, ShaderStage.Fragment)
        {
            this.createSlot = createSlot;
        }
    }
}

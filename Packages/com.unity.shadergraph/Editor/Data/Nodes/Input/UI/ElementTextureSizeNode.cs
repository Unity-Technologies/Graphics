using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Texture Size")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementTextureUVSize : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public const int TextureSizeSlotId = 0;

        private const string kTextureSizeName = "Texture Size";

        public override bool hasPreview { get { return false; } }

        public ElementTextureUVSize()
        {
            name = "Element Texture Size";
            synonyms = new string[] {};
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(TextureSizeSlotId, kTextureSizeName, kTextureSizeName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { TextureSizeSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (GetInputNodeFromSlot(TextureSizeSlotId) != null) sb.AppendLine(string.Format("TextureInfo UITKNodeOutput_{0} = GetTextureInfo(IN.typeTexSettings.y);", GetVariableNameForSlot(TextureSizeSlotId)));
            if (GetInputNodeFromSlot(TextureSizeSlotId) != null) sb.AppendLine(string.Format("$precision4 {0} = float4(UITKNodeOutput_{1}.textureSize, UITKNodeOutput_{2}.texelSize);",
                GetVariableNameForSlot(TextureSizeSlotId),
                GetVariableNameForSlot(TextureSizeSlotId),
                GetVariableNameForSlot(TextureSizeSlotId)));
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}

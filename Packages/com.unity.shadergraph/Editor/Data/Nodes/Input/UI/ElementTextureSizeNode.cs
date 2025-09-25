using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Texture Size")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementTextureUVSize : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public const int TextureWidthSlotId = 0;
        public const int TextureHeightSlotId = 1;
        public const int TexelWidthSlotId = 2;
        public const int TexelHeightSlotId = 3;

        private const string kTextureWidthName = "Width";
        private const string kTextureHeightName = "Height";
        private const string kTexelWidthName = "Texel Width";
        private const string kTexelHeightName = "Texel Height";

        public override bool hasPreview { get { return false; } }

        public ElementTextureUVSize()
        {
            name = "Element Texture Size";
            synonyms = new string[] {};
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(TextureWidthSlotId, kTextureWidthName, kTextureWidthName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(TextureHeightSlotId, kTextureHeightName, kTextureHeightName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(TexelWidthSlotId, kTexelWidthName, kTexelWidthName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(TexelHeightSlotId, kTexelHeightName, kTexelHeightName, SlotType.Output, 0.0f));

            RemoveSlotsNameNotMatching(new[] { TextureWidthSlotId, TextureHeightSlotId, TexelWidthSlotId, TexelHeightSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if ((GetInputNodeFromSlot(TextureWidthSlotId) != null) ||
                (GetInputNodeFromSlot(TextureHeightSlotId) != null) ||
                (GetInputNodeFromSlot(TexelWidthSlotId) != null) ||
                (GetInputNodeFromSlot(TexelHeightSlotId) != null))
            {
                string variableName = string.Format("TextureInfo_{0}", objectId.ToString());

                sb.AppendLine(string.Format("TextureInfo {0} = GetTextureInfo(IN.typeTexSettings.y);", variableName));
                if (GetInputNodeFromSlot(TextureWidthSlotId) != null) sb.AppendLine(string.Format("$precision1 {0} = {1}.textureSize.x;", GetVariableNameForSlot(TextureWidthSlotId), variableName));
                if (GetInputNodeFromSlot(TextureHeightSlotId) != null) sb.AppendLine(string.Format("$precision1 {0} = {1}.textureSize.y;", GetVariableNameForSlot(TextureHeightSlotId), variableName));
                if (GetInputNodeFromSlot(TexelWidthSlotId) != null) sb.AppendLine(string.Format("$precision1 {0} = {1}.texelSize.x;", GetVariableNameForSlot(TexelWidthSlotId), variableName));
                if (GetInputNodeFromSlot(TexelHeightSlotId) != null) sb.AppendLine(string.Format("$precision1 {0} = {1}.texelSize.y;", GetVariableNameForSlot(TexelHeightSlotId), variableName));
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}

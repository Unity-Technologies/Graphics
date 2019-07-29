using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{

    [Title("Input", "Texture", "Texel Size")]
    class Texture2DPropertiesNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotWId = 0;
        public const int OutputSlotHId = 2;
        public const int TextureInputId = 1;
        const string kOutputSlotWName = "Width";
        const string kOutputSlotHName = "Height";
        const string kTextureInputName = "Texture";

        public override bool hasPreview { get { return false; } }

        public Texture2DPropertiesNode()
        {
            name = "Texel Size";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotWId, kOutputSlotWName, kOutputSlotWName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotHId, kOutputSlotHName, kOutputSlotHName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            RemoveSlotsNameNotMatching(new[] { OutputSlotWId, OutputSlotHId, TextureInputId });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
			sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.z;", GetVariableNameForSlot(OutputSlotWId), GetSlotValue(TextureInputId, generationMode)));
			sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.w;", GetVariableNameForSlot(OutputSlotHId), GetSlotValue(TextureInputId, generationMode)));
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            var textureInput = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputId);
            var edge = textureInput != null ? owner.GetEdges(textureInput.slotReference).FirstOrDefault() : null;
            var fromNode = edge != null ? owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid) : null;
            var splatTexture = fromNode is PropertyNode propertyNode && propertyNode.shaderProperty is TextureShaderProperty textureProperty && textureProperty.splat;

            if (splatTexture)
            {
                // Create Texture{i}_TexelSize properties
                var textureName = fromNode.GetVariableNameForSlot(edge.outputSlot.slotId);
                textureName = textureName.Substring(0, textureName.Length - 1);
                for (int i = 0; i < 4; ++i)
                {
                    properties.AddShaderProperty(new Vector4ShaderProperty()
                    {
                        overrideReferenceName = $"{textureName}{i}_TexelSize",
                        generatePropertyBlock = false,
                    });
                }
            }
            else
            {
                properties.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = string.Format("{0}_TexelSize", GetSlotValue(TextureInputId, generationMode)),
                    generatePropertyBlock = false,
                });
            }

            base.CollectShaderProperties(properties, generationMode);
        }
    }
}

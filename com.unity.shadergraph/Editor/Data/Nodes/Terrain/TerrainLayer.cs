using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Layer")]
    class TerrainLayer : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        const int InputUVId = 0;
        const int InputLayerId = 1;
        const int OutputAlbedoId = 2;
        const int OutputNormalId = 3;
        const int OutputMetallicId = 4;
        const int OutputSmoothnessId = 5;
        const int OutputOcclusionId = 6;
        const int OutputAlphaId = 7;

        const string kInputUVSlotName = "UV";
        const string kInputLayerSlotName = "Layer Index";
        const string kOutputAlbedoSlotName = "Albedo";
        const string kOutputNormalSlotName = "Normal";
        const string kOutputMetallicSlotName = "Metallic";
        const string kOutputSmoothnessSlotName = "Smoothness";
        const string kOutputOcclusionSlotName = "Occlusion";
        const string kOutputAlphaSlotName = "Alpha";

        public TerrainLayer()
        {
            name = "Terrain Layer";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(InputLayerId, kInputLayerSlotName, kInputLayerSlotName, SlotType.Input, 0));
            AddSlot(new Vector3MaterialSlot(OutputAlbedoId, kOutputAlbedoSlotName, kOutputAlbedoSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputNormalId, kOutputNormalSlotName, kOutputNormalSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(OutputMetallicId, kOutputMetallicSlotName, kOutputMetallicSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSmoothnessId, kOutputSmoothnessSlotName, kOutputSmoothnessSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputOcclusionId, kOutputOcclusionSlotName, kOutputOcclusionSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputAlphaId, kOutputAlphaSlotName, kOutputAlphaSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[] { InputUVId, InputLayerId, OutputAlbedoId, OutputNormalId, OutputMetallicId, OutputSmoothnessId, OutputOcclusionId, OutputAlphaId, });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var albedoValue = GetVariableNameForSlot(OutputAlbedoId);
            var normalValue = GetVariableNameForSlot(OutputNormalId);
            var metallicValue = GetVariableNameForSlot(OutputMetallicId);
            var smoothnessValue = GetVariableNameForSlot(OutputSmoothnessId);
            var occlusionValue = GetVariableNameForSlot(OutputOcclusionId);
            var alphaValue = GetVariableNameForSlot(OutputAlbedoId);

            //sb.currentNode.owner.GetTargetIndex()
            //Debug.Log(sb.currentNode.name);

            //sb.AppendLine();
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                        return true;
                }

                return false;
            }
        }
    }
}

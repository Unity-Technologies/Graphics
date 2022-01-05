using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Splat")]
    class TerrainSplat : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        const int InputUVId = 0;
        const int InputSplatId = 1;
        const int OutputControlId = 2;

        const string kInputUVSlotName = "UV";
        const string kInputSplatSlotName = "Splat Index";
        const string kOutputControlSlotName = "Control";

        public TerrainSplat()
        {
            name = "Terrain Splat";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(InputSplatId, kInputSplatSlotName, kInputSplatSlotName, SlotType.Input, 0));
            AddSlot(new Vector3MaterialSlot(OutputControlId, kOutputControlSlotName, kOutputControlSlotName, SlotType.Output, Vector4.zero));

            RemoveSlotsNameNotMatching(new[] { InputUVId, InputSplatId, OutputControlId, });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {

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

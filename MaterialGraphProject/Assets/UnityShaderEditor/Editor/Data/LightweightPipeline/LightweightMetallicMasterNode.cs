using System;
using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Lightweight/PBR Metallic")]
    public class LightweightMetallicMasterNode : AbstractLightweightPBRMasterNode
    {
        public const string MetallicSlotName = "Metallic";
        public const int MetallicSlotId = 2;

        public LightweightMetallicMasterNode()
        {
            name = "LightweightMetallicMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, new Vector4(0.5f, 0.5f, 0.5f), ShaderStage.Fragment));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, new Vector4(0,0,1), ShaderStage.Fragment));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.one, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
                {
                    AlbedoSlotId,
                    NormalSlotId,
                    EmissionSlotId,
                    MetallicSlotId,
                    SmoothnessSlotId,
                    OcclusionSlotId,
                    AlphaSlotId
                });
        }

        protected override string GetTemplateName()
        {
            return "lightweightSubshaderPBR.template";
        }

        protected override void GetLightweightDefinesAndRemap(ShaderGenerator defines, ShaderGenerator surfaceOutputRemap, MasterRemapGraph remapper)
        {
            base.GetLightweightDefinesAndRemap(defines, surfaceOutputRemap, remapper);
            defines.AddShaderChunk("#define _METALLIC_SETUP 1", true);
        }

        protected override IEnumerable<int> masterSurfaceInputs
        {
            get
            {
                return new[]
                {
                    AlbedoSlotId,
                    NormalSlotId,
                    EmissionSlotId,
                    MetallicSlotId,
                    SmoothnessSlotId,
                    OcclusionSlotId,
                    AlphaSlotId,
                };
            }
        }

        protected override IEnumerable<int> masterVertexInputs
        {
            get
            {
                return new int[]
                {
                };
            }
        }
    }
}

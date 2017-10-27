using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master/Lightweight/PBR Specular")]
    public class LightweightSpecularMasterNode : AbstractLightweightPBRMasterNode
    {
        public const string SpecularSlotName = "Specular";
        public const int SpecularSlotId = 2;

        public const string WorkflowName = "Specular";

        public LightweightSpecularMasterNode()
        {
            name = "LightweightSpecularMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, new Vector3(0.5f, 0.5f, 0.5f), ShaderStage.Fragment));
            AddSlot(new Vector3MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, new Vector3(0,0,1), ShaderStage.Fragment));
            AddSlot(new Vector3MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Vector3.zero, ShaderStage.Fragment));
            AddSlot(new Vector3MaterialSlot(SpecularSlotId, SpecularSlotName, SpecularSlotName, SlotType.Input, Vector3.zero, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input,0, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                AlbedoSlotId,
                NormalSlotId,
                EmissionSlotId,
                SpecularSlotId,
                SmoothnessSlotId,
                OcclusionSlotId,
                AlphaSlotId
            });
        }
        protected override string GetTemplateName()
        {
            return "lightweightSubshaderPBR.template";
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
                    SpecularSlotId,
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

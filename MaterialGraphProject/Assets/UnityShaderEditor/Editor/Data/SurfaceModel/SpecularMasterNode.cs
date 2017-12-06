using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
/*    [Serializable]
    [Title("Master", "Specular")]
    public class SpecularMasterNode : AbstractSurfaceMasterNode
    {
        public const string SpecularSlotName = "Specular";
        public const int SpecularSlotId = 2;

        public const string LightFunctionName = "StandardSpecular";
        public const string SurfaceOutputStructureName = "SurfaceOutputStandardSpecular";

        public SpecularMasterNode()
        {
            name = "SpecularMasterNode";
            UpdateNodeAfterDeserialization();
        }

        protected override int[] surfaceInputs
        {
            get
            {
                return new[]
                {
                    AlbedoSlotId,
                    NormalSlotId,
                    EmissionSlotId,
                    SmoothnessSlotId,
                    OcclusionSlotId,
                    AlphaSlotId,
                    SpecularSlotId
                };
            }
        }


        protected override int[] vertexInputs
        {
            get
            {
                return new[]
                {
                    VertexOffsetId
                };
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(VertexOffsetId, VertexOffsetName, VertexOffsetName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(SpecularSlotId, SpecularSlotName, SpecularSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

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

        public override string GetSurfaceOutputName()
        {
            return SurfaceOutputStructureName;
        }

        public override string GetLightFunction()
        {
            return LightFunctionName;
        }
    }*/
}

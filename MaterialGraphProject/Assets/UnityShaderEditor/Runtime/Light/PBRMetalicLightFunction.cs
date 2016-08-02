using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class PBRMetalicLightFunction : BaseLightFunction
    {
        public const string AlbedoSlotName = "Albedo";
        public const string MetallicSlotName = "Metallic";
        public const string EmissionSlotName = "Emission";
        public const string SmoothnessSlotName = "Smoothness";
        public const string OcclusionSlotName = "Occlusion";
        public const string AlphaSlotName = "Alpha";

        public const string LightFunctionName =  "Standard";
        public const string SurfaceOutputStructureName = "SurfaceOutputStandard";

        public const int AlbedoSlotId = 0;
        public const int MetallicSlotId = 2;
        public const int EmissionSlotId = 3;
        public const int SmoothnessSlotId = 4;
        public const int OcclusionSlotId = 5;
        public const int AlphaSlotId = 6;

        public override string lightFunctionName
        {
            get { return LightFunctionName; }
        }

        public override string surfaceOutputStructureName
        {
            get { return SurfaceOutputStructureName; }
        }

        public override void DoSlotsForConfiguration(PixelShaderNode node)
        {
            node.AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(NormalSlotId, kNormalSlotName, kNormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

            // clear out slot names that do not match the slots
            // we support
            node.RemoveSlotsNameNotMatching(
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
    }
}

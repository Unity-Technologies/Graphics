using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Metallic")]
    public class MetallicMasterNode : AbstractSurfaceMasterNode
    {
        public const string MetallicSlotName = "Metallic";
        public const int MetallicSlotId = 2;

        public const string LightFunctionName = "Standard";
        public const string SurfaceOutputStructureName = "SurfaceOutputStandard";

        public MetallicMasterNode()
        {
            name = "MetallicMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
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
                                           MetallicSlotId,
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
    }
}

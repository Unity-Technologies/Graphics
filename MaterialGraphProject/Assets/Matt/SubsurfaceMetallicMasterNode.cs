using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/SubsurfaceMetallic")]
    public class SubsurfaceMetallicMasterNode : AbstractAdvancedMasterNode
    {
        public const string MetallicSlotName = "Metallic";
        public const int MetallicSlotId = 2;

        public const string AnisotropySlotName = "Anisotropy";
        public const int AnisotropySlotId = 8;
        public const string TranslucencySlotName = "Translucency";
        public const int TranslucencySlotId = 9;

        public const string LightFunctionName = "Advanced";
        public const string SurfaceOutputStructureName = "SurfaceOutputAdvanced";

        public SubsurfaceMetallicMasterNode()
        {
            name = "SubsurfaceMetallic";
            UpdateNodeAfterDeserialization();
        }

        public override string GetMaterialID()
        {
            return "SHADINGMODELID_SUBSURFACE";
        }

        public override bool RequireTangentCalculation()
        {
            return true;
        }

        public override int[] GetCustomDataSlots()
        {
            return new int[] { 9 };
        }

        public override string[] GetCustomData()
        {
            string translucencyInput = GetVariableNameForSlotAtId(9);
            if (translucencyInput == "")
                translucencyInput = "float3(0,0,0)";
            else
                translucencyInput = translucencyInput + ".rgb";
            return new string[] { "o.CustomData = float4(" + translucencyInput + ", 0);" };
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(AnisotropySlotId, AnisotropySlotName, AnisotropySlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(TranslucencySlotId, TranslucencySlotName, TranslucencySlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));

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
                                           AlphaSlotId,
                                           TangentSlotId,
                                           AnisotropySlotId,
                                           TranslucencySlotId
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

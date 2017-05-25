using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Cloth")]
    public class ClothMasterNode : AbstractAdvancedMasterNode
    {
        //public const string MetallicSlotName = "Metallic";
        //public const int MetallicSlotId = 2;

        public const string AnisotropySlotName = "Anisotropy";
        public const int AnisotropySlotId = 8;
        public const string FuzzSlotName = "Fuzz";
        public const int FuzzSlotId = 9;
        public const string ClothFactorSlotName = "ClothFactor";
        public const int ClothFactorSlotId = 10;

        public const string LightFunctionName = "Advanced";
        public const string SurfaceOutputStructureName = "SurfaceOutputAdvanced";

        public ClothMasterNode()
        {
            name = "Cloth";
            UpdateNodeAfterDeserialization();
        }

        public override string GetMaterialID()
        {
            return "SHADINGMODELID_CLOTH";
        }

        public override bool RequireTangentCalculation()
        {
            return true;
        }

        public override int[] GetCustomDataSlots()
        {
            return new int[] { 9, 10 };
        }

        public override string[] GetCustomData()
        {
            string fuzzInput = GetVariableNameForSlotAtId(9);
            if (fuzzInput == "")
                fuzzInput = "float3(0,0,0)";
            else
                fuzzInput = fuzzInput + ".rgb";
            string clothFactorInput = GetVariableNameForSlotAtId(10);
            if (clothFactorInput == "")
                clothFactorInput = "0";
            else
                clothFactorInput = clothFactorInput + ".r";
            return new string[] { "o.CustomData = float4(" + fuzzInput + ", "+ clothFactorInput + ");" };
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            //AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(AnisotropySlotId, AnisotropySlotName, AnisotropySlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(FuzzSlotId, FuzzSlotName, FuzzSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(ClothFactorSlotId, ClothFactorSlotName, ClothFactorSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                                       new[]
                                       {
                                           AlbedoSlotId,
                                           NormalSlotId,
                                           EmissionSlotId,
                                           //MetallicSlotId,
                                           SmoothnessSlotId,
                                           OcclusionSlotId,
                                           AlphaSlotId,
                                           TangentSlotId,
                                           AnisotropySlotId,
                                           FuzzSlotId,
                                           ClothFactorSlotId
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

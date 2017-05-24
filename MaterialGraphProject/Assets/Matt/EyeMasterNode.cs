using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Eye")]
    public class EyeMasterNode : AbstractAdvancedMasterNode
    {
        //public const string MetallicSlotName = "Metallic";
        //public const int MetallicSlotId = 2;

        public const string LightFunctionName = "Advanced";
        public const string SurfaceOutputStructureName = "SurfaceOutputAdvanced";

        public const string IrisMaskSlotName = "IrisMask";
        public const int IrisMaskSlotId = 8;

        public const string IrisDistanceSlotName = "IrisDistance";
        public const int IrisDistanceSlotId = 9;

        public EyeMasterNode()
        {
            name = "EyeMaster";
            UpdateNodeAfterDeserialization();
        }

        public override string GetMaterialID()
        {
            return "SHADINGMODELID_EYE";
        }

        public override int[] GetCustomDataSlots()
        {
            return new int[] { 8, 9 };
        }

        public override string[] GetCustomData()
        {
            string tangentInput = GetVariableNameForSlotAtId(7);
            if (tangentInput == "")
                tangentInput = "float2(1,0)";
            else
                tangentInput = tangentInput + ".rg";
            string irisMaskInput = GetVariableNameForSlotAtId(8);
            if (irisMaskInput == "")
                irisMaskInput = "0";
            else
                irisMaskInput = irisMaskInput + ".r";
            string irisDistanceInput = GetVariableNameForSlotAtId(9);
            if (irisDistanceInput == "")
                irisDistanceInput = "0";
            else
                irisDistanceInput = irisDistanceInput + ".r";
            return new string[] { "o.CustomData = float4(" + tangentInput + ", " + irisMaskInput + ", " + irisDistanceInput + ");" };
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            //AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, SlotType.Input, SlotValueType.Vector2, Vector4.zero));
            AddSlot(new MaterialSlot(IrisMaskSlotId, IrisMaskSlotName, IrisMaskSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(IrisDistanceSlotId, IrisDistanceSlotName, IrisDistanceSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

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
                                           IrisMaskSlotId,
                                           IrisDistanceSlotId
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

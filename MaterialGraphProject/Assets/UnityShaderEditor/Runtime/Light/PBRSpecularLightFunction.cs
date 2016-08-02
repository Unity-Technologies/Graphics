using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class PBRSpecularLightFunction : BaseLightFunction
    {
        public const string kAlbedoSlotName = "Albedo";
        public const string kSpecularSlotName = "Specular";
        public const string kEmissionSlotName = "Emission";
        public const string kSmoothnessSlotName = "Smoothness";
        public const string kOcclusionSlotName = "Occlusion";
        public const string kAlphaSlotName = "Alpha";
        
        public const int kAlbedoSlotId = 0;
        public const int kSpecularSlotId = 2;
        public const int kEmissionSlotId = 3;
        public const int kSmoothnessSlotId = 4;
        public const int kOcclusionSlotId = 5;
        public const int kAlphaSlotId = 6;

        public override string lightFunctionName
        {
            get { return "StandardSpecular"; }
        }

        public override string surfaceOutputStructureName
        {
            get { return "SurfaceOutputStandardSpecular"; }
        }

        public override void DoSlotsForConfiguration(PixelShaderNode node)
        {
            node.AddSlot(new MaterialSlot(kAlbedoSlotId, kAlbedoSlotName, kAlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(NormalSlotId, kNormalSlotName, kNormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kSpecularSlotId, kSpecularSlotName, kSpecularSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kEmissionSlotId, kEmissionSlotName, kEmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kSmoothnessSlotId, kSmoothnessSlotName, kSmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(kOcclusionSlotId, kOcclusionSlotName, kOcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(kAlphaSlotId, kAlphaSlotName, kAlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

            // clear out slot names that do not match the slots 
            // we support
            node.RemoveSlotsNameNotMatching(
                                            new[]
                                            {
                                                kAlbedoSlotId,
                                                NormalSlotId,
                                                kSpecularSlotId,
                                                kEmissionSlotId,
                                                kSmoothnessSlotId,
                                                kOcclusionSlotId,
                                                kAlphaSlotId
                                            });
        }
    }
}

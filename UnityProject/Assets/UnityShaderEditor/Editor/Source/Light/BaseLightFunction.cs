using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public abstract class BaseLightFunction
    {
        public const string kNormalSlotName = "Normal";

        public virtual string GetLightFunctionName() { return ""; }
        public virtual string GetSurfaceOutputStructureName() { return ""; }
        public virtual void GenerateLightFunctionBody(ShaderGenerator visitor) {}

        public virtual void GenerateLightFunctionName(ShaderGenerator visitor)
        {
            visitor.AddPragmaChunk(GetLightFunctionName());
        }

        public virtual void GenerateSurfaceOutputStructureName(ShaderGenerator visitor)
        {
            visitor.AddPragmaChunk(GetSurfaceOutputStructureName());
        }

        public abstract void DoSlotsForConfiguration(PixelShaderNode node);

        public virtual string GetFirstPassSlotName()
        {
            return kNormalSlotName;
        }
    }

    class PBRMetalicLightFunction : BaseLightFunction
    {
        public const string kAlbedoSlotName = "Albedo";
        public const string kEmissionSlotName = "Emission";
        public const string kMetallicSlotName = "Metallic";
        public const string kSmoothnessSlotName = "Smoothness";
        public const string kOcclusion = "Occlusion";
        public const string kAlphaSlotName = "Alpha";

        public override string GetLightFunctionName() { return "Standard"; }
        public override string GetSurfaceOutputStructureName() { return "SurfaceOutputStandard"; }
        public override void DoSlotsForConfiguration(PixelShaderNode node)
        {
            node.AddSlot(new Slot(node.guid, kAlbedoSlotName, kAlbedoSlotName, Slot.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kNormalSlotName, kNormalSlotName, Slot.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kMetallicSlotName, kMetallicSlotName, Slot.SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kSmoothnessSlotName, kSmoothnessSlotName, Slot.SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kOcclusion, kOcclusion, Slot.SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kAlphaSlotName, kAlphaSlotName, Slot.SlotType.Input, SlotValueType.Vector1, Vector4.zero));
          
            // clear out slot names that do not match the slots 
            // we support
            node.RemoveSlotsNameNotMatching(
                new[]
                {
                    kAlbedoSlotName,
                    kNormalSlotName,
                    kEmissionSlotName,
                    kMetallicSlotName,
                    kSmoothnessSlotName,
                    kOcclusion,
                    kAlphaSlotName
                });
        }
    }

    class PBRSpecularLightFunction : BaseLightFunction
    {
        public const string kAlbedoSlotName = "Albedo";
        public const string kSpecularSlotName = "Specular";
        public const string kEmissionSlotName = "Emission";
        public const string kSmoothnessSlotName = "Smoothness";
        public const string kOcclusion = "Occlusion";
        public const string kAlphaSlotName = "Alpha";

        public override string GetLightFunctionName() { return "StandardSpecular"; }
        public override string GetSurfaceOutputStructureName() { return "SurfaceOutputStandardSpecular"; }
        public override void DoSlotsForConfiguration(PixelShaderNode node)
        {
            node.AddSlot(new Slot(node.guid, kAlbedoSlotName, kAlbedoSlotName, Slot.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kNormalSlotName, kNormalSlotName, Slot.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kSpecularSlotName, kSpecularSlotName, Slot.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kEmissionSlotName, kEmissionSlotName, Slot.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kSmoothnessSlotName, kSmoothnessSlotName, Slot.SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kOcclusion, kOcclusion, Slot.SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new Slot(node.guid, kAlphaSlotName, kAlphaSlotName, Slot.SlotType.Input, SlotValueType.Vector1, Vector4.zero));

            // clear out slot names that do not match the slots 
            // we support
            node.RemoveSlotsNameNotMatching(
                new[]
                {
                    kAlbedoSlotName,
                    kNormalSlotName,
                    kSpecularSlotName,
                    kEmissionSlotName,
                    kSmoothnessSlotName,
                    kOcclusion,
                    kAlphaSlotName
                });
        }
    }
}

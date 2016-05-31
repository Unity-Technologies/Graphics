
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
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
            node.AddSlot(new MaterialSlot(kAlbedoSlotName, kAlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kNormalSlotName, kNormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kMetallicSlotName, kMetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(kSmoothnessSlotName, kSmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(kOcclusion, kOcclusion, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(kAlphaSlotName, kAlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
          
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
            node.AddSlot(new MaterialSlot(kAlbedoSlotName, kAlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kNormalSlotName, kNormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kSpecularSlotName, kSpecularSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kEmissionSlotName, kEmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            node.AddSlot(new MaterialSlot(kSmoothnessSlotName, kSmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(kOcclusion, kOcclusion, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            node.AddSlot(new MaterialSlot(kAlphaSlotName, kAlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    public abstract class BaseLightFunction
    {
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

        public virtual IEnumerable<Slot> FilterSlots(List<Slot> slots)
        {
            return new List<Slot>();
        }
    }

    class PBRMetalicLightFunction : BaseLightFunction
    {
        public override string GetLightFunctionName() { return "Standard"; }
        public override string GetSurfaceOutputStructureName() { return "SurfaceOutputStandard"; }

        public override IEnumerable<Slot> FilterSlots(List<Slot> slots)
        {
            var rSlots =  new List<Slot>();
            foreach (var slot in slots)
            {
                switch (slot.name)
                {
                    case PixelShaderNode.kAlbedoSlotName:
                    case PixelShaderNode.kNormalSlotName:
                    case PixelShaderNode.kEmissionSlotName:
                    case PixelShaderNode.kMetallicSlotName:
                    case PixelShaderNode.kSmoothnessSlotName:
                    case PixelShaderNode.kOcclusion:
                    case PixelShaderNode.kAlphaSlotName:
                        rSlots.Add(slot);
                        break;
                }
            }
            return rSlots;
        }
    }

    class PBRSpecularLightFunction : BaseLightFunction
    {
        public override string GetLightFunctionName() { return "StandardSpecular"; }
        public override string GetSurfaceOutputStructureName() { return "SurfaceOutputStandardSpecular"; }

        public override IEnumerable<Slot> FilterSlots(List<Slot> slots)
        {
            var rSlots =  new List<Slot>();
            foreach (var slot in slots)
            {
                switch (slot.name)
                {
                    case PixelShaderNode.kAlbedoSlotName:
                    case PixelShaderNode.kSpecularSlotName:
                    case PixelShaderNode.kNormalSlotName:
                    case PixelShaderNode.kEmissionSlotName:
                    case PixelShaderNode.kSmoothnessSlotName:
                    case PixelShaderNode.kOcclusion:
                    case PixelShaderNode.kAlphaSlotName:
                        rSlots.Add(slot);
                        break;
                }
            }
            return rSlots;
        }
    }
}

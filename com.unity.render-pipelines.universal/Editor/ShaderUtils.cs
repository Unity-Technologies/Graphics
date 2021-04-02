using System;
using UnityEngine;
using UnityEditor;
using ShaderPathID = UnityEngine.Rendering.Universal.ShaderPathID;
using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.Universal.ShaderGraph;

namespace Unity.Rendering.Universal // Unity.RenderPipelines.Universal.Editor
{
    public static class ShaderUtils
    {
        internal enum ShaderID
        {
            Unknown = -1,

            Lit = ShaderPathID.Lit,
            SimpleLit = ShaderPathID.SimpleLit,
            Unlit = ShaderPathID.Unlit,
            TerrainLit = ShaderPathID.TerrainLit,
            ParticlesLit = ShaderPathID.ParticlesLit,
            ParticlesSimpleLit = ShaderPathID.ParticlesSimpleLit,
            ParticlesUnlit = ShaderPathID.ParticlesUnlit,
            BakedLit = ShaderPathID.BakedLit,
            SpeedTree7 = ShaderPathID.SpeedTree7,
            SpeedTree7Billboard = ShaderPathID.SpeedTree7Billboard,
            SpeedTree8 = ShaderPathID.SpeedTree8,

            // ShaderGraph IDs start at 1000, correspond to subtargets
            SG_Unlit = 1000,        // UniversalUnlitSubTarget
            SG_Lit,                 // UniversalLitSubTarget
        }

        internal static ShaderID GetShaderID(Shader shader)
        {
            // TODO: this won't work for non-Asset shaders...  luckily I think that's the only kind we care about versioning properly..
            if (shader.IsShaderGraphAsset())
            {
                UniversalMetadata meta;
                if (!shader.TryGetMetadataOfType<UniversalMetadata>(out meta))
                    return ShaderID.Unknown;
                return meta.shaderID;
            }
            else
            {
                ShaderPathID pathID = UnityEngine.Rendering.Universal.ShaderUtils.GetEnumFromPath(shader.name);
                return (ShaderID)pathID;
            }
        }

        public static void ResetMaterialKeywords(Material material)
        {
            var sgTargetId = material.GetTag("ShaderGraphTargetId", false, null);
            if (sgTargetId == "UniversalLitSubTarget")
            {
                URPLitGUI.UpdateMaterial(material);
            }
            else if (sgTargetId == "UniversalUnlitSubTarget")
            {
                URPUnlitGUI.UpdateMaterial(material);
            }
            else
            {
                ShaderID shaderID = GetShaderID(material.shader);
                switch (shaderID)
                {
                    case ShaderID.SG_Lit:
                        URPLitGUI.UpdateMaterial(material);
                        break;
                    case ShaderID.SG_Unlit:
                        URPUnlitGUI.UpdateMaterial(material);
                        break;
                    default:
                        // TODO: handle non shadergraph shaders here, if they need keyword resets
                        Debug.LogWarning("Unhandled material in ResetMaterialKeywords: " + material.name + " of TargetID: " + sgTargetId + " ShaderID: " + shaderID);
                        break;
//                    case ShaderID.Unknown:
//                        break;
                }
            }
        }
    }
}

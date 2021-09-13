using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.BuiltIn
{
    public static class ShaderUtils
    {
        internal enum ShaderID
        {
            Unknown = -1,

            // ShaderGraph IDs start at 1000, correspond to subtargets
            SG_Start = 1000,
            SG_Unlit = SG_Start,        // BuiltInUnlitSubTarget
            SG_Lit,                 // BuiltInLitSubTarget
        }

        internal static bool IsShaderGraph(this ShaderID id)
        {
            return (id >= ShaderID.SG_Start);
        }

        internal static ShaderID GetShaderID(Shader shader)
        {
            if (shader.IsShaderGraphAsset())
            {
                BuiltInMetadata meta;
                if (!shader.TryGetMetadataOfType<BuiltInMetadata>(out meta))
                    return ShaderID.Unknown;
                return meta.shaderID;
            }
            else
            {
                return ShaderID.Unknown;
            }
        }

        internal static void ResetMaterialKeywords(Material material, ShaderID shaderID = ShaderID.Unknown)
        {
            // if unknown, look it up from the material's shader
            // NOTE: this will only work for asset-based shaders..
            if (shaderID == ShaderID.Unknown)
                shaderID = GetShaderID(material.shader);

            switch (shaderID)
            {
                case ShaderID.SG_Lit:
                    BuiltInLitGUI.UpdateMaterial(material);
                    break;
                case ShaderID.SG_Unlit:
                    BuiltInUnlitGUI.UpdateMaterial(material);
                    break;
            }
        }
    }
}

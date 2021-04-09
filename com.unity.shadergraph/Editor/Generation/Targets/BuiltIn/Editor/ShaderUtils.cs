using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;

namespace Unity.Rendering.BuiltIn // Unity.RenderPipelines.BuiltIn.Editor
{

    //using ShaderPathID = UnityEngine.Rendering.BuiltIn.ShaderPathID;

    public static class ShaderUtils
    {
        internal enum ShaderID
        {
            Unknown = -1,

            // ShaderGraph IDs start at 1000, correspond to subtargets
            SG_Unlit = 1000,        // BuiltInUnlitSubTarget
            SG_Lit,                 // BuiltInLitSubTarget
        }

        internal static ShaderID GetShaderID(Shader shader)
        {
            // TODO:
            // this won't work for non-Asset shaders...  luckily I think that's the only kind we care about versioning properly..
            if (shader.IsShaderGraph())
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

        public static void ResetMaterialKeywords(Material material)
        {
            var sgTargetId = material.GetTag("ShaderGraphTargetId", false, null);
            if (sgTargetId == "BuiltInLitSubTarget")
            {
                BuiltInLitGUI.UpdateMaterial(material);
            }
            else if (sgTargetId == "BuiltInUnlitSubTarget")
            {
                BuiltInUnlitGUI.UpdateMaterial(material);
            }
            else
            {
                ShaderID shaderID = GetShaderID(material.shader);
                switch (shaderID)
                {
                    case ShaderID.SG_Lit:
                        BuiltInLitGUI.UpdateMaterial(material);
                        break;
                    case ShaderID.SG_Unlit:
                        BuiltInUnlitGUI.UpdateMaterial(material);
                        break;
                    // TODO: handle other shaders that need keyword resets here
                    default:
                        break;
                }
            }
        }
    }
}

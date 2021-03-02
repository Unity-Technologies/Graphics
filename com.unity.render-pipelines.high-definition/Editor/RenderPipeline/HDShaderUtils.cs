using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Utility class for shaders.
    /// </summary>
    public class HDShaderUtils
    {
        //enum representing all shader and shadergraph that we expose to user
        internal enum ShaderID
        {
            Lit,
            LitTesselation,
            LayeredLit,
            LayeredLitTesselation,
            Unlit,
            Decal,
            TerrainLit,
            AxF,
            Count_Standard,
            SG_Unlit = Count_Standard,
            SG_Lit,
            SG_Hair,
            SG_Fabric,
            SG_StackLit,
            SG_Decal,
            SG_Eye,
            Count_All,
            Count_ShaderGraph = Count_All - Count_Standard
        }

        // exposed shader, for reference while searching the ShaderID
        static readonly string[] s_ShaderPaths =
        {
            "HDRP/Lit",
            "HDRP/LitTessellation",
            "HDRP/LayeredLit",
            "HDRP/LayeredLitTessellation",
            "HDRP/Unlit",
            "HDRP/Decal",
            "HDRP/TerrainLit",
            "HDRP/AxF",
        };

        // list of methods for resetting keywords
        delegate void MaterialResetter(Material material);
        static Dictionary<ShaderID, MaterialResetter> k_MaterialResetters = new Dictionary<ShaderID, MaterialResetter>()
        {
            { ShaderID.Lit, LitGUI.SetupLitKeywordsAndPass },
            { ShaderID.LitTesselation, LitGUI.SetupLitKeywordsAndPass },
            { ShaderID.LayeredLit,  LayeredLitGUI.SetupLayeredLitKeywordsAndPass },
            { ShaderID.LayeredLitTesselation, LayeredLitGUI.SetupLayeredLitKeywordsAndPass },
            // no entry for ShaderID.StackLit
            { ShaderID.Unlit, UnlitGUI.SetupUnlitKeywordsAndPass },
            // no entry for ShaderID.Fabric
            { ShaderID.Decal, DecalUI.SetupDecalKeywordsAndPass },
            { ShaderID.TerrainLit, TerrainLitGUI.SetupTerrainLitKeywordsAndPass },
            { ShaderID.AxF, AxFGUI.SetupAxFKeywordsAndPass },
            { ShaderID.SG_Unlit, UnlitShaderGraphGUI.SetupUnlitKeywordsAndPass },
            { ShaderID.SG_Lit, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_Hair, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_Fabric, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_StackLit, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_Decal, DecalShaderGraphGUI.SetupDecalKeywordsAndPass },
            // no entry for ShaderID.SG_Decal
            // no entry for ShaderID.SG_Eye
        };

        /// <summary>
        /// Reset the dedicated Keyword and Pass regarding the shader kind.
        /// Also re-init the drawers and set the material dirty for the engine.
        /// </summary>
        /// <param name="material">The material that needs to be setup</param>
        /// <returns>
        /// True: managed to do the operation.
        /// False: unknown shader used in material
        /// </returns>
        public static bool ResetMaterialKeywords(Material material)
        {
            MaterialResetter resetter;

            // If we send a non HDRP material we don't throw an exception, the return type already handles errors.
            try
            {
                k_MaterialResetters.TryGetValue(GetShaderEnumFromShader(material.shader), out resetter);
            }
            catch
            {
                return false;
            }

            if (resetter != null)
            {
                CoreEditorUtils.RemoveMaterialKeywords(material);
                // We need to reapply ToggleOff/Toggle keyword after reset via ApplyMaterialPropertyDrawers
                MaterialEditor.ApplyMaterialPropertyDrawers(material);
                resetter(material);
                EditorUtility.SetDirty(material);
                return true;
            }

            return false;
        }

        /// <summary>Gather all the shader preprocessors</summary>
        /// <returns>The list of shader preprocessor</returns>
        internal static List<BaseShaderPreprocessor> GetBaseShaderPreprocessorList()
            => UnityEngine.Rendering.CoreUtils
            .GetAllTypesDerivedFrom<BaseShaderPreprocessor>()
            .Select(Activator.CreateInstance)
            .Cast<BaseShaderPreprocessor>()
            .OrderByDescending(spp => spp.Priority)
            .ToList();

        internal static bool IsHDRPShader(Shader shader, bool upgradable = false)
        {
            if (shader == null)
                return false;

            if (shader.IsShaderGraph())
            {
                // All HDRP shader graphs should have HD metadata
                return shader.TryGetMetadataOfType<HDMetadata>(out _);
            }
            else if (upgradable)
                return s_ShaderPaths.Contains(shader.name);
            else
                return shader.name.Contains("HDRP");
        }

        internal static bool IsUnlitHDRPShader(Shader shader)
        {
            if (shader == null)
                return false;

            if (shader.IsShaderGraph())
            {
                // Throw exception if no metadata is found
                // This case should be handled by the Target
                HDMetadata obj;
                if (!shader.TryGetMetadataOfType<HDMetadata>(out obj))
                    throw new ArgumentException("Unknown shader");

                return obj.shaderID == ShaderID.SG_Unlit;
            }
            else
                return shader.name == "HDRP/Unlit";
        }

        internal static string GetShaderPath(ShaderID id)
        {
            int index = (int)id;
            if (index < 0 && index >= (int)ShaderID.Count_Standard)
            {
                Debug.LogError("Trying to access HDRP shader path out of bounds");
                return "";
            }

            return s_ShaderPaths[index];
        }

        internal static ShaderID GetShaderEnumFromShader(Shader shader)
        {
            if (shader.IsShaderGraph())
            {
                // Throw exception if no metadata is found
                // This case should be handled by the Target
                HDMetadata obj;
                if (!shader.TryGetMetadataOfType<HDMetadata>(out obj))
                    throw new ArgumentException("Unknown shader");

                return obj.shaderID;
            }
            else
            {
                var index = Array.FindIndex(s_ShaderPaths, m => m == shader.name);
                if (index == -1)
                    throw new ArgumentException("Unknown shader");
                return (ShaderID)index;
            }
        }
    }
}

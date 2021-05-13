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
            Count_ShaderGraph = Count_All - Count_Standard,
            SG_External = -1, // material packaged outside of HDRP
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
        internal delegate void MaterialResetter(Material material);
        static Dictionary<ShaderID, MaterialResetter> k_PlainShadersMaterialResetters = new Dictionary<ShaderID, MaterialResetter>()
        {
            { ShaderID.Lit, LitGUI.SetupLitKeywordsAndPass },
            { ShaderID.LitTesselation, LitGUI.SetupLitKeywordsAndPass },
            { ShaderID.LayeredLit,  LayeredLitGUI.SetupLayeredLitKeywordsAndPass },
            { ShaderID.LayeredLitTesselation, LayeredLitGUI.SetupLayeredLitKeywordsAndPass },
            { ShaderID.Unlit, UnlitGUI.SetupUnlitKeywordsAndPass },
            { ShaderID.Decal, DecalUI.SetupDecalKeywordsAndPass },
            { ShaderID.TerrainLit, TerrainLitGUI.SetupTerrainLitKeywordsAndPass },
            { ShaderID.AxF, AxFGUI.SetupAxFKeywordsAndPass },
            // TODO: These need to stay until all SG materials are reimported and resaved (so that HDMetaData contains the m_SubTargetGuidString)
            { ShaderID.SG_Unlit, UnlitShaderGraphGUI.SetupUnlitKeywordsAndPass },
            { ShaderID.SG_Lit, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_Hair, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_Fabric, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_StackLit, LightingShaderGraphGUI.SetupLightingKeywordsAndPass },
            { ShaderID.SG_Decal, DecalShaderGraphGUI.SetupDecalKeywordsAndPass },
            { ShaderID.SG_Eye, LightingShaderGraphGUI.SetupLightingKeywordsAndPass }
        };

        // List of all discovered HDSubTargets
        // Should be refreshed on assembly reload so no need to poll (cf MaterialPostProcessor's polling)
        static List<HDSubTarget> k_HDSubTargets = new List<HDSubTarget>(
            UnityEngine.Rendering.CoreUtils
                .GetAllTypesDerivedFrom<HDSubTarget>()
                .Where(type => (!type.IsAbstract && type.IsClass))
                .Select(Activator.CreateInstance)
                .Cast<HDSubTarget>()
                //.Where(subTarget => subTarget.IsExternalPlugin())
                .ToList());

        // Map from HDMetaData subTarget GUID to its MaterialResetter function
        static Dictionary<GUID, MaterialResetter> k_HDSubTargetsMaterialResetters =
            k_HDSubTargets.ToDictionary(subTarget => subTarget.subTargetGuid, subTarget => subTarget.setupMaterialKeywordsAndPassFunc);
        // Note: could autogenerate GUID from ShaderID using namespace guids (like SG JsonObject does), and this would permit us to merge the resetters dictionaries into one.

        // Map from HDMetaData subTarget GUID to interface to access latest version and migration functions
        // of materials using plugin-subtargets shaders
        static Dictionary<GUID, IPluginSubTargetMaterialUtils> k_HDPluginSubTargets =
            k_HDSubTargets.Where(x => x is IPluginSubTargetMaterialUtils).ToDictionary(subTarget => subTarget.subTargetGuid, subTarget => ((IPluginSubTargetMaterialUtils)subTarget));

        // To accelerate MaterialProcessor polling of project packages/code changes/updates, we track
        // the sum of all present plugin material latest versions (that a plugin SubTarget "advertizes" through IPluginSubTargetMaterialUtils),
        // such that the project HDProjectSettings will do the same, with the precondition that each last seen versions in HDProjectSettings
        // should never be allowed to be higher than the currently present plugin SubTarget's latestMaterialVersion
        // (otherwise the sums of HDProjectSettings vs code base can't reliably be compared). This could happen if we downgrade the codebase
        // of a plugin SubTarget, which is not supported anyway.
        static long pluginSubTargetMaterialVersionsSum =
            k_HDPluginSubTargets.Count > 0 ? k_HDPluginSubTargets.Sum(pair => (long)pair.Value.latestMaterialVersion) : (long)PluginMaterial.GenericVersions.NeverMigrated;

        static long pluginSubTargetVersionsSum =
            k_HDPluginSubTargets.Count > 0 ? k_HDPluginSubTargets.Sum(pair => (long)pair.Value.latestSubTargetVersion) : (long)PluginMaterial.GenericVersions.NeverMigrated;

        /// <summary>
        /// Checks if a SubTarget GUID of a shadegraph shader used by a material correspond to a plugin SubTarget.
        /// If so, also returns that plugin material interface giving access to its latest version and material migration.
        /// </summary>
        /// <param name="pluginMaterialGUID">The SubTarget GUID (<see cref="GetShaderIDsFromShader"/>)</param>
        /// <param name="subTargetMaterialUtils">The interface from which to get latest version and material migration function for that SubTarget</param>
        /// <returns>
        /// True: The GUID matches a found plugin SubTarget and the subTargetMaterialUtils interface is found.
        /// False: Unknown plugin SubTarget GUID.
        /// </returns>
        public static bool GetMaterialPluginSubTarget(GUID pluginMaterialGUID, out IPluginSubTargetMaterialUtils subTargetMaterialUtils)
        {
            try
            {
                k_HDPluginSubTargets.TryGetValue(pluginMaterialGUID, out subTargetMaterialUtils);
            }
            catch
            {
                subTargetMaterialUtils = null;
            }
            return (subTargetMaterialUtils != null);
        }

        internal static Dictionary<GUID, IPluginSubTargetMaterialUtils> GetHDPluginSubTargets()
        {
            return k_HDPluginSubTargets;
        }

        internal static long GetHDPluginSubTargetMaterialVersionsSum()
        {
            return pluginSubTargetMaterialVersionsSum;
        }

        internal static long GetHDPluginSubTargetVersionsSum()
        {
            return pluginSubTargetVersionsSum;
        }

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
            var shaderID = GetShaderEnumFromShader(material.shader);
            return ResetMaterialKeywords(material, shaderID);
        }

        internal static bool ResetMaterialKeywords(Material material, ShaderID shaderId)
        {
            MaterialResetter resetter;

            (ShaderID id, GUID extMaterialGUID) = GetShaderIDsFromShader(material.shader);
            // If we send a non HDRP material we don't throw an exception, the return type already handles errors.
            try
            {
                k_PlainShadersMaterialResetters.TryGetValue(id, out resetter);
                if (resetter == null)
                    k_HDSubTargetsMaterialResetters.TryGetValue(extMaterialGUID, out resetter);
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

        internal static bool IsHDRPShaderGraph(Shader shader)
        {
            if (shader == null)
                return false;

            if (shader.IsShaderGraphAsset())
            {
                // All HDRP shader graphs should have HD metadata
                return shader.TryGetMetadataOfType<HDMetadata>(out _);
            }
            return false;
        }

        internal static bool IsHDRPShader(Shader shader, bool upgradable = false)
        {
            if (shader == null)
                return false;

            if (shader.IsShaderGraphAsset())
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

            if (shader.IsShaderGraphAsset())
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

        internal static (ShaderID, GUID) GetShaderIDsFromShader(Shader shader)
        {
            if (shader.IsShaderGraphAsset())
            {
                // Throw exception if no metadata is found
                // This case should be handled by the Target
                HDMetadata obj;
                if (!shader.TryGetMetadataOfType<HDMetadata>(out obj))
                    throw new ArgumentException("Unknown shader");

                return (obj.shaderID, obj.subTargetGuid);
            }
            else
            {
                var index = Array.FindIndex(s_ShaderPaths, m => m == shader.name);
                if (index == -1)
                    throw new ArgumentException("Unknown shader");
                return ((ShaderID)index, new GUID());
            }
        }
    }
}

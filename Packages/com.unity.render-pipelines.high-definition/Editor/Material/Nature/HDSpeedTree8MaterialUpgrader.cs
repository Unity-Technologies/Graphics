using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// SpeedTree 8 material upgrader for HDRP.
    /// </summary>
    class HDSpeedTree8MaterialUpgrader : SpeedTree8MaterialUpgrader
    {
        static class HDUniforms
        {
            
            internal static int _WINDQUALITY = Shader.PropertyToID("_WINDQUALITY");
            internal static int EFFECT_BILLBOARD = Shader.PropertyToID("EFFECT_BILLBOARD");
            internal static int EFFECT_EXTRA_TEX = Shader.PropertyToID("EFFECT_EXTRA_TEX");
            internal static int _TwoSided = Shader.PropertyToID("_TwoSided");
            internal static int _WindQuality = Shader.PropertyToID("_WindQuality");
            internal static int _DoubleSidedEnable = Shader.PropertyToID("_DoubleSidedEnable");
            internal static int _CullMode = Shader.PropertyToID("_CullMode");
            internal static int _CullModeForward = Shader.PropertyToID("_CullModeForward");
            internal static int Diffusion_Profile_Asset = Shader.PropertyToID("Diffusion_Profile_Asset");
            internal static int Diffusion_Profile = Shader.PropertyToID("Diffusion_Profile");
        }

        /// <summary>
        /// Creates a SpeedTree 8 material upgrader for HDRP.
        /// </summary>
        /// <param name="sourceShaderName">Original shader name.</param>
        /// <param name="destShaderName">Upgraded shader name.</param>
        public HDSpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName)
            : base(sourceShaderName, destShaderName, HDSpeedTree8MaterialFinalizer)
        {
            RenameKeywordToFloat("EFFECT_BILLBOARD", "_DoubleSidedEnable", 0, 1);
            RenameFloat("_TwoSided", "_CullMode");
            RenameFloat("_TwoSided", "_CullModeForward");
        }

        public static void HDSpeedTree8MaterialFinalizer(Material mat)
        {
            SetHDSpeedTree8Defaults(mat);

            // Need to call this again after reconfiguring keyword toggles
            HDShaderUtils.ResetMaterialKeywords(mat);
        }

        /// <summary>
        /// Determines if a given material is using the default SpeedTree 8 shader for HDRP.
        /// </summary>
        /// <param name="mat">Material to check for an HDRP-compatible SpeedTree 8 shader.</param>
        /// <returns></returns>
        public static bool IsHDSpeedTree8Material(Material mat)
        {
            return (mat.shader.name == "HDRP/Nature/SpeedTree8");
        }

        /// <summary>
        /// (Obsolete) HDRP may reset SpeedTree-specific keywords which should not be modified. This method restores these keywords to their original state.
        /// </summary>
        /// <param name="mat">SpeedTree 8 material.</param>
        [System.Obsolete("No longer needed from 21.2 onwards.")]
        public static void RestoreHDSpeedTree8Keywords(Material mat)
        {
            // Since ShaderGraph now supports toggling keywords via float properties, keywords get
            // correctly restored by default and this function is no longer needed.
        }

        // Should match HDRenderPipelineEditorResources.defaultDiffusionProfileSettingsList[foliageIdx]
        private const string kFoliageDiffusionProfilePath = "Runtime/RenderPipelineResources/FoliageDiffusionProfile.asset";
        // Should match HDRenderPipelineEditorResources.defaultDiffusionProfileSettingsList[foliageIdx].name
        private const string kDefaultDiffusionProfileName = "Foliage";
        private static void SetHDSpeedTree8Defaults(Material mat)
        {
            SetupHDPropertiesOnImport(mat);
            SetDefaultDiffusionProfile(mat);
        }

        private static void SetupHDPropertiesOnImport(Material mat)
        {
            // When importing a new SpeedTree8, upgrade/convert has not run beforehand,
            // so setup HD-specific properties based on builtin configuration.
            // TODO: Only do this if called during OnPostprocessSpeedTree.

            // Since _DoubleSidedEnable controls _CullMode in HD,
            // disable it for billboard LOD.
            float doubleSided = mat.GetFloat(HDUniforms.EFFECT_BILLBOARD) > 0 ? 0.0f : 1.0f;
            mat.SetFloat(HDUniforms._DoubleSidedEnable, doubleSided);

            if (mat.HasFloat(HDUniforms._TwoSided))
            {
                mat.SetFloat(HDUniforms._CullMode, mat.GetFloat(HDUniforms._TwoSided));
                mat.SetFloat(HDUniforms._CullModeForward, mat.GetFloat(HDUniforms._TwoSided));
            }

            // motion vectors
            bool supportsWind = mat.GetFloat(HDUniforms._WINDQUALITY) > 0.0f; // preprocessor define
            bool windEnabled = mat.GetFloat(HDUniforms._WindQuality) > 0.0f; // cbuffer value (= Wind Enabled checkbox in shader GUI)
            bool enableMotionVectorPass = supportsWind && windEnabled; // Trees render motion vectors only when wind enabled on the model
            mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enableMotionVectorPass);
        }

        private static void SetDefaultDiffusionProfile(Material mat)
        {
            if (!mat.HasVector(HDUniforms.Diffusion_Profile_Asset))
                return;

            string matDiffProfile = HDUtils.ConvertVector4ToGUID(mat.GetVector(HDUniforms.Diffusion_Profile_Asset));
            string guid = "";
            long localID;
            uint diffusionProfileHash = 0;
            var diffusionProfiles = VolumeUtils.GetOrCreateDiffusionProfileList(GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>().volumeProfile).ToArray();
            foreach (var diffusionProfileAsset in diffusionProfiles)
            {
                if (diffusionProfileAsset != null)
                {
                    bool gotGuid = AssetDatabase.TryGetGUIDAndLocalFileIdentifier<DiffusionProfileSettings>(diffusionProfileAsset, out guid, out localID);
                    if (gotGuid && (diffusionProfileAsset.name.Equals(kDefaultDiffusionProfileName) || guid.Equals(matDiffProfile)))
                    {
                        diffusionProfileHash = diffusionProfileAsset.profile.hash;
                        break;
                    }
                }
            }

            if (diffusionProfileHash == 0)
            {
                // If the user doesn't have a foliage diffusion profile defined, grab the foliage diffusion profile that comes with HD.
                // This won't work until the user adds it to their default diffusion profiles list,
                // but there is a nice "fix" button on the material to help with that.
                DiffusionProfileSettings foliageSettings = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(HDUtils.GetHDRenderPipelinePath() + kFoliageDiffusionProfilePath);
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier<DiffusionProfileSettings>(foliageSettings, out guid, out localID))
                {
                    diffusionProfileHash = foliageSettings.profile.hash;
                }
            }

            if (diffusionProfileHash != 0)
            {
                mat.SetVector(HDUniforms.Diffusion_Profile_Asset, HDUtils.ConvertGUIDToVector4(guid));
                mat.SetFloat(HDUniforms.Diffusion_Profile, HDShadowUtils.Asfloat(diffusionProfileHash));
            }
        }
    }

    class HDSpeedTree8PostProcessor : AssetPostprocessor
    {
        public void OnPostprocessSpeedTree(GameObject speedTree)
        {
            context.DependsOnCustomDependency("srp/default-pipeline");
            if (RenderPipelineManager.currentPipeline is HDRenderPipeline)
            {
                SpeedTreeImporter stImporter = assetImporter as SpeedTreeImporter;
                SpeedTree8MaterialUpgrader.PostprocessSpeedTree8Materials(speedTree, stImporter, HDSpeedTree8MaterialUpgrader.HDSpeedTree8MaterialFinalizer);
            }
        }
    }
}

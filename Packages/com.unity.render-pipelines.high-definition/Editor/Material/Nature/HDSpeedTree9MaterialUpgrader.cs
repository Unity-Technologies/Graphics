using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.SpeedTree.Importer;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// SpeedTree 9 material upgrader for HDRP.
    /// </summary>
    class HDSpeedTree9MaterialUpgrader : SpeedTree9MaterialUpgrader
    {
        private static class HDProperties
        {
            internal static readonly int ExtraMapKwToggleID = Shader.PropertyToID("_ExtraMapKwToggle");
            internal static readonly int TwoSidedID = Shader.PropertyToID("_TwoSided");
            internal static readonly int DoubleSidedEnableID = Shader.PropertyToID("_DoubleSidedEnable");
            internal static readonly int CullModeID = Shader.PropertyToID("_CullMode");
            internal static readonly int CullModeForwardID = Shader.PropertyToID("_CullModeForward");
            internal static readonly int DiffusionProfileAssetID = Shader.PropertyToID("_Diffusion_Profile_Asset");
            internal static readonly int DiffusionProfileID = Shader.PropertyToID("_Diffusion_Profile");
            internal static readonly int BillboardToggleID = Shader.PropertyToID("_BillboardKwToggle");

            internal static readonly string WindShared = "_WIND_SHARED";
            internal static readonly string WindBranch2 = "_WIND_BRANCH2";
            internal static readonly string WindBranch1 = "_WIND_BRANCH1";
            internal static readonly string WindRipple = "_WIND_RIPPLE";
            internal static readonly string WindShimmer = "_WIND_SHIMMER";

            // Should match HDRenderPipelineEditorResources.defaultDiffusionProfileSettingsList[foliageIdx].name
            internal static readonly string DefaultDiffusionProfileName = "Foliage";
        }

        const int kMaterialUpgraderVersion = 1;
        const int kCustomGUIVersion = 1; 

        [MaterialSettingsCallbackAttribute(kMaterialUpgraderVersion)]
        private static void OnAssetPostProcessDelegate(GameObject mainObject)
        {
            if (IsCurrentPipelineHDRP())
            {
                SpeedTree9MaterialUpgrader.PostprocessSpeedTree9Materials(mainObject, HDSpeedTree9MaterialUpgrader.HDSpeedTree9MaterialFinalizer);
            }
        }

        private static void HDSpeedTree9MaterialFinalizer(Material mat)
        {
            SetupHDPropertiesOnImport(mat);
            SetDefaultDiffusionProfileIfNecessary(mat);

            // Need to call this again after reconfiguring keyword toggles (like motion vectors).
            HDShaderUtils.ResetMaterialKeywords(mat);
        }

        private static void SetupHDPropertiesOnImport(Material mat)
        {
            // Since _DoubleSidedEnable controls _CullMode in HD, disable it for billboard LOD.
            if (mat.HasFloat(HDProperties.BillboardToggleID))
            {
                var isBillboard = mat.GetFloat(HDProperties.BillboardToggleID) == 1.0f;
                mat.SetFloat(HDProperties.DoubleSidedEnableID, (isBillboard) ? 0.0f : 1.0f);
            }

            if (mat.HasFloat(HDProperties.TwoSidedID))
            {
                mat.SetFloat(HDProperties.CullModeID, mat.GetFloat(HDProperties.TwoSidedID));
                mat.SetFloat(HDProperties.CullModeForwardID, mat.GetFloat(HDProperties.TwoSidedID));
            }

            bool windShared = mat.IsKeywordEnabled(HDProperties.WindShared);
            bool windBranch2 = mat.IsKeywordEnabled(HDProperties.WindBranch2);
            bool windBranch1 = mat.IsKeywordEnabled(HDProperties.WindBranch1);
            bool windRipple = mat.IsKeywordEnabled(HDProperties.WindRipple);
            bool windShimmer = mat.IsKeywordEnabled(HDProperties.WindShimmer);

            // Trees render motion vectors only when wind enabled on the model
            bool enableMotionVectorPass = windShared || windBranch2 || windBranch1 || windRipple || windShimmer;
            mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enableMotionVectorPass);
            mat.SetInt(HDShaderPassNames.s_MotionVectorsStr, (enableMotionVectorPass) ? 1 : 0);
        }

        private static void SetDefaultDiffusionProfileIfNecessary(Material mat)
        {
            bool hasDiffusionProfile = mat.HasVector(HDProperties.DiffusionProfileAssetID);
            bool hasDiffusionProfileID = mat.HasFloat(HDProperties.DiffusionProfileID);

            if (!hasDiffusionProfile || !hasDiffusionProfileID)
                return;

            Vector4 profAsset = mat.GetVector(HDProperties.DiffusionProfileAssetID);
            float profHash = mat.GetFloat(HDProperties.DiffusionProfileID);

            // User already set values from the inspector.
            if (profAsset != null && profAsset != Vector4.zero && profHash != 0)
                return;

            string matDiffProfile = HDUtils.ConvertVector4ToGUID(mat.GetVector(HDProperties.DiffusionProfileAssetID));
            string guid = "";
            uint diffusionProfileHash = 0;

            var volumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();

            if (volumeProfileSettings != null)
            {
                var diffusionProfiles = VolumeUtils.GetOrCreateDiffusionProfileList(volumeProfileSettings.volumeProfile).ToArray();
                foreach (var diffusionProfileAsset in diffusionProfiles)
                {
                    if (diffusionProfileAsset != null)
                    {
                        bool gotGuid = AssetDatabase.TryGetGUIDAndLocalFileIdentifier<DiffusionProfileSettings>(diffusionProfileAsset, out guid, out var localID);
                        if (gotGuid && (diffusionProfileAsset.name.Equals(HDProperties.DefaultDiffusionProfileName) || guid.Equals(matDiffProfile)))
                        {
                            diffusionProfileHash = diffusionProfileAsset.profile.hash;
                            break;
                        }
                    }
                }
            }

            if (diffusionProfileHash == 0)
            {
                // Use the first DiffusionProfileSettings as the default profile if nothing has been set by the user previously.
                HDRenderPipelineEditorAssets editorAssets = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorAssets>();
                if (editorAssets != null && editorAssets.defaultDiffusionProfileSettingsList != null && editorAssets.defaultDiffusionProfileSettingsList.Length > 0)
                {
                    DiffusionProfileSettings profielSettings = editorAssets.defaultDiffusionProfileSettingsList[0];
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier<DiffusionProfileSettings>(profielSettings, out guid, out long localID))
                    {
                        diffusionProfileHash = profielSettings.profile.hash;
                    }
                }
            }

            if (diffusionProfileHash != 0)
            {
                mat.SetVector(HDProperties.DiffusionProfileAssetID, HDUtils.ConvertGUIDToVector4(guid));
                mat.SetFloat(HDProperties.DiffusionProfileID, HDShadowUtils.Asfloat(diffusionProfileHash));
            }
        }

        private static bool IsCurrentPipelineHDRP()
        {
            return GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset;
        }

        [DiffuseProfileCallbackAttribute(kCustomGUIVersion)]
        private static void OnGUIDiffuseProfile(ref SerializedProperty diffusionProfileAssetValue, ref SerializedProperty diffusionProfileHashValue)
        {
            string guid = HDUtils.ConvertVector4ToGUID(diffusionProfileAssetValue.vector4Value);
            DiffusionProfileSettings diffusionProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetDatabase.GUIDToAssetPath(guid));

            EditorGUI.BeginChangeCheck();
            diffusionProfile = (DiffusionProfileSettings)EditorGUILayout.ObjectField("Diffusion Profile", diffusionProfile, typeof(DiffusionProfileSettings), false);
            if (EditorGUI.EndChangeCheck())
            {
                Vector4 newGuid = Vector4.zero;
                uint hash = 0;

                if (diffusionProfile != null)
                {
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(diffusionProfile));
                    newGuid = HDUtils.ConvertGUIDToVector4(guid);
                    hash = diffusionProfile.profile.hash;
                }

                diffusionProfileAssetValue.vector4Value = newGuid;
                diffusionProfileHashValue.uintValue = hash;
            }
        }
    }
}

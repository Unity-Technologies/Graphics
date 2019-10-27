using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    static class DiffusionProfileMaterialUI
    {
        static GUIContent    diffusionProfileNotInHDRPAsset = new GUIContent("You must add this diffusion profile in the HDRP asset to make it work", EditorGUIUtility.IconContent("console.warnicon").image);

        public static bool IsSupported(MaterialEditor materialEditor)
        {
            return !materialEditor.targets.Any(o => {
                Material m = o as Material;
                return !m.HasProperty("_DiffusionProfileAsset") || !m.HasProperty("_DiffusionProfileHash");
            });
        }

        public static void OnGUI(MaterialProperty diffusionProfileAsset, MaterialProperty diffusionProfileHash)
        {
            // We can't cache these fields because of several edge cases like undo/redo or pressing escape in the object picker
            string guid = HDUtils.ConvertVector4ToGUID(diffusionProfileAsset.vectorValue);
            DiffusionProfileSettings diffusionProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetDatabase.GUIDToAssetPath(guid));

            // is it okay to do this every frame ?
            EditorGUI.BeginChangeCheck();
            diffusionProfile = (DiffusionProfileSettings)EditorGUILayout.ObjectField("Diffusion Profile", diffusionProfile, typeof(DiffusionProfileSettings), false);
            if (EditorGUI.EndChangeCheck())
            {
                Vector4 newGuid = Vector4.zero;
                float    hash = 0;

                if (diffusionProfile != null)
                {
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(diffusionProfile));
                    newGuid = HDUtils.ConvertGUIDToVector4(guid);
                    hash = HDShadowUtils.Asfloat(diffusionProfile.profile.hash);
                }

                // encode back GUID and it's hash
                diffusionProfileAsset.vectorValue = newGuid;
                diffusionProfileHash.floatValue = hash;
            }

            DrawDiffusionProfileWarning(diffusionProfile);
        }

        static void DrawDiffusionProfileWarning(DiffusionProfileSettings materialProfile)
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (materialProfile != null && !hdPipeline.asset.diffusionProfileSettingsList.Any(d => d == materialProfile))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUIStyle wordWrap = new GUIStyle(EditorStyles.label);
                    wordWrap.wordWrap = true;
                    EditorGUILayout.LabelField(diffusionProfileNotInHDRPAsset, wordWrap);
                    if (GUILayout.Button("Fix", GUILayout.ExpandHeight(true)))
                    {
                        hdPipeline.asset.AddDiffusionProfile(materialProfile);
                    }
                }
            }
        }
    }
}

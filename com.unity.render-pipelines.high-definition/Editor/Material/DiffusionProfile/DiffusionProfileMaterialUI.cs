using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static class DiffusionProfileMaterialUI
    {
        public static bool IsSupported(MaterialEditor materialEditor)
        {
            return !materialEditor.targets.Any(o => {
                Material m = o as Material;
                return !m.HasProperty("_DiffusionProfileAsset") || !m.HasProperty("_DiffusionProfileHash");
            });
        }

        public static void OnGUI(MaterialProperty diffusionProfileAsset, MaterialProperty diffusionProfileHash)
        {
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
        }
    }
}

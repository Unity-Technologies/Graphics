using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.SpeedTree.Importer;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalSpeedTree9Upgrader : SpeedTree9MaterialUpgrader
    {
        const int kMaterialUpgraderVersion = 1;

        [MaterialSettingsCallbackAttribute(kMaterialUpgraderVersion)]
        private static void OnAssetPostProcessDelegate(GameObject mainObject)
        {
            if (IsCurrentPipelineURP())
            {
                SpeedTree9MaterialUpgrader.PostprocessSpeedTree9Materials(mainObject, UniversalSpeedTree9MaterialFinalizer);
            }
        }

        static private bool IsCurrentPipelineURP()
        {
            return GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
        }

        static public void UniversalSpeedTree9MaterialFinalizer(Material mat)
        {
            if (mat.HasFloat("_TwoSided"))
                mat.SetFloat(Property.CullMode, mat.GetFloat("_TwoSided"));

            Unity.Rendering.Universal.ShaderUtils.UpdateMaterial(mat,
                Unity.Rendering.Universal.ShaderUtils.MaterialUpdateType.CreatedNewMaterial,
                Unity.Rendering.Universal.ShaderUtils.ShaderID.SpeedTree9);
        }
    }
}

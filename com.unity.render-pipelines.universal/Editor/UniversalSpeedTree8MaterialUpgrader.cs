using System;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    class UniversalSpeedTree8Upgrader : SpeedTree8MaterialUpgrader
    {
        internal UniversalSpeedTree8Upgrader(string oldShaderName)
            : base(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SpeedTree8), UniversalSpeedTree8MaterialFinalizer)
        {
            RenameFloat("_TwoSided", Property.CullMode);
        }
        static public void UniversalSpeedTree8MaterialFinalizer(Material mat)
        {
            SpeedTree8MaterialFinalizer(mat);

            if (mat.HasFloat("_TwoSided"))
                mat.SetFloat(Property.CullMode, mat.GetFloat("_TwoSided"));

            Unity.Rendering.Universal.ShaderUtils.UpdateMaterial(mat,
                Unity.Rendering.Universal.ShaderUtils.MaterialUpdateType.CreatedNewMaterial,
                Unity.Rendering.Universal.ShaderUtils.ShaderID.SpeedTree8);
        }
    }

    class UniversalSpeedTree8PostProcessor : AssetPostprocessor
    {
        void OnPostprocessSpeedTree(GameObject speedTree)
        {
            context.DependsOnCustomDependency("srp/default-pipeline");

            if (RenderPipelineManager.currentPipeline is UniversalRenderPipeline)
            {
                SpeedTreeImporter stImporter = assetImporter as SpeedTreeImporter;
                SpeedTree8MaterialUpgrader.PostprocessSpeedTree8Materials(speedTree, stImporter, UniversalSpeedTree8Upgrader.UniversalSpeedTree8MaterialFinalizer);
            }
        }
    }
}

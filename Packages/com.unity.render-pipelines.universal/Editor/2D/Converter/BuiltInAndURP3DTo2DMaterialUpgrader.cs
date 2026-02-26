using System;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using static UnityEditor.AssetDatabase;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [PipelineTools]
    [BatchModeConverterClassInfo("UpgradeURP2DAssets", "URPToReadonlyMaterial2D")]
    [ElementInfo(Name = "Convert Built-in and URP ( Universal Renderer ) Materials to Mesh2D-Lit-Default",
                 Order = 300,
                 Description = "This will upgrade/crossgrade all 3D materials and 3D material references for URP 2D.")]
    internal sealed class BuiltInAndURP3DTo2DMaterialUpgrader : Base2DMaterialUpgrader
    {
        public override MaterialConversionInfo[] InitializeMaterialConversionInfo()
        {
            // Note: functions here are shortened versions using static AssetDatabase
            Material meshLit = LoadAssetAtPath<Material>(k_PackageMaterialsPath + "Mesh2D-Lit-Default.mat");
            Material spriteDefaultMat = GetSpriteDefaultMaterial();

            MaterialConversionInfo[]  materialConversionInfo = new MaterialConversionInfo[]
            {
                // Conversion from built-in to URP 2D
                new MaterialConversionInfo(
                    GetBuiltinExtraResource<Material>("Default-Material.mat"),
                    meshLit
                ),

                // Cross conversion from URP 3D to URP 2D. Just supports simple conversion for now
                new MaterialConversionInfo(
                    LoadAssetAtPath<Material>(k_PackageMaterialsPath + "Lit.mat"),
                    meshLit
                ),
                new MaterialConversionInfo(
                    LoadAssetAtPath<Material>(k_PackageMaterialsPath + "SimpleLit.mat"),
                    meshLit
                ),
            };

            return materialConversionInfo;
        }
    }
}

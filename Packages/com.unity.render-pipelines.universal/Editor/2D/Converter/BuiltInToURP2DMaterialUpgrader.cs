using System;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using static UnityEditor.AssetDatabase;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [PipelineConverter("Built-in", "Universal Render Pipeline (2D Renderer)")]
    [ElementInfo(Name = "Material and Material Reference Upgrade",
             Order = 400,
             Description = "This will upgrade all materials and material references.")]
    internal sealed class BuiltInToURP2DMaterialUpgrader : Base2DMaterialUpgrader
    {
        public override MaterialConversionInfo[] InitializeMaterialConversionInfo()
        {
            // Note: functions here are shortened versions using static AssetDatabase
            Material spriteDefaultMat = GetSpriteDefaultMaterial();

            MaterialConversionInfo[] materialConversionInfo = new MaterialConversionInfo[]
            {
                // Conversion from built-in to URP 2D
                new MaterialConversionInfo(
                    GetBuiltinExtraResource<Material>("Sprites-Default.mat"),
                    spriteDefaultMat
                ),
                new MaterialConversionInfo(
                    GetBuiltinExtraResource<Material>("Sprites-Mask.mat"),
                    LoadAssetAtPath<Material>(k_PackageMaterialsPath + "SpriteMask-Default.mat")
                ),
            };

            return materialConversionInfo;
        }

    }
}

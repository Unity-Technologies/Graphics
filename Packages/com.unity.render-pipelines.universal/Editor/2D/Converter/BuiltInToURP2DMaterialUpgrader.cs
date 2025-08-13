using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.AssetDatabase;
using System.Collections;

namespace UnityEditor.Rendering.Universal
{
    internal sealed class BuiltInToURP2DMaterialUpgrader : Base2DMaterialUpgrader
    {
        public override string name => "Material and Material Reference Upgrade";
        public override string info => "This will upgrade all materials and material references.";
        public override int priority => -1000;
        public override Type container => typeof(BuiltInToURP2DConverterContainer);

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

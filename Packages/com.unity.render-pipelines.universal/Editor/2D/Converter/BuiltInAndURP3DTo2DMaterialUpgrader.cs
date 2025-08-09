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
    internal sealed class BuiltInAndURP3DTo2DMaterialUpgrader : Base2DMaterialUpgrader
    {
        public override string name => "Material and Material Reference Upgrade";
        public override string info => "This will upgrade/crossgrade all 3D materials and 3D material references for URP 2D.";
        public override int priority => -1000;
        public override Type container => typeof(BuiltInAndURP3DTo2DConverterContainer);

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

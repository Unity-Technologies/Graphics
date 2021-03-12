using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

// Include material common properties names
using static UnityEditor.Rendering.Universal.HDMaterialProperties;


namespace UnityEditor.Rendering.Universal
{
    internal static class HDShaderIDs
    {
        public static readonly int _DecalColorMask0 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask0);
        public static readonly int _DecalColorMask1 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask1);
        public static readonly int _DecalColorMask2 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask2);
        public static readonly int _DecalColorMask3 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask3);
    }

    internal static class HDMaterialProperties
    {
        internal const string kDecalColorMask0 = "_DecalColorMask0";
        internal const string kDecalColorMask1 = "_DecalColorMask1";
        internal const string kDecalColorMask2 = "_DecalColorMask2";
        internal const string kDecalColorMask3 = "_DecalColorMask3";

        /// <summary>Enable affect Albedo (decal only).</summary>
        public const string kAffectAlbedo = "_AffectAlbedo";
        /// <summary>Enable affect Normal (decal only.</summary>
        public const string kAffectNormal = "_AffectNormal";
        /// <summary>Enable affect AO (decal only.</summary>
        public const string kAffectAO = "_AffectAO";
        /// <summary>Enable affect Metal (decal only.</summary>
        public const string kAffectMetal = "_AffectMetal";
        /// <summary>Enable affect Smoothness (decal only.</summary>
        public const string kAffectSmoothness = "_AffectSmoothness";
        /// <summary>Enable affect Emission (decal only.</summary>
        public const string kAffectEmission = "_AffectEmission";

        internal const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
        internal const string kDecalStencilRef = "_DecalStencilRef";
    }

    internal static class HDShaderPassNames
    {
        /// <summary>DBuffer Projector pass name.</summary>
        public static readonly string s_DBufferProjectorStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector];
        /// <summary>Decal Projector Forward Emissive pass name.</summary>
        public static readonly string s_DecalProjectorForwardEmissiveStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalProjectorForwardEmissive];
        /// <summary>DBuffer Mesh pass name.</summary>
        public static readonly string s_DBufferMeshStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh];
        /// <summary>Decal Mesh Forward Emissive pass name.</summary>
        public static readonly string s_DecalMeshForwardEmissiveStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive];
    }

    /// <summary>
    /// Represents the GUI for HDRP Shader Graph materials.
    /// </summary>
    internal class DecalShaderGraphGUI : PBRMasterGUI
    {
        /// <summary>
        /// Override this function to implement your custom GUI. To display a user interface similar to HDRP shaders, use a MaterialUIBlockList.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties the material has.</param>
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // always instanced
            SerializedProperty instancing = materialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                base.OnGUI(materialEditor, props);
                foreach (var serializedObject in materialEditor.targets)
                {
                    var material = serializedObject as Material;
                    SetupDecalKeywordsAndPass(material);
                }
            }

            // We should always do this call at the end
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        public static void SetupDecalKeywordsAndPass(Material material)
        {
            /*bool affectsMaskmap = false;
            affectsMaskmap |= material.HasProperty(kAffectMetal) && material.GetFloat(kAffectMetal) == 1.0f;
            affectsMaskmap |= material.HasProperty(kAffectAO) && material.GetFloat(kAffectAO) == 1.0f;
            affectsMaskmap |= material.HasProperty(kAffectSmoothness) && material.GetFloat(kAffectSmoothness) == 1.0f;

            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_ALBEDO", material.HasProperty(kAffectAlbedo) && material.GetFloat(kAffectAlbedo) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_NORMAL", material.HasProperty(kAffectNormal) && material.GetFloat(kAffectNormal) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", affectsMaskmap);*/

            // Albedo : RT0 RGB, A - sRGB
            // Normal : RT1 RGB, A
            // Smoothness: RT2 B, A
            // Metal: RT2 R, RT3 R
            // AO: RT2 G, RT3 G
            // Note RT3 is only RG
            //ColorWriteMask mask0 = 0, mask1 = 0, mask2 = 0, mask3 = 0;

            /*if (material.HasProperty(kAffectAlbedo) && material.GetFloat(kAffectAlbedo) == 1.0f)
                mask0 |= ColorWriteMask.All;
            if (material.HasProperty(kAffectNormal) && material.GetFloat(kAffectNormal) == 1.0f)
                mask1 |= ColorWriteMask.All;
            if (material.HasProperty(kAffectMetal) && material.GetFloat(kAffectMetal) == 1.0f)
                mask2 |= mask3 |= ColorWriteMask.Red;
            if (material.HasProperty(kAffectAO) && material.GetFloat(kAffectAO) == 1.0f)
                mask2 |= mask3 |= ColorWriteMask.Green;
            if (material.HasProperty(kAffectSmoothness) && material.GetFloat(kAffectSmoothness) == 1.0f)
                mask2 |= ColorWriteMask.Blue | ColorWriteMask.Alpha;*/

            /*material.SetInt(HDShaderIDs._DecalColorMask0, (int)mask0);
            material.SetInt(HDShaderIDs._DecalColorMask1, (int)mask1);
            material.SetInt(HDShaderIDs._DecalColorMask2, (int)mask2);
            material.SetInt(HDShaderIDs._DecalColorMask3, (int)mask3);*/

            // First reset the pass (in case new shader graph add or remove a pass)
            /*material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferProjectorStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferMeshStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DecalMeshForwardEmissiveStr, true);*/

            // Then disable pass is they aren't needed
            /*if (material.FindPass(HDShaderPassNames.s_DBufferProjectorStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferProjectorStr, ((int)mask0 + (int)mask1 + (int)mask2 + (int)mask3) != 0);
            if (material.FindPass(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr, material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f);
            if (material.FindPass(HDShaderPassNames.s_DBufferMeshStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferMeshStr, ((int)mask0 + (int)mask1 + (int)mask2 + (int)mask3) != 0);
            if (material.FindPass(HDShaderPassNames.s_DecalMeshForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalMeshForwardEmissiveStr, material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f);
            */
            // Set stencil state
            //material.SetInt(kDecalStencilWriteMask, (int)StencilUsage.Decals);
            //material.SetInt(kDecalStencilRef, (int)StencilUsage.Decals);
        }
    }
}

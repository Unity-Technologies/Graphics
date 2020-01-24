using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Decal materials (does not include ShaderGraphs)
    /// </summary>
    class DecalUI : ShaderGUI
    {
        // Same hack as in HDShaderGUI but for some reason, this editor does not inherit from HDShaderGUI
        bool m_FirstFrame = true;

        [Flags]
        enum Expandable : uint
        {
            Input = 1 << 0,
            Sorting = 1 << 1,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new DecalSurfaceInputsUIBlock((MaterialUIBlock.Expandable)Expandable.Input),
            new DecalSortingInputsUIBlock((MaterialUIBlock.Expandable)Expandable.Sorting),
        };

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            SetupMaterialKeywordsAndPassInternal(material);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            LoadMaterialProperties(props);

            SerializedProperty instancing = materialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);

                var surfaceInputs = uiBlocks.FetchUIBlock< DecalSurfaceInputsUIBlock >();

                // Apply material keywords and pass:
                if (changed.changed || m_FirstFrame)
                {
                    m_FirstFrame = false;

                    normalBlendSrc.floatValue = surfaceInputs.normalBlendSrcValue;
                    maskBlendSrc.floatValue = surfaceInputs.maskBlendSrcValue;
                    maskBlendMode.floatValue = (float)surfaceInputs.maskBlendFlags;
                    smoothnessRemapMin.floatValue = surfaceInputs.smoothnessRemapMinValue;
                    smoothnessRemapMax.floatValue = surfaceInputs.smoothnessRemapMaxValue;
                    AORemapMin.floatValue = surfaceInputs.AORemapMinValue;
                    AORemapMax.floatValue = surfaceInputs.AORemapMaxValue;
                    if (useEmissiveIntensity.floatValue == 1.0f)
                    {
                        emissiveColor.colorValue = emissiveColorLDR.colorValue * emissiveIntensity.floatValue;
                    }
                    else
                    {
                        emissiveColor.colorValue = emissiveColorHDR.colorValue;
                    }

                    foreach (var material in uiBlocks.materials)
                        SetupMaterialKeywordsAndPassInternal(material);
                }
            }
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue
        }
        protected const string kBaseColorMap = "_BaseColorMap";

        protected const string kBaseColor = "_BaseColor";

        protected const string kNormalMap = "_NormalMap";

        protected const string kMaskMap = "_MaskMap";

        protected const string kDecalBlend = "_DecalBlend";

        protected const string kAlbedoMode = "_AlbedoMode";

        protected MaterialProperty normalBlendSrc = new MaterialProperty();
        protected const string kNormalBlendSrc = "_NormalBlendSrc";

        protected MaterialProperty maskBlendSrc = new MaterialProperty();
        protected const string kMaskBlendSrc = "_MaskBlendSrc";

        protected MaterialProperty maskBlendMode = new MaterialProperty();
        protected const string kMaskBlendMode = "_MaskBlendMode";

        protected const string kMaskmapMetal = "_MaskmapMetal";

        protected const string kMaskmapAO = "_MaskmapAO";

        protected const string kMaskmapSmoothness = "_MaskmapSmoothness";

        protected const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        protected const string kDrawOrder = "_DrawOrder";

        protected const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
        protected const string kDecalStencilRef = "_DecalStencilRef";

        protected MaterialProperty AORemapMin = new MaterialProperty();
        protected const string kAORemapMin = "_AORemapMin";

        protected MaterialProperty AORemapMax = new MaterialProperty();
        protected const string kAORemapMax = "_AORemapMax";

        protected MaterialProperty smoothnessRemapMin = new MaterialProperty();
        protected const string kSmoothnessRemapMin = "_SmoothnessRemapMin";

        protected MaterialProperty smoothnessRemapMax = new MaterialProperty();
        protected const string kSmoothnessRemapMax = "_SmoothnessRemapMax";

        protected const string kMetallicScale = "_MetallicScale";

        protected const string kMaskMapBlueScale = "_DecalMaskMapBlueScale";

        protected MaterialProperty emissiveColor = new MaterialProperty();
        protected const string kEmissiveColor = "_EmissiveColor";

        protected MaterialProperty emissiveColorMap = new MaterialProperty();
        protected const string kEmissiveColorMap = "_EmissiveColorMap";

        protected const string kEmissive = "_Emissive";

        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";

        protected const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";

        protected MaterialProperty useEmissiveIntensity = null;
        protected const string kUseEmissiveIntensity = "_UseEmissiveIntensity";

        protected MaterialProperty emissiveColorLDR = null;
        protected const string kEmissiveColorLDR = "_EmissiveColorLDR";

        protected MaterialProperty emissiveColorHDR = null;
        protected const string kEmissiveColorHDR = "_EmissiveColorHDR";

        void LoadMaterialProperties(MaterialProperty[] properties)
        {
            normalBlendSrc = FindProperty(kNormalBlendSrc, properties);
            maskBlendSrc = FindProperty(kMaskBlendSrc, properties);
            maskBlendMode = FindProperty(kMaskBlendMode, properties);
            AORemapMin = FindProperty(kAORemapMin, properties);
            AORemapMax = FindProperty(kAORemapMax, properties);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin, properties);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax, properties);
            emissiveColor = FindProperty(kEmissiveColor, properties);
            emissiveColorMap = FindProperty(kEmissiveColorMap, properties);
            useEmissiveIntensity = FindProperty(kUseEmissiveIntensity, properties);
            emissiveIntensity = FindProperty(kEmissiveIntensity, properties);
            emissiveColorLDR = FindProperty(kEmissiveColorLDR, properties);
            emissiveColorHDR = FindProperty(kEmissiveColorHDR, properties);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            Decal.MaskBlendFlags blendMode = (Decal.MaskBlendFlags)material.GetFloat(kMaskBlendMode);

            CoreUtils.SetKeyword(material, "_ALBEDOCONTRIBUTION", material.GetFloat(kAlbedoMode) == 1.0f);
            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            CoreUtils.SetKeyword(material, "_EMISSIVEMAP", material.GetTexture(kEmissiveColorMap));

            material.SetInt(kDecalStencilWriteMask, (int)StencilUsage.Decals);
            material.SetInt(kDecalStencilRef, (int)StencilUsage.Decals);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecals3RTStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsForwardEmissive, material.GetFloat("_Emissive") == 1.0f);
            switch (blendMode)
            {
                case Decal.MaskBlendFlags.Metal:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, true);
                    break;

                case Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, true);
                    break;

                case Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, true);
                    break;
            }
        }

        //protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }
    }
}

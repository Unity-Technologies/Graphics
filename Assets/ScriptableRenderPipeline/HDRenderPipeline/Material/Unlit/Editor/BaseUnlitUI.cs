using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // This is the purpose of BaseLitGUI. It contain all properties that are common to all Material based on Lit template.
    // For the default hand written Lit material see LitUI.cs that contain specific properties for our default implementation.
    public abstract class BaseUnlitGUI : ShaderGUI
    {
        protected static class StylesBaseUnlit
        {
            public static string optionText = "Surface options";
            public static string surfaceTypeText = "Surface Type";
            public static string blendModeText = "Blend Mode";

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "Threshold for alpha cutoff");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent doubleSidedEnableText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling) and flip/mirror normal");
            public static GUIContent distortionEnableText = new GUIContent("Distortion", "Enable distortion on this shader");
            public static GUIContent distortionOnlyText = new GUIContent("Distortion Only", "This shader will only be use to render distortion");
            public static GUIContent distortionDepthTestText = new GUIContent("Distortion Depth Test", "Enable the depth test for distortion");

            public static string advancedText = "Advanced Options";
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum BlendMode
        {
            Lerp,
            Add,
            SoftAdd,
            Multiply,
            Premultiply
        }

        protected MaterialEditor m_MaterialEditor;

        // Properties
        protected MaterialProperty surfaceType = null;
        protected const string kSurfaceType = "_SurfaceType";
        protected MaterialProperty alphaCutoffEnable = null;
        protected const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        protected MaterialProperty alphaCutoff = null;
        protected const string kAlphaCutoff = "_AlphaCutoff";
        protected MaterialProperty doubleSidedEnable = null;
        protected const string kDoubleSidedEnable = "_DoubleSidedEnable";
        protected MaterialProperty blendMode = null;
        protected const string kBlendMode = "_BlendMode";
        protected MaterialProperty distortionEnable = null;
        protected const string kDistortionEnable = "_DistortionEnable";
        protected MaterialProperty distortionOnly = null;
        protected const string kDistortionOnly = "_DistortionOnly";
        protected MaterialProperty distortionDepthTest = null;
        protected const string kDistortionDepthTest = "_DistortionDepthTest";

        // See comment in LitProperties.hlsl
        const string kEmissionColor = "_EmissionColor";

        // The following set of functions are call by the ShaderGraph
        // It will allow to display our common parameters + setup keyword correctly for them
        protected abstract void FindMaterialProperties(MaterialProperty[] props);
        protected abstract void SetupMaterialKeywordsAndPassInternal(Material material);
        protected abstract void MaterialPropertiesGUI(Material material);
        protected abstract void VertexAnimationPropertiesGUI();
        // This function will say if emissive is used or not regarding enlighten/PVR
        protected abstract bool ShouldEmissionBeEnabled(Material material);

        protected virtual void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            surfaceType = FindProperty(kSurfaceType, props);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled, props);
            alphaCutoff = FindProperty(kAlphaCutoff, props);
            doubleSidedEnable = FindProperty(kDoubleSidedEnable, props);
            blendMode = FindProperty(kBlendMode, props);
            distortionEnable = FindProperty(kDistortionEnable, props, false);
            distortionOnly = FindProperty(kDistortionOnly, props, false);
            distortionDepthTest = FindProperty(kDistortionDepthTest, props, false);
        }

        void SurfaceTypePopup()
        {
            EditorGUI.showMixedValue = surfaceType.hasMixedValue;
            var mode = (SurfaceType)surfaceType.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (SurfaceType)EditorGUILayout.Popup(StylesBaseUnlit.surfaceTypeText, (int)mode, StylesBaseUnlit.surfaceTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Surface Type");
                surfaceType.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        private void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(StylesBaseUnlit.blendModeText, (int)mode, StylesBaseUnlit.blendModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected virtual void BaseMaterialPropertiesGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(StylesBaseUnlit.optionText, EditorStyles.boldLabel);
            SurfaceTypePopup();
            if ((SurfaceType)surfaceType.floatValue == SurfaceType.Transparent)
            {
                BlendModePopup();

                if (distortionEnable != null)
                {
                    m_MaterialEditor.ShaderProperty(distortionEnable, StylesBaseUnlit.distortionEnableText);

                    if (distortionEnable.floatValue == 1.0f)
                    {
                        m_MaterialEditor.ShaderProperty(distortionOnly, StylesBaseUnlit.distortionOnlyText);
                        m_MaterialEditor.ShaderProperty(distortionDepthTest, StylesBaseUnlit.distortionDepthTestText);
                    }
                }
            }
            m_MaterialEditor.ShaderProperty(alphaCutoffEnable, StylesBaseUnlit.alphaCutoffEnableText);
            if (alphaCutoffEnable.floatValue == 1.0f)
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, StylesBaseUnlit.alphaCutoffText);
            }
            // This function must finish with double sided option (see LitUI.cs)
            m_MaterialEditor.ShaderProperty(doubleSidedEnable, StylesBaseUnlit.doubleSidedEnableText);
        }

        static public void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if ocde change
        static public void SetupBaseUnlitKeywords(Material material)
        {
            bool alphaTestEnable = material.GetFloat(kAlphaCutoffEnabled) > 0.0f;
            SurfaceType surfaceType = (SurfaceType)material.GetFloat(kSurfaceType);
            BlendMode blendMode = (BlendMode)material.GetFloat(kBlendMode);

            if (surfaceType == SurfaceType.Opaque)
            {
                material.SetOverrideTag("RenderType", alphaTestEnable ? "TransparentCutout" : "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = alphaTestEnable ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
            }
            else
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                switch (blendMode)
                {
                    case BlendMode.Lerp:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;

                    case BlendMode.Add:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        break;

                    case BlendMode.SoftAdd:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        break;

                    case BlendMode.Multiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        break;

                    case BlendMode.Premultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;
                }
            }

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;
            if (doubleSidedEnable)
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            }
            else
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
            }

            SetKeyword(material, "_DOUBLESIDED_ON", doubleSidedEnable);
            SetKeyword(material, "_ALPHATEST_ON", alphaTestEnable);

            if (material.HasProperty(kDistortionEnable))
            {
                bool distortionEnable = material.GetFloat(kDistortionEnable) > 0.0f;
                if (distortionEnable)
                {
                    material.SetShaderPassEnabled("DistortionVectors", true);
                }
                else
                {
                    material.SetShaderPassEnabled("DistortionVectors", false);
                }

                bool distortionDepthTest = material.GetFloat(kDistortionDepthTest) > 0.0f;
                if (distortionDepthTest)
                {
                    material.SetInt("_ZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                }
                else
                {
                    material.SetInt("_ZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                }

                SetKeyword(material, "_DISTORTION_ON", distortionEnable);
            }

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
        }

        static public void SetupBaseUnlitMaterialPass(Material material)
        {
            if (material.HasProperty(kDistortionEnable))
            {
                bool distortionEnable = material.GetFloat(kDistortionEnable) > 0.0f;
                bool distortionOnly = material.GetFloat(kDistortionOnly) > 0.0f;

                if (distortionEnable && distortionOnly)
                {
                    // Disable all passes except debug material
                    material.SetShaderPassEnabled("GBuffer", false);
                    material.SetShaderPassEnabled("DebugViewMaterial", true);
                    material.SetShaderPassEnabled("Meta", false);
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    material.SetShaderPassEnabled("DepthOnly", false);
                    material.SetShaderPassEnabled("MotionVectors", false);
                    material.SetShaderPassEnabled("Forward", false);
                }
                else
                {
                    material.SetShaderPassEnabled("GBuffer", true);
                    material.SetShaderPassEnabled("DebugViewMaterial", true);
                    material.SetShaderPassEnabled("Meta", true);
                    material.SetShaderPassEnabled("ShadowCaster", true);
                    material.SetShaderPassEnabled("DepthOnly", true);
                    material.SetShaderPassEnabled("MotionVectors", true);
                    material.SetShaderPassEnabled("Forward", true);
                }
            }
        }

        // Dedicated to emissive - for emissive Enlighten/PVR
        protected void DoEmissionArea(Material material)
        {
            // Emission for GI?
            if (ShouldEmissionBeEnabled(material))
            {
                if (m_MaterialEditor.EmissionEnabledProperty())
                {
                    // change the GI flag and fix it up with emissive as black if necessary
                    m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
                }
            }
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                BaseMaterialPropertiesGUI();
                EditorGUILayout.Space();

                VertexAnimationPropertiesGUI();

                EditorGUILayout.Space();
                MaterialPropertiesGUI(material);

                DoEmissionArea(material);

                GUILayout.Label(StylesBaseUnlit.advancedText, EditorStyles.boldLabel);
                // NB renderqueue editor is not shown on purpose: we want to override it based on blend mode
                m_MaterialEditor.EnableInstancingField();
                m_MaterialEditor.DoubleSidedGIField();
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
            }
        }

        // This is call by the inspector
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;
            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            FindBaseMaterialProperties(props);
            FindMaterialProperties(props);

            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
}

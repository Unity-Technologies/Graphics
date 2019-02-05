using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // This is the purpose of BaseLitGUI. It contain all properties that are common to all Material based on Lit template.
    // For the default hand written Lit material see LitUI.cs that contain specific properties for our default implementation.
    abstract class BaseUnlitGUI : ExpandableAreaMaterial
    {
        //Be sure to end before after last LayeredLitGUI.LayerExpendable
        [Flags]
        protected enum Expandable : uint
        {
            Base = 1<<0,
            Input = 1<<1,
            Tesselation = 1<<2,
            Transparency = 1<<3,
            VertexAnimation = 1<<4,
            Detail = 1<<5,
            Emissive = 1<<6,
            Advance = 1<<7,
            Other = 1 << 8
        }

        protected static class StylesBaseUnlit
        {
            public static string TransparencyInputsText = "Transparency Inputs";
            public static string optionText = "Surface Options";
            public static string surfaceTypeText = "Surface Type";
            public static string renderingPassText = "Rendering Pass";
            public static string blendModeText = "Blending Mode";

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));
            public static readonly int[] blendModeValues = Enum.GetValues(typeof(BlendMode)) as int[];


            

            

    

            public static GUIContent transparentPrepassText = new GUIContent("Appear in Refraction", "When enabled, HDRP handles objects with this Material before the refraction pass.");


            public static GUIContent useShadowThresholdText = new GUIContent("Use Shadow Threshold", "Enable separate threshold for shadow pass");
            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Clipping", "When enabled, HDRP processes Alpha Clipping for this Material.");
            public static GUIContent alphaCutoffText = new GUIContent("Threshold", "Controls the threshold for the Alpha Clipping effect.");
            public static GUIContent alphaCutoffShadowText = new GUIContent("Shadow Threshold", "Contols the threshold for shadow pass alpha clipping.");
            public static GUIContent alphaCutoffPrepassText = new GUIContent("Prepass Threshold", "Controls the threshold for transparent depth prepass alpha clipping.");
            public static GUIContent alphaCutoffPostpassText = new GUIContent("Postpass Threshold", "Controls the threshold for transparent depth postpass alpha clipping.");
            public static GUIContent transparentDepthPostpassEnableText = new GUIContent("Transparent Depth Postpass", "When enabled, HDRP renders a depth postpass for transparent objects. This improves post-processing effects like depth of field.");
            public static GUIContent transparentDepthPrepassEnableText = new GUIContent("Transparent Depth Prepass", "When enabled, HDRP renders a depth prepass for transparent GameObjects. This improves sorting.");
            public static GUIContent transparentBackfaceEnableText = new GUIContent("Back Then Front Rendering", "When enabled, HDRP renders the back face and then the front face, in two separate draw calls, to better sort transparent meshes.");

            public static GUIContent transparentSortPriorityText = new GUIContent("Sorting Priority", "Sets the sort priority (from -100 to 100) of transparent meshes using this Material. HDRP uses this value to calculate the sorting order of all transparent meshes on screen.");
            public static GUIContent enableTransparentFogText = new GUIContent("Receive fog", "When enabled, this Material can receive fog.");
            public static GUIContent enableBlendModePreserveSpecularLightingText = new GUIContent("Preserve specular lighting", "When enabled, blending only affects diffuse lighting, allowing for correct specular lighting on transparent meshes that use this Material.");

            public static GUIContent doubleSidedEnableText = new GUIContent("Double-Sided", "When enabled, HDRP renders both faces of the polygons that make up meshes using this Material. Disables backface culling.");
            public static GUIContent distortionEnableText = new GUIContent("Distortion", "When enabled, HDRP processes distortion for this Material.");
            public static GUIContent distortionOnlyText = new GUIContent("Distortion Only", "When enabled, HDRP only uses this Material to render distortion.");
            public static GUIContent distortionDepthTestText = new GUIContent("Distortion Depth Test", "When enabled, HDRP calculates a depth test for distortion.");
            public static GUIContent distortionVectorMapText = new GUIContent("Distortion Vector Map (RGB)", "Specifies the Vector Map HDRP uses for the distortion effect\nDistortion 2D vector (RG) and Blur amount (B)\nHDRP applies the scale and bias to the distortion vector only, not the blur amount.");
            public static GUIContent distortionBlendModeText = new GUIContent("Distortion Blend Mode", "Specifies the mode HDRP uses to calculate distortion.");
            public static GUIContent distortionScaleText = new GUIContent("Distortion Scale", "Sets the scale HDRP applies to the Distortion Vector Map.");
            public static GUIContent distortionBlurScaleText = new GUIContent("Distortion Blur Scale", "Sets the scale HDRP applies to the distortion blur effect.");
            public static GUIContent distortionBlurRemappingText = new GUIContent("Distortion Blur Remapping", "Controls a remap for the Distortion Blur effect.");

            public static GUIContent enableMotionVectorForVertexAnimationText = new GUIContent("MotionVector For Vertex Animation", "When enabled, HDRP processes an object motion vector pass for this material.");


            public static string advancedText = "Advanced Options";
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        // Enum values are hardcoded for retro-compatibility. Don't change them.
        public enum BlendMode
        {
            // Note: value is due to code change, don't change the value
            Alpha = 0,
            Premultiply = 4,
            Additive = 1
        }

        protected MaterialEditor m_MaterialEditor;

        // Properties
        protected MaterialProperty surfaceType = null;
        protected const string kSurfaceType = "_SurfaceType";
        protected MaterialProperty alphaCutoffEnable = null;

        protected const string kUseShadowThreshold = "_UseShadowThreshold";
        protected MaterialProperty useShadowThreshold = null;
        protected const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        protected MaterialProperty alphaCutoff = null;
        protected const string kAlphaCutoff = "_AlphaCutoff";
        protected MaterialProperty alphaCutoffShadow = null;
        protected const string kAlphaCutoffShadow = "_AlphaCutoffShadow";
        protected MaterialProperty alphaCutoffPrepass = null;
        protected const string kAlphaCutoffPrepass = "_AlphaCutoffPrepass";
        protected MaterialProperty alphaCutoffPostpass = null;
        protected const string kAlphaCutoffPostpass = "_AlphaCutoffPostpass";
        protected MaterialProperty transparentDepthPrepassEnable = null;
        protected const string kTransparentDepthPrepassEnable = "_TransparentDepthPrepassEnable";
        protected MaterialProperty transparentDepthPostpassEnable = null;
        protected const string kTransparentDepthPostpassEnable = "_TransparentDepthPostpassEnable";
        protected MaterialProperty transparentBackfaceEnable = null;
        protected const string kTransparentBackfaceEnable = "_TransparentBackfaceEnable";
        protected MaterialProperty transparentSortPriority = null;
        protected const string kTransparentSortPriority = "_TransparentSortPriority";
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
        protected MaterialProperty distortionVectorMap = null;
        protected const string kDistortionVectorMap = "_DistortionVectorMap";
        protected MaterialProperty distortionBlendMode = null;
        protected const string kDistortionBlendMode = "_DistortionBlendMode";
        protected MaterialProperty distortionScale = null;
        protected const string kDistortionScale = "_DistortionScale";
        protected MaterialProperty distortionVectorScale = null;
        protected const string kDistortionVectorScale = "_DistortionVectorScale";
        protected MaterialProperty distortionVectorBias = null;
        protected const string kDistortionVectorBias = "_DistortionVectorBias";
        protected MaterialProperty distortionBlurScale = null;
        protected const string kDistortionBlurScale = "_DistortionBlurScale";
        protected MaterialProperty distortionBlurRemapMin = null;
        protected const string kDistortionBlurRemapMin = "_DistortionBlurRemapMin";
        protected MaterialProperty distortionBlurRemapMax = null;
        protected const string kDistortionBlurRemapMax = "_DistortionBlurRemapMax";
        protected MaterialProperty enableFogOnTransparent = null;
        protected const string kEnableFogOnTransparent = "_EnableFogOnTransparent";
        protected MaterialProperty enableBlendModePreserveSpecularLighting = null;
        protected const string kEnableBlendModePreserveSpecularLighting = "_EnableBlendModePreserveSpecularLighting";

        protected MaterialProperty enableMotionVectorForVertexAnimation = null;
        protected const string kEnableMotionVectorForVertexAnimation = "_EnableMotionVectorForVertexAnimation";

        protected const string kZTestDepthEqualForOpaque = "_ZTestDepthEqualForOpaque";
        protected const string kZTestGBuffer = "_ZTestGBuffer";
        protected const string kZTestModeDistortion = "_ZTestModeDistortion";

        // See comment in LitProperties.hlsl
        const string kEmissionColor = "_EmissionColor";

        protected virtual SurfaceType defaultSurfaceType { get { return SurfaceType.Opaque; } }

        protected virtual bool showBlendModePopup { get { return true; } }
        protected virtual bool showPreRefractionPass { get { return true; } }
        protected virtual bool showLowResolutionPass { get { return false; } } // Not implemented yet
        protected virtual bool showAfterPostProcessPass { get { return true; } }

        List<string> m_RenderingPassNames = new List<string>();
        List<int> m_RenderingPassValues = new List<int>();

        // The following set of functions are call by the ShaderGraph
        // It will allow to display our common parameters + setup keyword correctly for them
        protected abstract void FindMaterialProperties(MaterialProperty[] props);
        protected abstract void SetupMaterialKeywordsAndPassInternal(Material material);
        protected abstract void MaterialPropertiesGUI(Material material);
        protected virtual void MaterialPropertiesAdvanceGUI(Material material) {}
        protected abstract void VertexAnimationPropertiesGUI();
        // This function will say if emissive is used or not regarding enlighten/PVR
        protected virtual bool ShouldEmissionBeEnabled(Material material) { return false; }

        protected virtual void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            // Everything is optional (except surface type) so users that derive from this class can decide what they expose or not
            surfaceType = FindProperty(kSurfaceType, props, false);
            useShadowThreshold = FindProperty(kUseShadowThreshold, props, false);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled, props, false);
            alphaCutoff = FindProperty(kAlphaCutoff, props, false);

            alphaCutoffShadow = FindProperty(kAlphaCutoffShadow, props, false);
            alphaCutoffPrepass = FindProperty(kAlphaCutoffPrepass, props, false);
            alphaCutoffPostpass = FindProperty(kAlphaCutoffPostpass, props, false);
            transparentDepthPrepassEnable = FindProperty(kTransparentDepthPrepassEnable, props, false);
            transparentDepthPostpassEnable = FindProperty(kTransparentDepthPostpassEnable, props, false);
            transparentBackfaceEnable = FindProperty(kTransparentBackfaceEnable, props, false);

            transparentSortPriority = FindProperty(kTransparentSortPriority, props, false);

            doubleSidedEnable = FindProperty(kDoubleSidedEnable, props, false);
            blendMode = FindProperty(kBlendMode, props, false);

            // Distortion is optional
            distortionEnable = FindProperty(kDistortionEnable, props, false);
            distortionOnly = FindProperty(kDistortionOnly, props, false);
            distortionDepthTest = FindProperty(kDistortionDepthTest, props, false);
            distortionVectorMap = FindProperty(kDistortionVectorMap, props, false);
            distortionBlendMode = FindProperty(kDistortionBlendMode, props, false);
            distortionScale = FindProperty(kDistortionScale, props, false);
            distortionVectorScale = FindProperty(kDistortionVectorScale, props, false);
            distortionVectorBias = FindProperty(kDistortionVectorBias, props, false);
            distortionBlurScale = FindProperty(kDistortionBlurScale, props, false);
            distortionBlurRemapMin = FindProperty(kDistortionBlurRemapMin, props, false);
            distortionBlurRemapMax = FindProperty(kDistortionBlurRemapMax, props, false);

            enableFogOnTransparent = FindProperty(kEnableFogOnTransparent, props, false);
            enableBlendModePreserveSpecularLighting = FindProperty(kEnableBlendModePreserveSpecularLighting, props, false);

            enableMotionVectorForVertexAnimation = FindProperty(kEnableMotionVectorForVertexAnimation, props, false);
        }

        protected SurfaceType surfaceTypeValue
        {
            get { return surfaceType != null ? (SurfaceType)surfaceType.floatValue : defaultSurfaceType; }
        }

        int DoOpaqueRenderingPassPopup(string text, int inputValue, bool afterPost)
        {
            // Build UI enums
            m_RenderingPassNames.Clear();
            m_RenderingPassValues.Clear();

            m_RenderingPassNames.Add("Default");
            m_RenderingPassValues.Add((int)HDRenderQueue.OpaqueRenderQueue.Default);

            if (afterPost)
            {
                m_RenderingPassNames.Add("After post-process");
                m_RenderingPassValues.Add((int)HDRenderQueue.OpaqueRenderQueue.AfterPostProcessing);
            }

#if ENABLE_RAYTRACING
            m_RenderingPassNames.Add("Raytracing");
            m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.Raytracing);
#endif

            return EditorGUILayout.IntPopup(text, inputValue, m_RenderingPassNames.ToArray(), m_RenderingPassValues.ToArray());
        }

        int DoTransparentRenderingPassPopup(string text, int inputValue, bool refraction, bool lowRes, bool afterPost)
        {
            // Build UI enums
            m_RenderingPassNames.Clear();
            m_RenderingPassValues.Clear();

            if (refraction)
            {
                m_RenderingPassNames.Add("Before refraction");
                m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.BeforeRefraction);
            }

            m_RenderingPassNames.Add("Default");
            m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.Default);

            if (lowRes)
            {
                m_RenderingPassNames.Add("Low resolution");
                m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.LowResolution);
            }

            if (afterPost)
            {
                m_RenderingPassNames.Add("After post-process");
                m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.AfterPostProcessing);
            }

#if ENABLE_RAYTRACING
            m_RenderingPassNames.Add("Raytracing");
            m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.Raytracing);
#endif

            return EditorGUILayout.IntPopup(text, inputValue, m_RenderingPassNames.ToArray(), m_RenderingPassValues.ToArray());
        }

        void SurfaceTypePopup()
        {
            if (surfaceType == null)
                return;

            Material material = m_MaterialEditor.target as Material;
            var mode = (SurfaceType)surfaceType.floatValue;
            var renderQueueType = HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue);
            bool alphaTest = material.HasProperty(kAlphaCutoffEnabled) && material.GetFloat(kAlphaCutoffEnabled) > 0.0f;

            EditorGUI.showMixedValue = surfaceType.hasMixedValue;
            var newMode = (SurfaceType)EditorGUILayout.Popup(StylesBaseUnlit.surfaceTypeText, (int)mode, StylesBaseUnlit.surfaceTypeNames);
            if (newMode != mode) //EditorGUI.EndChangeCheck is called even if value remain the same after the popup. Prefer not to use it here
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Surface Type");
                surfaceType.floatValue = (float)newMode;
                HDRenderQueue.RenderQueueType targetQueueType;
                switch(newMode)
                {
                    case SurfaceType.Opaque:
                        targetQueueType = HDRenderQueue.GetOpaqueEquivalent(HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue));
                        break;
                    case SurfaceType.Transparent:
                        targetQueueType = HDRenderQueue.GetTransparentEquivalent(HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue));
                        break;
                    default:
                        throw new ArgumentException("Unknown SurfaceType");
                }
                material.renderQueue = HDRenderQueue.ChangeType(targetQueueType, (int)transparentSortPriority.floatValue, alphaTest);
            }
            EditorGUI.showMixedValue = false;

            bool isMixedRenderQueue = surfaceType.hasMixedValue || m_MaterialEditor.targets.Select(m => HDRenderQueue.GetTypeByRenderQueueValue(((Material)m).renderQueue)).Distinct().Count() > 1;
            EditorGUI.showMixedValue = isMixedRenderQueue;
            ++EditorGUI.indentLevel;
            switch (mode)
            {
                case SurfaceType.Opaque:
                    //GetOpaqueEquivalent: prevent issue when switching surface type
                    HDRenderQueue.OpaqueRenderQueue renderQueueOpaqueType = HDRenderQueue.ConvertToOpaqueRenderQueue(HDRenderQueue.GetOpaqueEquivalent(renderQueueType));
                    var newRenderQueueOpaqueType = (HDRenderQueue.OpaqueRenderQueue)DoOpaqueRenderingPassPopup(StylesBaseUnlit.renderingPassText, (int)renderQueueOpaqueType, showAfterPostProcessPass);
                    if (newRenderQueueOpaqueType != renderQueueOpaqueType) //EditorGUI.EndChangeCheck is called even if value remain the same after the popup. Prefer not to use it here
                    {
                        m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Pass");
                        renderQueueType = HDRenderQueue.ConvertFromOpaqueRenderQueue(newRenderQueueOpaqueType);
                        material.renderQueue = HDRenderQueue.ChangeType(renderQueueType, alphaTest: alphaTest);
                    }
                    break;
                case SurfaceType.Transparent:
                    //GetTransparentEquivalent: prevent issue when switching surface type
                    HDRenderQueue.TransparentRenderQueue renderQueueTransparentType = HDRenderQueue.ConvertToTransparentRenderQueue(HDRenderQueue.GetTransparentEquivalent(renderQueueType));
                    var newRenderQueueTransparentType = (HDRenderQueue.TransparentRenderQueue)DoTransparentRenderingPassPopup(StylesBaseUnlit.renderingPassText, (int)renderQueueTransparentType, showPreRefractionPass, showLowResolutionPass, showAfterPostProcessPass);
                    if (newRenderQueueTransparentType != renderQueueTransparentType) //EditorGUI.EndChangeCheck is called even if value remain the same after the popup. Prefer not to use it here
                    {
                        m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Pass");
                        renderQueueType = HDRenderQueue.ConvertFromTransparentRenderQueue(newRenderQueueTransparentType);
                        material.renderQueue = HDRenderQueue.ChangeType(renderQueueType, offset: (int)transparentSortPriority.floatValue);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }
            --EditorGUI.indentLevel;
            EditorGUI.showMixedValue = false;
        }

        private void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.IntPopup(StylesBaseUnlit.blendModeText, (int)mode, StylesBaseUnlit.blendModeNames, StylesBaseUnlit.blendModeValues);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected virtual void BaseMaterialPropertiesGUI()
        {
            SurfaceTypePopup();
            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                EditorGUI.indentLevel++;

                if (blendMode != null && showBlendModePopup)
                    BlendModePopup();

                EditorGUI.indentLevel++;
                if (enableBlendModePreserveSpecularLighting != null && blendMode != null && showBlendModePopup)
                    m_MaterialEditor.ShaderProperty(enableBlendModePreserveSpecularLighting, StylesBaseUnlit.enableBlendModePreserveSpecularLightingText);
                EditorGUI.indentLevel--;

                if (transparentSortPriority != null)
                {
                    EditorGUI.BeginChangeCheck();
                    m_MaterialEditor.ShaderProperty(transparentSortPriority, StylesBaseUnlit.transparentSortPriorityText);
                    if (EditorGUI.EndChangeCheck())
                    {
                        transparentSortPriority.floatValue = HDRenderQueue.ClampsTransparentRangePriority((int)transparentSortPriority.floatValue);
                    }
                }

                if (enableFogOnTransparent != null)
                    m_MaterialEditor.ShaderProperty(enableFogOnTransparent, StylesBaseUnlit.enableTransparentFogText);

                if (transparentBackfaceEnable != null)
                    m_MaterialEditor.ShaderProperty(transparentBackfaceEnable, StylesBaseUnlit.transparentBackfaceEnableText);

                if (transparentDepthPrepassEnable != null)
                    m_MaterialEditor.ShaderProperty(transparentDepthPrepassEnable, StylesBaseUnlit.transparentDepthPrepassEnableText);

                if (transparentDepthPostpassEnable != null)
                    m_MaterialEditor.ShaderProperty(transparentDepthPostpassEnable, StylesBaseUnlit.transparentDepthPostpassEnableText);

                EditorGUI.indentLevel--;
            }

            // This function must finish with double sided option (see LitUI.cs)
            if (doubleSidedEnable != null)
            {
                m_MaterialEditor.ShaderProperty(doubleSidedEnable, StylesBaseUnlit.doubleSidedEnableText);
            }

            if (alphaCutoffEnable != null)
                m_MaterialEditor.ShaderProperty(alphaCutoffEnable, StylesBaseUnlit.alphaCutoffEnableText);

            if (alphaCutoffEnable != null && alphaCutoffEnable.floatValue == 1.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(alphaCutoff, StylesBaseUnlit.alphaCutoffText);

                if (useShadowThreshold != null)
                    m_MaterialEditor.ShaderProperty(useShadowThreshold, StylesBaseUnlit.useShadowThresholdText);

                if (alphaCutoffShadow != null && useShadowThreshold != null && useShadowThreshold.floatValue == 1.0f)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(alphaCutoffShadow, StylesBaseUnlit.alphaCutoffShadowText);
                    EditorGUI.indentLevel--;
                }

                // With transparent object and few specific materials like Hair, we need more control on the cutoff to apply
                // This allow to get a better sorting (with prepass), better shadow (better silhouettes fidelity) etc...
                if (surfaceTypeValue == SurfaceType.Transparent)
                {
                    if (transparentDepthPrepassEnable != null && transparentDepthPrepassEnable.floatValue == 1.0f)
                    {
                        m_MaterialEditor.ShaderProperty(alphaCutoffPrepass, StylesBaseUnlit.alphaCutoffPrepassText);
                    }

                    if (transparentDepthPostpassEnable != null && transparentDepthPostpassEnable.floatValue == 1.0f)
                    {
                        m_MaterialEditor.ShaderProperty(alphaCutoffPostpass, StylesBaseUnlit.alphaCutoffPostpassText);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        protected void DoDistortionInputsGUI()
        {
            if (distortionEnable != null)
            {
                m_MaterialEditor.ShaderProperty(distortionEnable, StylesBaseUnlit.distortionEnableText);

                if (distortionEnable.floatValue == 1.0f)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(distortionBlendMode, StylesBaseUnlit.distortionBlendModeText);
                    if (distortionOnly != null)
                        m_MaterialEditor.ShaderProperty(distortionOnly, StylesBaseUnlit.distortionOnlyText);
                    m_MaterialEditor.ShaderProperty(distortionDepthTest, StylesBaseUnlit.distortionDepthTestText);

                    EditorGUI.indentLevel++;
                    m_MaterialEditor.TexturePropertySingleLine(StylesBaseUnlit.distortionVectorMapText, distortionVectorMap, distortionVectorScale, distortionVectorBias);
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(distortionScale, StylesBaseUnlit.distortionScaleText);
                    m_MaterialEditor.ShaderProperty(distortionBlurScale, StylesBaseUnlit.distortionBlurScaleText);
                    float remapMin = distortionBlurRemapMin.floatValue;
                    float remapMax = distortionBlurRemapMax.floatValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(StylesBaseUnlit.distortionBlurRemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        distortionBlurRemapMin.floatValue = remapMin;
                        distortionBlurRemapMax.floatValue = remapMax;
                    }
                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel--;
                }
            }
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if ocde change
        static public void SetupBaseUnlitKeywords(Material material)
        {
            bool alphaTestEnable = material.HasProperty(kAlphaCutoffEnabled) && material.GetFloat(kAlphaCutoffEnabled) > 0.0f;
            CoreUtils.SetKeyword(material, "_ALPHATEST_ON", alphaTestEnable);

            SurfaceType surfaceType = material.HasProperty(kSurfaceType) ? (SurfaceType)material.GetFloat(kSurfaceType) : SurfaceType.Opaque;
            CoreUtils.SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", surfaceType == SurfaceType.Transparent);

            bool enableBlendModePreserveSpecularLighting = (surfaceType == SurfaceType.Transparent) && material.HasProperty(kEnableBlendModePreserveSpecularLighting) && material.GetFloat(kEnableBlendModePreserveSpecularLighting) > 0.0f;
            CoreUtils.SetKeyword(material, "_BLENDMODE_PRESERVE_SPECULAR_LIGHTING", enableBlendModePreserveSpecularLighting);

            // These need to always been set either with opaque or transparent! So a users can switch to opaque and remove the keyword correctly
            CoreUtils.SetKeyword(material, "_BLENDMODE_ALPHA", false);
            CoreUtils.SetKeyword(material, "_BLENDMODE_ADD", false);
            CoreUtils.SetKeyword(material, "_BLENDMODE_PRE_MULTIPLY", false);

            HDRenderQueue.RenderQueueType renderQueueType = HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue);
            bool needOffScreenBlendFactor = renderQueueType == HDRenderQueue.RenderQueueType.AfterPostprocessTransparent || renderQueueType == HDRenderQueue.RenderQueueType.LowTransparent;

            // Alpha tested materials always have a prepass where we perform the clip.
            // Then during Gbuffer pass we don't perform the clip test, so we need to use depth equal in this case.
            if (alphaTestEnable)
            {
                material.SetInt(kZTestGBuffer, (int)UnityEngine.Rendering.CompareFunction.Equal);
            }
            else
            {
                material.SetInt(kZTestGBuffer, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }

            // If the material use the kZTestDepthEqualForOpaque it mean it require depth equal test for opaque but transparent are not affected
            if (material.HasProperty(kZTestDepthEqualForOpaque))
            {
                if (surfaceType == SurfaceType.Opaque)
                    material.SetInt(kZTestDepthEqualForOpaque, (int)UnityEngine.Rendering.CompareFunction.Equal);
                else
                    material.SetInt(kZTestDepthEqualForOpaque, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }

            if (surfaceType == SurfaceType.Opaque)
            {
                material.SetOverrideTag("RenderType", alphaTestEnable ? "TransparentCutout" : "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
            }
            else
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_ZWrite", 0);

                if (material.HasProperty(kBlendMode))
                {
                    BlendMode blendMode = (BlendMode)material.GetFloat(kBlendMode);

                    CoreUtils.SetKeyword(material, "_BLENDMODE_ALPHA", BlendMode.Alpha == blendMode);
                    CoreUtils.SetKeyword(material, "_BLENDMODE_ADD", BlendMode.Additive == blendMode);
                    CoreUtils.SetKeyword(material, "_BLENDMODE_PRE_MULTIPLY", BlendMode.Premultiply == blendMode);

                    // When doing off-screen transparency accumulation, we change blend factors as described here: https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
                    switch (blendMode)
                    {
                        // Alpha
                        // color: src * src_a + dst * (1 - src_a)
                        // src * src_a is done in the shader as it allow to reduce precision issue when using _BLENDMODE_PRESERVE_SPECULAR_LIGHTING (See Material.hlsl)
                        case BlendMode.Alpha:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            if (needOffScreenBlendFactor)
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            else
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            break;

                        // Additive
                        // color: src * src_a + dst
                        // src * src_a is done in the shader
                        case BlendMode.Additive:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            if (needOffScreenBlendFactor)
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            }
                            else
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            }
                            break;

                        // PremultipliedAlpha
                        // color: src * src_a + dst * (1 - src_a)
                        // src is supposed to have been multiplied by alpha in the texture on artists side.
                        case BlendMode.Premultiply:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            if (needOffScreenBlendFactor)
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            else
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            break;
                    }
                }
            }

            bool fogEnabled = material.HasProperty(kEnableFogOnTransparent) && material.GetFloat(kEnableFogOnTransparent) > 0.0f && surfaceType == SurfaceType.Transparent;
            CoreUtils.SetKeyword(material, "_ENABLE_FOG_ON_TRANSPARENT", fogEnabled);

            if (material.HasProperty(kDistortionEnable))
            {
                bool distortionDepthTest = material.GetFloat(kDistortionDepthTest) > 0.0f;
                if (material.HasProperty(kZTestModeDistortion))
                {
                    if (distortionDepthTest)
                    {
                        material.SetInt(kZTestModeDistortion, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                    }
                    else
                    {
                        material.SetInt(kZTestModeDistortion, (int)UnityEngine.Rendering.CompareFunction.Always);
                    }
                }

                var distortionBlendMode = material.GetInt(kDistortionBlendMode);
                switch (distortionBlendMode)
                {
                    default:
                    case 0: // Add
                        material.SetInt("_DistortionSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionDstBlend", (int)UnityEngine.Rendering.BlendMode.One);

                        material.SetInt("_DistortionBlurSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionBlurDstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionBlurBlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                        break;

                    case 1: // Multiply
                        material.SetInt("_DistortionSrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DistortionDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);

                        material.SetInt("_DistortionBlurSrcBlend", (int)UnityEngine.Rendering.BlendMode.DstAlpha);
                        material.SetInt("_DistortionBlurDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_DistortionBlurBlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                        break;

                    case 2: // Replace
                        material.SetInt("_DistortionSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);

                        material.SetInt("_DistortionBlurSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionBlurDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_DistortionBlurBlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                        break;
                }
            }

            bool isBackFaceEnable = material.HasProperty(kTransparentBackfaceEnable) && material.GetFloat(kTransparentBackfaceEnable) > 0.0f && surfaceType == SurfaceType.Transparent;
            bool doubleSidedEnable = material.HasProperty(kDoubleSidedEnable) && material.GetFloat(kDoubleSidedEnable) > 0.0f;

            // Disable culling if double sided
            material.SetInt("_CullMode", doubleSidedEnable ? (int)UnityEngine.Rendering.CullMode.Off : (int)UnityEngine.Rendering.CullMode.Back);

            // We have a separate cullmode (_CullModeForward) for Forward in case we use backface then frontface rendering, need to configure it
            if (isBackFaceEnable)
            {
                material.SetInt("_CullModeForward", (int)UnityEngine.Rendering.CullMode.Back);
            }
            else
            {
                material.SetInt("_CullModeForward", doubleSidedEnable ? (int)UnityEngine.Rendering.CullMode.Off : (int)UnityEngine.Rendering.CullMode.Back);
            }

            CoreUtils.SetKeyword(material, "_DOUBLESIDED_ON", doubleSidedEnable);

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            if (material.HasProperty(kEmissionColor))
                material.SetColor(kEmissionColor, Color.white); // kEmissionColor must always be white to allow our own material to control the GI (this allow to fallback from builtin unity to our system).
                                                                // as it happen with old material that it isn't the case, we force it.
            MaterialEditor.FixupEmissiveFlag(material);

            // Commented out for now because unfortunately we used the hard coded property names used by the GI system for our own parameters
            // So we need a way to work around that before we activate this.
            SetupMainTexForAlphaTestGI("_EmissiveColorMap", "_EmissiveColor", material);

            // DoubleSidedGI has to be synced with our double sided toggle
            var serializedObject = new SerializedObject(material);
            var doubleSidedGIppt = serializedObject.FindProperty("m_DoubleSidedGI");
            doubleSidedGIppt.boolValue = doubleSidedEnable;
            serializedObject.ApplyModifiedProperties();
        }

        // This is a hack for GI. PVR looks in the shader for a texture named "_MainTex" to extract the opacity of the material for baking. In the same manner, "_Cutoff" and "_Color" are also necessary.
        // Since we don't have those parameters in our shaders we need to provide a "fake" useless version of them with the right values for the GI to work.
        protected static void SetupMainTexForAlphaTestGI(string colorMapPropertyName, string colorPropertyName, Material material)
        {
            if (material.HasProperty(colorMapPropertyName))
            {
                var mainTex = material.GetTexture(colorMapPropertyName);
                material.SetTexture("_MainTex", mainTex);
            }

            if (material.HasProperty(colorPropertyName))
            {
                var color = material.GetColor(colorPropertyName);
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_AlphaCutoff")) // Same for all our materials
            {
                var cutoff = material.GetFloat("_AlphaCutoff");
                material.SetFloat("_Cutoff", cutoff);
            }
        }

        static public void SetupBaseUnlitMaterialPass(Material material)
        {
            if (material.HasProperty(kDistortionEnable))
            {
                bool distortionEnable = material.GetFloat(kDistortionEnable) > 0.0f && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);

                bool distortionOnly = false;
                if (material.HasProperty(kDistortionOnly))
                {
                    distortionOnly = material.GetFloat(kDistortionOnly) > 0.0f;
                }

                // If distortion only is enabled, disable all passes (except distortion and debug)
                bool enablePass = !(distortionEnable && distortionOnly);

                // Disable all passes except distortion
                // Distortion is setup in code above
                material.SetShaderPassEnabled(HDShaderPassNames.s_ForwardStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_DepthOnlyStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_DepthForwardOnlyStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_ForwardOnlyStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_GBufferStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_GBufferWithPrepassStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_DistortionVectorsStr, distortionEnable); // note: use distortionEnable
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentBackfaceStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPostpassStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_MetaStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_ShadowCasterStr, enablePass);
            }

            if (material.HasProperty(kTransparentDepthPrepassEnable))
            {
                bool depthWriteEnable = (material.GetFloat(kTransparentDepthPrepassEnable) > 0.0f) && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr, depthWriteEnable);
            }

            if (material.HasProperty(kTransparentDepthPostpassEnable))
            {
                bool depthWriteEnable = (material.GetFloat(kTransparentDepthPostpassEnable) > 0.0f) && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPostpassStr, depthWriteEnable);
            }

            if (material.HasProperty(kTransparentBackfaceEnable))
            {
                bool backFaceEnable = (material.GetFloat(kTransparentBackfaceEnable) > 0.0f) && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentBackfaceStr, backFaceEnable);
            }

            if (material.HasProperty(kEnableMotionVectorForVertexAnimation))
            {
                material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, material.GetFloat(kEnableMotionVectorForVertexAnimation) > 0.0f);
            }
            else
            {
                // In case we have an HDRP material that inherits from this UI but does not have an _EnableMotionVectorForVertexAnimation property, we need to set it to false (default behavior)
                material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, false);
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
                    m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true, true);
                }
            }
        }

        public virtual void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                using (var header = new HeaderScope(StylesBaseUnlit.optionText, (uint)Expandable.Base, this))
                {
                    if (header.expanded)
                        BaseMaterialPropertiesGUI();
                }
                VertexAnimationPropertiesGUI();
                MaterialPropertiesGUI(material);
                using (var header = new HeaderScope(StylesBaseUnlit.advancedText, (uint)Expandable.Advance, this))
                {
                    if(header.expanded)
                    {
                        m_MaterialEditor.EnableInstancingField();
                        MaterialPropertiesAdvanceGUI(material);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
            }
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // When switching shader, the custom RenderQueue is reset due to shader assignment
            // To keep the correct render queue we need to save it here, do the change and re-assign it
            int currentRenderQueue = material.renderQueue;
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            material.renderQueue = currentRenderQueue;

            SetupMaterialKeywordsAndPassInternal(material);
        }

        // This is call by the inspector
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;

            // We should always register the key used to keep collapsable state
            InitExpandableState(materialEditor);

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

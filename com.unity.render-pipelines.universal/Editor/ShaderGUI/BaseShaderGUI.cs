using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;
using RenderQueue = UnityEngine.Rendering.RenderQueue;

namespace UnityEditor
{
    public abstract class BaseShaderGUI : ShaderGUI
    {
        #region EnumsAndClasses

        [Flags]
        [URPHelpURL("shaders-in-universalrp")]
        protected enum Expandable
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Advanced = 1 << 2,
            Details = 1 << 3,
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum BlendMode
        {
            Alpha,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
            Additive,
            Multiply
        }

        public enum SmoothnessSource
        {
            SpecularAlpha,
            BaseAlpha,
        }

        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        public enum QueueControl
        {
            Auto = 0,
            UserOverride = 1
        }

        protected class Styles
        {
            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));
            public static readonly string[] renderFaceNames = Enum.GetNames(typeof(RenderFace));
            public static readonly string[] zwriteNames = Enum.GetNames(typeof(UnityEditor.Rendering.Universal.ShaderGraph.ZWriteControl));
            public static readonly string[] queueControlNames = Enum.GetNames(typeof(QueueControl));

            // need to skip the first entry for ztest (ZTestMode.Disabled is not a valid value)
            public static readonly int[] ztestValues = ((int[])Enum.GetValues(typeof(UnityEditor.Rendering.Universal.ShaderGraph.ZTestMode))).Skip(1).ToArray();
            public static readonly string[] ztestNames = Enum.GetNames(typeof(UnityEditor.Rendering.Universal.ShaderGraph.ZTestMode)).Skip(1).ToArray();

            // Categories
            public static readonly GUIContent SurfaceOptions =
                EditorGUIUtility.TrTextContent("Surface Options", "Controls how URP Renders the material on screen.");

            public static readonly GUIContent SurfaceInputs = EditorGUIUtility.TrTextContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            public static readonly GUIContent AdvancedLabel = EditorGUIUtility.TrTextContent("Advanced Options",
                "These settings affect behind-the-scenes rendering and underlying calculations.");

            public static readonly GUIContent surfaceType = EditorGUIUtility.TrTextContent("Surface Type",
                "Select a surface type for your texture. Choose between Opaque or Transparent.");

            public static readonly GUIContent blendingMode = EditorGUIUtility.TrTextContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");

            public static readonly GUIContent preserveSpecularText = EditorGUIUtility.TrTextContent("Preserve Specular Lighting",
                "Preserves specular lighting intensity and size by not applying transparent alpha to the specular light contribution.");

            public static readonly GUIContent cullingText = EditorGUIUtility.TrTextContent("Render Face",
                "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");

            public static readonly GUIContent zwriteText = EditorGUIUtility.TrTextContent("Depth Write",
                "Controls whether the shader writes depth.  Auto will write only when the shader is opaque.");

            public static readonly GUIContent ztestText = EditorGUIUtility.TrTextContent("Depth Test",
                "Specifies the depth test mode.  The default is LEqual.");

            public static readonly GUIContent alphaClipText = EditorGUIUtility.TrTextContent("Alpha Clipping",
                "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas.");

            public static readonly GUIContent alphaClipThresholdText = EditorGUIUtility.TrTextContent("Threshold",
                "Sets where the Alpha Clipping starts. The higher the value is, the brighter the  effect is when clipping starts.");

            public static readonly GUIContent castShadowText = EditorGUIUtility.TrTextContent("Cast Shadows",
                "When enabled, this GameObject will cast shadows onto any geometry that can receive them.");

            public static readonly GUIContent receiveShadowText = EditorGUIUtility.TrTextContent("Receive Shadows",
                "When enabled, other GameObjects can cast shadows onto this GameObject.");

            public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map",
                "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");

            public static readonly GUIContent emissionMap = EditorGUIUtility.TrTextContent("Emission Map",
                "Determines the color and intensity of light that the surface of the material emits.");

            public static readonly GUIContent normalMapText =
                EditorGUIUtility.TrTextContent("Normal Map", "Designates a Normal Map to create the illusion of bumps and dents on this Material's surface.");

            public static readonly GUIContent bumpScaleNotSupported =
                EditorGUIUtility.TrTextContent("Bump scale is not supported on mobile platforms");

            public static readonly GUIContent fixNormalNow = EditorGUIUtility.TrTextContent("Fix now",
                "Converts the assigned texture to be a normal map format.");

            public static readonly GUIContent queueSlider = EditorGUIUtility.TrTextContent("Sorting Priority",
                "Determines the chronological rendering order for a Material. Materials with lower value are rendered first.");

            public static readonly GUIContent queueControl = EditorGUIUtility.TrTextContent("Queue Control",
                "Controls whether render queue is automatically set based on material surface type, or explicitly set by the user.");

            public static readonly GUIContent documentationIcon = EditorGUIUtility.TrIconContent("_Help", $"Open Reference for URP Shaders.");
        }

        #endregion

        #region Variables

        protected MaterialEditor materialEditor { get; set; }

        protected MaterialProperty surfaceTypeProp { get; set; }

        protected MaterialProperty blendModeProp { get; set; }
        protected MaterialProperty preserveSpecProp { get; set; }

        protected MaterialProperty cullingProp { get; set; }

        protected MaterialProperty ztestProp { get; set; }

        protected MaterialProperty zwriteProp { get; set; }

        protected MaterialProperty alphaClipProp { get; set; }

        protected MaterialProperty alphaCutoffProp { get; set; }

        protected MaterialProperty castShadowsProp { get; set; }

        protected MaterialProperty receiveShadowsProp { get; set; }

        // Common Surface Input properties

        protected MaterialProperty baseMapProp { get; set; }

        protected MaterialProperty baseColorProp { get; set; }

        protected MaterialProperty emissionMapProp { get; set; }

        protected MaterialProperty emissionColorProp { get; set; }

        protected MaterialProperty queueOffsetProp { get; set; }

        protected MaterialProperty queueControlProp { get; set; }

        public bool m_FirstTimeApply = true;

        // By default, everything is expanded, except advanced
        readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);

        #endregion

        private const int queueOffsetRange = 50;

        ////////////////////////////////////
        // General Functions              //
        ////////////////////////////////////
        #region GeneralFunctions

        [Obsolete("MaterialChanged has been renamed ValidateMaterial", false)]
        public virtual void MaterialChanged(Material material)
        {
            ValidateMaterial(material);
        }

        public virtual void FindProperties(MaterialProperty[] properties)
        {
            var material = materialEditor?.target as Material;
            if (material == null)
                return;

            surfaceTypeProp = FindProperty(Property.SurfaceType, properties, false);
            blendModeProp = FindProperty(Property.BlendMode, properties, false);
            preserveSpecProp = FindProperty(Property.BlendModePreserveSpecular, properties, false);  // Separate blend for diffuse and specular.
            cullingProp = FindProperty(Property.CullMode, properties, false);
            zwriteProp = FindProperty(Property.ZWriteControl, properties, false);
            ztestProp = FindProperty(Property.ZTest, properties, false);
            alphaClipProp = FindProperty(Property.AlphaClip, properties, false);

            // ShaderGraph Lit and Unlit Subtargets only
            castShadowsProp = FindProperty(Property.CastShadows, properties, false);
            queueControlProp = FindProperty(Property.QueueControl, properties, false);

            // ShaderGraph Lit, and Lit.shader
            receiveShadowsProp = FindProperty(Property.ReceiveShadows, properties, false);

            // The following are not mandatory for shadergraphs (it's up to the user to add them to their graph)
            alphaCutoffProp = FindProperty("_Cutoff", properties, false);
            baseMapProp = FindProperty("_BaseMap", properties, false);
            baseColorProp = FindProperty("_BaseColor", properties, false);
            emissionMapProp = FindProperty(Property.EmissionMap, properties, false);
            emissionColorProp = FindProperty(Property.EmissionColor, properties, false);
            queueOffsetProp = FindProperty(Property.QueueOffset, properties, false);
        }

        public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            if (materialEditorIn == null)
                throw new ArgumentNullException("materialEditorIn");

            materialEditor = materialEditorIn;
            Material material = materialEditor.target as Material;

            FindProperties(properties);   // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a universal shader.
            if (m_FirstTimeApply)
            {
                OnOpenGUI(material, materialEditorIn);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        protected virtual uint materialFilter => uint.MaxValue;

        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            var filter = (Expandable)materialFilter;

            // Generate the foldouts
            if (filter.HasFlag(Expandable.SurfaceOptions))
                m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceOptions, (uint)Expandable.SurfaceOptions, DrawSurfaceOptions);

            if (filter.HasFlag(Expandable.SurfaceInputs))
                m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceInputs, (uint)Expandable.SurfaceInputs, DrawSurfaceInputs);

            if (filter.HasFlag(Expandable.Details))
                FillAdditionalFoldouts(m_MaterialScopeList);

            if (filter.HasFlag(Expandable.Advanced))
                m_MaterialScopeList.RegisterHeaderScope(Styles.AdvancedLabel, (uint)Expandable.Advanced, DrawAdvancedOptions);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            m_MaterialScopeList.DrawHeaders(materialEditor, material);
        }

        #endregion
        ////////////////////////////////////
        // Drawing Functions              //
        ////////////////////////////////////
        #region DrawingFunctions

        internal void DrawShaderGraphProperties(Material material, IEnumerable<MaterialProperty> properties)
        {
            if (properties == null)
                return;

            ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, properties);
        }

        internal static void DrawFloatToggleProperty(GUIContent styles, MaterialProperty prop, int indentLevel = 0, bool isDisabled = false)
        {
            if (prop == null)
                return;

            EditorGUI.BeginDisabledGroup(isDisabled);
            EditorGUI.indentLevel += indentLevel;
            EditorGUI.BeginChangeCheck();
            MaterialEditor.BeginProperty(prop);
            bool newValue = EditorGUILayout.Toggle(styles, prop.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue ? 1.0f : 0.0f;
            MaterialEditor.EndProperty();
            EditorGUI.indentLevel -= indentLevel;
            EditorGUI.EndDisabledGroup();
        }

        public virtual void DrawSurfaceOptions(Material material)
        {
            DoPopup(Styles.surfaceType, surfaceTypeProp, Styles.surfaceTypeNames);
            if ((surfaceTypeProp != null) && ((SurfaceType)surfaceTypeProp.floatValue == SurfaceType.Transparent))
            {
                DoPopup(Styles.blendingMode, blendModeProp, Styles.blendModeNames);

                if (material.HasProperty(Property.BlendModePreserveSpecular))
                {
                    BlendMode blendMode = (BlendMode)material.GetFloat(Property.BlendMode);
                    var isDisabled = blendMode == BlendMode.Multiply || blendMode == BlendMode.Premultiply;
                    if (!isDisabled)
                        DrawFloatToggleProperty(Styles.preserveSpecularText, preserveSpecProp, 1, isDisabled);
                }
            }
            DoPopup(Styles.cullingText, cullingProp, Styles.renderFaceNames);
            DoPopup(Styles.zwriteText, zwriteProp, Styles.zwriteNames);

            if (ztestProp != null)
                materialEditor.IntPopupShaderProperty(ztestProp, Styles.ztestText.text, Styles.ztestNames, Styles.ztestValues);

            DrawFloatToggleProperty(Styles.alphaClipText, alphaClipProp);

            if ((alphaClipProp != null) && (alphaCutoffProp != null) && (alphaClipProp.floatValue == 1))
                materialEditor.ShaderProperty(alphaCutoffProp, Styles.alphaClipThresholdText, 1);

            DrawFloatToggleProperty(Styles.castShadowText, castShadowsProp);
            DrawFloatToggleProperty(Styles.receiveShadowText, receiveShadowsProp);
        }

        public virtual void DrawSurfaceInputs(Material material)
        {
            DrawBaseProperties(material);
        }

        public virtual void DrawAdvancedOptions(Material material)
        {
            // Only draw the sorting priority field if queue control is set to "auto"
            bool autoQueueControl = GetAutomaticQueueControlSetting(material);
            if (autoQueueControl)
                DrawQueueOffsetField();
            materialEditor.EnableInstancingField();
        }

        protected void DrawQueueOffsetField()
        {
            if (queueOffsetProp != null)
                materialEditor.IntSliderShaderProperty(queueOffsetProp, -queueOffsetRange, queueOffsetRange, Styles.queueSlider);
        }

        public virtual void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList) { }

        public virtual void DrawBaseProperties(Material material)
        {
            if (baseMapProp != null && baseColorProp != null) // Draw the baseMap, most shader will have at least a baseMap
            {
                materialEditor.TexturePropertySingleLine(Styles.baseMap, baseMapProp, baseColorProp);
            }
        }

        private void DrawEmissionTextureProperty()
        {
            if ((emissionMapProp == null) || (emissionColorProp == null))
                return;

            using (new EditorGUI.IndentLevelScope(2))
            {
                materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProp, emissionColorProp, false);
            }
        }

        protected virtual void DrawEmissionProperties(Material material, bool keyword)
        {
            var emissive = true;

            if (!keyword)
            {
                DrawEmissionTextureProperty();
            }
            else
            {
                emissive = materialEditor.EmissionEnabledProperty();
                using (new EditorGUI.DisabledScope(!emissive))
                {
                    DrawEmissionTextureProperty();
                }
            }

            // If texture was assigned and color was black set color to white
            if ((emissionMapProp != null) && (emissionColorProp != null))
            {
                var hadEmissionTexture = emissionMapProp?.textureValue != null;
                var brightness = emissionColorProp.colorValue.maxColorComponent;
                if (emissionMapProp.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                    emissionColorProp.colorValue = Color.white;
            }

            if (emissive)
            {
                // Change the GI emission flag and fix it up with emissive as black if necessary.
                materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
            }
        }

        public static void DrawNormalArea(MaterialEditor materialEditor, MaterialProperty bumpMap, MaterialProperty bumpMapScale = null)
        {
            if (bumpMapScale != null)
            {
                materialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap,
                    bumpMap.textureValue != null ? bumpMapScale : null);
                if (bumpMapScale.floatValue != 1 &&
                    UnityEditorInternal.InternalEditorUtility.IsMobilePlatform(
                        EditorUserBuildSettings.activeBuildTarget))
                    if (materialEditor.HelpBoxWithButton(Styles.bumpScaleNotSupported, Styles.fixNormalNow))
                        bumpMapScale.floatValue = 1;
            }
            else
            {
                materialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap);
            }
        }

        protected static void DrawTileOffset(MaterialEditor materialEditor, MaterialProperty textureProp)
        {
            if (textureProp != null)
                materialEditor.TextureScaleOffsetProperty(textureProp);
        }

        #endregion
        ////////////////////////////////////
        // Material Data Functions        //
        ////////////////////////////////////
        #region MaterialDataFunctions

        // this function is shared with ShaderGraph Lit/Unlit GUIs and also the hand-written GUIs
        internal static void UpdateMaterialSurfaceOptions(Material material, bool automaticRenderQueue)
        {
            // Setup blending - consistent across all Universal RP shaders
            SetupMaterialBlendModeInternal(material, out int renderQueue);

            // apply automatic render queue
            if (automaticRenderQueue && (renderQueue != material.renderQueue))
                material.renderQueue = renderQueue;

            bool isShaderGraph = material.IsShaderGraph();

            // Cast Shadows
            bool castShadows = true;
            if (material.HasProperty(Property.CastShadows))
            {
                castShadows = (material.GetFloat(Property.CastShadows) != 0.0f);
            }
            else
            {
                if (isShaderGraph)
                {
                    // Lit.shadergraph or Unlit.shadergraph, but no material control defined
                    // enable the pass in the material, so shader can decide...
                    castShadows = true;
                }
                else
                {
                    // Lit.shader or Unlit.shader -- set based on transparency
                    castShadows = Rendering.Universal.ShaderGUI.LitGUI.IsOpaque(material);
                }
            }
            material.SetShaderPassEnabled("ShadowCaster", castShadows);

            // Receive Shadows
            if (material.HasProperty(Property.ReceiveShadows))
                CoreUtils.SetKeyword(material, ShaderKeywordStrings._RECEIVE_SHADOWS_OFF, material.GetFloat(Property.ReceiveShadows) == 0.0f);
        }

        // this function is shared between ShaderGraph and hand-written GUIs
        internal static void UpdateMaterialRenderQueueControl(Material material)
        {
            //
            // Render Queue Control handling
            //
            // Check for a raw render queue (the actual serialized setting - material.renderQueue has already been converted)
            // setting of -1, indicating that the material property should be inherited from the shader.
            // If we find this, add a new property "render queue control" set to 0 so we will
            // always know to follow the surface type of the material (this matches the hand-written behavior)
            // If we find another value, add the the property set to 1 so we will know that the
            // user has explicitly selected a render queue and we should not override it.
            //
            bool isShaderGraph = material.IsShaderGraph(); // Non-shadergraph materials use automatic behavior
            int rawRenderQueue = MaterialAccess.ReadMaterialRawRenderQueue(material);
            if (!isShaderGraph || rawRenderQueue == -1)
            {
                material.SetFloat(Property.QueueControl, (float)QueueControl.Auto); // Automatic behavior - surface type override
            }
            else
            {
                material.SetFloat(Property.QueueControl, (float)QueueControl.UserOverride); // User has selected explicit render queue
            }
        }

        internal static bool GetAutomaticQueueControlSetting(Material material)
        {
            // If a Shader Graph material doesn't yet have the queue control property,
            // we should not engage automatic behavior until the shader gets reimported.
            bool automaticQueueControl = !material.IsShaderGraph();
            if (material.HasProperty(Property.QueueControl))
            {
                var queueControl = material.GetFloat(Property.QueueControl);
                if (queueControl < 0.0f)
                {
                    // The property was added with a negative value, indicating it needs to be validated for this material
                    UpdateMaterialRenderQueueControl(material);
                }
                automaticQueueControl = (material.GetFloat(Property.QueueControl) == (float)QueueControl.Auto);
            }
            return automaticQueueControl;
        }

        // this is the function used by Lit.shader, Unlit.shader GUIs
        public static void SetMaterialKeywords(Material material, Action<Material> shadingModelFunc = null, Action<Material> shaderFunc = null)
        {
            UpdateMaterialSurfaceOptions(material, automaticRenderQueue: true);

            // Setup double sided GI based on Cull state
            if (material.HasProperty(Property.CullMode))
                material.doubleSidedGI = (RenderFace)material.GetFloat(Property.CullMode) != RenderFace.Front;

            // Temporary fix for lightmapping. TODO: to be replaced with attribute tag.
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", material.GetTexture("_BaseMap"));
                material.SetTextureScale("_MainTex", material.GetTextureScale("_BaseMap"));
                material.SetTextureOffset("_MainTex", material.GetTextureOffset("_BaseMap"));
            }
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", material.GetColor("_BaseColor"));

            // Emission
            if (material.HasProperty(Property.EmissionColor))
                MaterialEditor.FixupEmissiveFlag(material);

            bool shouldEmissionBeEnabled =
                (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;

            // Not sure what this is used for, I don't see this property declared by any Unity shader in our repo...
            // I'm guessing it is some kind of legacy material upgrade support thing?  Or maybe just dead code now...
            if (material.HasProperty("_EmissionEnabled") && !shouldEmissionBeEnabled)
                shouldEmissionBeEnabled = material.GetFloat("_EmissionEnabled") >= 0.5f;

            CoreUtils.SetKeyword(material, ShaderKeywordStrings._EMISSION, shouldEmissionBeEnabled);

            // Normal Map
            if (material.HasProperty("_BumpMap"))
                CoreUtils.SetKeyword(material, ShaderKeywordStrings._NORMALMAP, material.GetTexture("_BumpMap"));

            // Shader specific keyword functions
            shadingModelFunc?.Invoke(material);
            shaderFunc?.Invoke(material);
        }

        internal static void SetMaterialSrcDstBlendProperties(Material material, UnityEngine.Rendering.BlendMode srcBlend, UnityEngine.Rendering.BlendMode dstBlend)
        {
            if (material.HasProperty(Property.SrcBlend))
                material.SetFloat(Property.SrcBlend, (float)srcBlend);

            if (material.HasProperty(Property.DstBlend))
                material.SetFloat(Property.DstBlend, (float)dstBlend);

            if (material.HasProperty(Property.SrcBlendAlpha))
                material.SetFloat(Property.SrcBlendAlpha, (float)srcBlend);

            if (material.HasProperty(Property.DstBlendAlpha))
                material.SetFloat(Property.DstBlendAlpha, (float)dstBlend);
        }

        internal static void SetMaterialSrcDstBlendProperties(Material material, UnityEngine.Rendering.BlendMode srcBlendRGB, UnityEngine.Rendering.BlendMode dstBlendRGB, UnityEngine.Rendering.BlendMode srcBlendAlpha, UnityEngine.Rendering.BlendMode dstBlendAlpha)
        {
            if (material.HasProperty(Property.SrcBlend))
                material.SetFloat(Property.SrcBlend, (float)srcBlendRGB);

            if (material.HasProperty(Property.DstBlend))
                material.SetFloat(Property.DstBlend, (float)dstBlendRGB);

            if (material.HasProperty(Property.SrcBlendAlpha))
                material.SetFloat(Property.SrcBlendAlpha, (float)srcBlendAlpha);

            if (material.HasProperty(Property.DstBlendAlpha))
                material.SetFloat(Property.DstBlendAlpha, (float)dstBlendAlpha);
        }

        internal static void SetMaterialZWriteProperty(Material material, bool zwriteEnabled)
        {
            if (material.HasProperty(Property.ZWrite))
                material.SetFloat(Property.ZWrite, zwriteEnabled ? 1.0f : 0.0f);
        }

        internal static void SetupMaterialBlendModeInternal(Material material, out int automaticRenderQueue)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool alphaClip = false;
            if (material.HasProperty(Property.AlphaClip))
                alphaClip = material.GetFloat(Property.AlphaClip) >= 0.5;
            CoreUtils.SetKeyword(material, ShaderKeywordStrings._ALPHATEST_ON, alphaClip);

            // default is to use the shader render queue
            int renderQueue = material.shader.renderQueue;
            material.SetOverrideTag("RenderType", "");      // clear override tag
            if (material.HasProperty(Property.SurfaceType))
            {
                SurfaceType surfaceType = (SurfaceType)material.GetFloat(Property.SurfaceType);
                bool zwrite = false;
                CoreUtils.SetKeyword(material, ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT, surfaceType == SurfaceType.Transparent);
                if (surfaceType == SurfaceType.Opaque)
                {
                    if (alphaClip)
                    {
                        renderQueue = (int)RenderQueue.AlphaTest;
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                    }
                    else
                    {
                        renderQueue = (int)RenderQueue.Geometry;
                        material.SetOverrideTag("RenderType", "Opaque");
                    }

                    SetMaterialSrcDstBlendProperties(material, UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero);
                    zwrite = true;
                    material.DisableKeyword(ShaderKeywordStrings._ALPHAPREMULTIPLY_ON);
                    material.DisableKeyword(ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT);
                    material.DisableKeyword(ShaderKeywordStrings._ALPHAMODULATE_ON);
                }
                else // SurfaceType Transparent
                {
                    BlendMode blendMode = (BlendMode)material.GetFloat(Property.BlendMode);

                    // Clear blend keyword state.
                    material.DisableKeyword(ShaderKeywordStrings._ALPHAPREMULTIPLY_ON);
                    material.DisableKeyword(ShaderKeywordStrings._ALPHAMODULATE_ON);

                    var srcBlendRGB = UnityEngine.Rendering.BlendMode.One;
                    var dstBlendRGB = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    var srcBlendA = UnityEngine.Rendering.BlendMode.One;
                    var dstBlendA = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

                    // Specific Transparent Mode Settings
                    switch (blendMode)
                    {
                        // srcRGB * srcAlpha + dstRGB * (1 - srcAlpha)
                        // preserve spec:
                        // srcRGB * (<in shader> ? 1 : srcAlpha) + dstRGB * (1 - srcAlpha)
                        case BlendMode.Alpha:
                            srcBlendRGB = UnityEngine.Rendering.BlendMode.SrcAlpha;
                            dstBlendRGB = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                            srcBlendA = UnityEngine.Rendering.BlendMode.One;
                            dstBlendA = dstBlendRGB;
                            break;

                        // srcRGB < srcAlpha, (alpha multiplied in asset)
                        // srcRGB * 1 + dstRGB * (1 - srcAlpha)
                        case BlendMode.Premultiply:
                            srcBlendRGB = UnityEngine.Rendering.BlendMode.One;
                            dstBlendRGB = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                            srcBlendA = srcBlendRGB;
                            dstBlendA = dstBlendRGB;
                            break;

                        // srcRGB * srcAlpha + dstRGB * 1, (alpha controls amount of addition)
                        // preserve spec:
                        // srcRGB * (<in shader> ? 1 : srcAlpha) + dstRGB * (1 - srcAlpha)
                        case BlendMode.Additive:
                            srcBlendRGB = UnityEngine.Rendering.BlendMode.SrcAlpha;
                            dstBlendRGB = UnityEngine.Rendering.BlendMode.One;
                            srcBlendA = UnityEngine.Rendering.BlendMode.One;
                            dstBlendA = dstBlendRGB;
                            break;

                        // srcRGB * 0 + dstRGB * srcRGB
                        // in shader alpha controls amount of multiplication, lerp(1, srcRGB, srcAlpha)
                        // Multiply affects color only, keep existing alpha.
                        case BlendMode.Multiply:
                            srcBlendRGB = UnityEngine.Rendering.BlendMode.DstColor;
                            dstBlendRGB = UnityEngine.Rendering.BlendMode.Zero;
                            srcBlendA = UnityEngine.Rendering.BlendMode.Zero;
                            dstBlendA = UnityEngine.Rendering.BlendMode.One;

                            material.EnableKeyword(ShaderKeywordStrings._ALPHAMODULATE_ON);
                            break;
                    }

                    // Lift alpha multiply from ROP to shader by setting pre-multiplied _SrcBlend mode.
                    // The intent is to do different blending for diffuse and specular in shader.
                    // ref: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
                    bool preserveSpecular = (material.HasProperty(Property.BlendModePreserveSpecular) &&
                                             material.GetFloat(Property.BlendModePreserveSpecular) > 0) &&
                                            blendMode != BlendMode.Multiply && blendMode != BlendMode.Premultiply;
                    if (preserveSpecular)
                    {
                        srcBlendRGB = UnityEngine.Rendering.BlendMode.One;
                        material.EnableKeyword(ShaderKeywordStrings._ALPHAPREMULTIPLY_ON);
                    }

                    // When doing off-screen transparency accumulation, we change blend factors as described here: https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
                    bool offScreenAccumulateAlpha = false;
                    if (offScreenAccumulateAlpha)
                        srcBlendA = UnityEngine.Rendering.BlendMode.Zero;

                    SetMaterialSrcDstBlendProperties(material, srcBlendRGB, dstBlendRGB, // RGB
                        srcBlendA, dstBlendA); // Alpha

                    // General Transparent Material Settings
                    material.SetOverrideTag("RenderType", "Transparent");
                    zwrite = false;
                    material.EnableKeyword(ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT);
                    renderQueue = (int)RenderQueue.Transparent;
                }

                // check for override enum
                if (material.HasProperty(Property.ZWriteControl))
                {
                    var zwriteControl = (UnityEditor.Rendering.Universal.ShaderGraph.ZWriteControl)material.GetFloat(Property.ZWriteControl);
                    if (zwriteControl == UnityEditor.Rendering.Universal.ShaderGraph.ZWriteControl.ForceEnabled)
                        zwrite = true;
                    else if (zwriteControl == UnityEditor.Rendering.Universal.ShaderGraph.ZWriteControl.ForceDisabled)
                        zwrite = false;
                }
                SetMaterialZWriteProperty(material, zwrite);
                material.SetShaderPassEnabled("DepthOnly", zwrite);
            }
            else
            {
                // no surface type property -- must be hard-coded by the shadergraph,
                // so ensure the pass is enabled at the material level
                material.SetShaderPassEnabled("DepthOnly", true);
            }

            // must always apply queue offset, even if not set to material control
            if (material.HasProperty(Property.QueueOffset))
                renderQueue += (int)material.GetFloat(Property.QueueOffset);

            automaticRenderQueue = renderQueue;
        }

        public static void SetupMaterialBlendMode(Material material)
        {
            SetupMaterialBlendModeInternal(material, out int renderQueue);

            // apply automatic render queue
            if (renderQueue != material.renderQueue)
                material.renderQueue = renderQueue;
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // Clear all keywords for fresh start
            // Note: this will nuke user-selected custom keywords when they change shaders
            material.shaderKeywords = null;

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // Setup keywords based on the new shader
            UpdateMaterial(material, MaterialUpdateType.ChangedAssignedShader);
        }

        #endregion
        ////////////////////////////////////
        // Helper Functions               //
        ////////////////////////////////////
        #region HelperFunctions

        public static void TwoFloatSingleLine(GUIContent title, MaterialProperty prop1, GUIContent prop1Label,
            MaterialProperty prop2, GUIContent prop2Label, MaterialEditor materialEditor, float labelWidth = 30f)
        {
            const int kInterFieldPadding = 2;

            MaterialEditor.BeginProperty(prop1);
            MaterialEditor.BeginProperty(prop2);

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.PrefixLabel(rect, title);

            var indent = EditorGUI.indentLevel;
            var preLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = labelWidth;

            Rect propRect1 = new Rect(rect.x + preLabelWidth, rect.y,
                (rect.width - preLabelWidth) * 0.5f - 1, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop1.hasMixedValue;
            var prop1val = EditorGUI.FloatField(propRect1, prop1Label, prop1.floatValue);
            if (EditorGUI.EndChangeCheck())
                prop1.floatValue = prop1val;

            Rect propRect2 = new Rect(propRect1.x + propRect1.width + kInterFieldPadding, rect.y,
                propRect1.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop2.hasMixedValue;
            var prop2val = EditorGUI.FloatField(propRect2, prop2Label, prop2.floatValue);
            if (EditorGUI.EndChangeCheck())
                prop2.floatValue = prop2val;

            EditorGUI.indentLevel = indent;
            EditorGUIUtility.labelWidth = preLabelWidth;

            EditorGUI.showMixedValue = false;

            MaterialEditor.EndProperty();
            MaterialEditor.EndProperty();
        }

        public void DoPopup(GUIContent label, MaterialProperty property, string[] options)
        {
            if (property != null)
                materialEditor.PopupShaderProperty(property, label, options);
        }

        // Helper to show texture and color properties
        public static Rect TextureColorProps(MaterialEditor materialEditor, GUIContent label, MaterialProperty textureProp, MaterialProperty colorProp, bool hdr = false)
        {
            MaterialEditor.BeginProperty(textureProp);
            if (colorProp != null)
                MaterialEditor.BeginProperty(colorProp);

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = textureProp.hasMixedValue;
            materialEditor.TexturePropertyMiniThumbnail(rect, textureProp, label.text, label.tooltip);
            EditorGUI.showMixedValue = false;

            if (colorProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = colorProp.hasMixedValue;
                int indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                Rect rectAfterLabel = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                    EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight);
                var col = EditorGUI.ColorField(rectAfterLabel, GUIContent.none, colorProp.colorValue, true,
                    false, hdr);
                EditorGUI.indentLevel = indentLevel;
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(colorProp.displayName);
                    colorProp.colorValue = col;
                }
                EditorGUI.showMixedValue = false;
            }

            if (colorProp != null)
                MaterialEditor.EndProperty();
            MaterialEditor.EndProperty();

            return rect;
        }

        // Copied from shaderGUI as it is a protected function in an abstract class, unavailable to others
        public new static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties)
        {
            return FindProperty(propertyName, properties, true);
        }

        // Copied from shaderGUI as it is a protected function in an abstract class, unavailable to others
        public new static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory)
        {
            for (int index = 0; index < properties.Length; ++index)
            {
                if (properties[index] != null && properties[index].name == propertyName)
                    return properties[index];
            }
            if (propertyIsMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + (object)properties.Length);
            return null;
        }

        #endregion
    }
}

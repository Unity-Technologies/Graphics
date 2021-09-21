using System;
using UnityEngine;
using UnityEngine.Rendering;
using RenderQueue = UnityEngine.Rendering.RenderQueue;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    public class BuiltInBaseShaderGUI : ShaderGUI
    {
        [Flags]
        protected enum Expandable
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Advanced = 1 << 2
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
            public static readonly string[] zwriteNames = Enum.GetNames(typeof(UnityEditor.Rendering.BuiltIn.ShaderGraph.ZWriteControl));
            // need to skip the first entry for ztest (ZTestMode.Disabled is not a valid value)
            public static readonly int[] ztestValues = (int[])Enum.GetValues(typeof(UnityEditor.Rendering.BuiltIn.ShaderGraph.ZTestMode));
            public static readonly string[] ztestNames = Enum.GetNames(typeof(UnityEditor.Rendering.BuiltIn.ShaderGraph.ZTestMode));
            public static readonly string[] queueControlNames = Enum.GetNames(typeof(QueueControl));

            // Categories
            public static readonly GUIContent SurfaceOptions =
                EditorGUIUtility.TrTextContent("Surface Options", "Controls how Built-In RP renders the Material on a screen.");

            public static readonly GUIContent SurfaceInputs = EditorGUIUtility.TrTextContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            public static readonly GUIContent AdvancedLabel = EditorGUIUtility.TrTextContent("Advanced Options",
                "These settings affect behind-the-scenes rendering and underlying calculations.");

            public static readonly GUIContent surfaceType = EditorGUIUtility.TrTextContent("Surface Type",
                "Select a surface type for your texture. Choose between Opaque or Transparent.");
            public static readonly GUIContent blendingMode = EditorGUIUtility.TrTextContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");
            public static readonly GUIContent cullingText = EditorGUIUtility.TrTextContent("Render Face",
                "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");
            public static readonly GUIContent zwriteText = EditorGUIUtility.TrTextContent("Depth Write",
                "Controls whether the shader writes depth.  Auto will write only when the shader is opaque.");
            public static readonly GUIContent ztestText = EditorGUIUtility.TrTextContent("Depth Test",
                "Specifies the depth test mode.  The default is LEqual.");
            public static readonly GUIContent alphaClipText = EditorGUIUtility.TrTextContent("Alpha Clipping",
                "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas.");

            public static readonly GUIContent queueSlider = EditorGUIUtility.TrTextContent("Sorting Priority",
                "Determines the chronological rendering order for a Material. Materials with lower value are rendered first.");
            public static readonly GUIContent queueControl = EditorGUIUtility.TrTextContent("Queue Control",
                "Controls whether render queue is automatically set based on material surface type, or explicitly set by the user.");
        }

        public bool m_FirstTimeApply = true;

        // By default, everything is expanded, except advanced
        readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);

        // These have to be stored due to how MaterialHeaderScopeList callbacks work (they don't provide this data in the callbacks)
        MaterialEditor m_MaterialEditor;
        MaterialProperty[] m_Properties;

        private const int queueOffsetRange = 50;

        override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            m_MaterialEditor = materialEditor;
            m_Properties = properties;

            Material targetMat = materialEditor.target as Material;

            if (m_FirstTimeApply)
            {
                OnOpenGUI(targetMat, materialEditor, properties);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(materialEditor, targetMat, properties);
        }

        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Generate the foldouts
            m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceOptions, (uint)Expandable.SurfaceOptions, DrawSurfaceOptions);
            m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceInputs, (uint)Expandable.SurfaceInputs, DrawSurfaceInputs);
            m_MaterialScopeList.RegisterHeaderScope(Styles.AdvancedLabel, (uint)Expandable.Advanced, DrawAdvancedOptions);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // Clear all keywords for fresh start
            // Note: this will nuke user-selected custom keywords when they change shaders
            material.shaderKeywords = null;

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // Setup keywords based on the new shader
            UnityEditor.Rendering.BuiltIn.ShaderUtils.ResetMaterialKeywords(material);
        }

        void ShaderPropertiesGUI(MaterialEditor materialEditor, Material material, MaterialProperty[] properties)
        {
            m_MaterialScopeList.DrawHeaders(materialEditor, material);
        }

        protected virtual void DrawSurfaceOptions(Material material)
        {
            var materialEditor = m_MaterialEditor;
            var properties = m_Properties;

            var surfaceTypeProp = FindProperty(Property.Surface(), properties, false);
            if (surfaceTypeProp != null)
            {
                DoPopup(Styles.surfaceType, materialEditor, surfaceTypeProp, Styles.surfaceTypeNames);
                var surfaceType = (SurfaceType)surfaceTypeProp.floatValue;
                if (surfaceType == SurfaceType.Transparent)
                {
                    var blendModeProp = FindProperty(Property.Blend(), properties, false);
                    DoPopup(Styles.blendingMode, materialEditor, blendModeProp, Styles.blendModeNames);
                }
            }
            var cullingProp = FindProperty(Property.Cull(), properties, false);
            DoPopup(Styles.cullingText, materialEditor, cullingProp, Enum.GetNames(typeof(RenderFace)));

            var zWriteProp = FindProperty(Property.ZWriteControl(), properties, false);
            DoPopup(Styles.zwriteText, materialEditor, zWriteProp, Styles.zwriteNames);

            var ztestProp = FindProperty(Property.ZTest(), properties, false);
            DoIntPopup(Styles.ztestText, materialEditor, ztestProp, Styles.ztestNames, Styles.ztestValues);

            var alphaClipProp = FindProperty(Property.AlphaClip(), properties, false);
            DrawFloatToggleProperty(Styles.alphaClipText, alphaClipProp);
        }

        protected virtual void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(m_MaterialEditor, material, m_Properties);
        }

        protected virtual void DrawAdvancedOptions(Material material)
        {
            // Only draw sorting priority if queue control is set to "auto", otherwise draw render queue
            // GetAutomaticQueueControlSetting will guarantee we have a sane queue control value
            bool autoQueueControl = GetAutomaticQueueControlSetting(material);
            var queueControlProp = FindProperty(Property.QueueControl(), m_Properties, false);
            DoPopup(Styles.queueControl, m_MaterialEditor, queueControlProp, Styles.queueControlNames);
            if (autoQueueControl)
                DrawQueueOffsetField(m_MaterialEditor, material, m_Properties);
            else
                m_MaterialEditor.RenderQueueField();
        }

        protected void DrawQueueOffsetField(MaterialEditor materialEditor, Material material, MaterialProperty[] properties)
        {
            var queueOffsetProp = FindProperty(Property.QueueOffset(), properties, false);
            if (queueOffsetProp != null)
                materialEditor.IntSliderShaderProperty(queueOffsetProp, -queueOffsetRange, queueOffsetRange, Styles.queueSlider);
        }

        static void DrawShaderGraphProperties(MaterialEditor materialEditor, Material material, MaterialProperty[] properties)
        {
            if (properties == null)
                return;

            ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, properties);
        }

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
            int rawRenderQueue = MaterialAccess.ReadMaterialRawRenderQueue(material);
            if (rawRenderQueue == -1)
            {
                material.SetFloat(Property.QueueControl(), (float)QueueControl.Auto); // Automatic behavior - surface type override
            }
            else
            {
                material.SetFloat(Property.QueueControl(), (float)QueueControl.UserOverride); // User has selected explicit render queue
            }
        }

        internal static bool GetAutomaticQueueControlSetting(Material material)
        {
            // If the material doesn't yet have the queue control property,
            // we should not engage automatic behavior until the shader gets reimported.
            bool automaticQueueControl = false;
            if (material.HasProperty(Property.QueueControl()))
            {
                var queueControl = material.GetFloat(Property.QueueControl());
                if (queueControl < 0.0f)
                {
                    // The property was added with a negative value, indicating it needs to be validated for this material
                    UpdateMaterialRenderQueueControl(material);
                }
                automaticQueueControl = (material.GetFloat(Property.QueueControl()) == (float)QueueControl.Auto);
            }
            return automaticQueueControl;
        }

        public override void ValidateMaterial(Material material) => SetupSurface(material);

        public static void SetupSurface(Material material)
        {
            bool alphaClipping = false;
            var alphaClipProp = Property.AlphaClip();
            if (material.HasProperty(alphaClipProp))
                alphaClipping = material.GetFloat(alphaClipProp) >= 0.5;

            CoreUtils.SetKeyword(material, Keyword.SG_AlphaTestOn, alphaClipping);
            CoreUtils.SetKeyword(material, Keyword.SG_AlphaClip, alphaClipping);

            int renderQueue = material.shader.renderQueue;

            var surfaceTypeProp = Property.Surface();
            if (material.HasProperty(surfaceTypeProp))
            {
                bool zwrite = false;
                var surfaceType = (SurfaceType)material.GetFloat(surfaceTypeProp);
                if (surfaceType == SurfaceType.Opaque)
                {
                    string renderType;
                    if (alphaClipping)
                    {
                        renderQueue = (int)RenderQueue.AlphaTest;
                        renderType = "TransparentCutout";
                    }
                    else
                    {
                        renderQueue = (int)RenderQueue.Geometry;
                        renderType = "Opaque";
                    }

                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetOverrideTag("RenderType", renderType);
                    SetBlendMode(material, UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero);
                    material.DisableKeyword(Keyword.SG_AlphaPremultiplyOn);
                    zwrite = true;
                }
                else
                {
                    var blendProp = Property.Blend();
                    if (material.HasProperty(blendProp))
                    {
                        var blendMode = (BlendMode)material.GetFloat(blendProp);
                        if (blendMode == BlendMode.Alpha)
                            SetBlendMode(material, UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        else if (blendMode == BlendMode.Premultiply)
                            SetBlendMode(material, UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        else if (blendMode == BlendMode.Additive)
                            SetBlendMode(material, UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One);
                        else if (blendMode == BlendMode.Multiply)
                            SetBlendMode(material, UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.Zero);
                        CoreUtils.SetKeyword(material, Keyword.SG_AlphaPremultiplyOn, blendMode == BlendMode.Premultiply);
                    }

                    renderQueue = (int)RenderQueue.Transparent;
                    material.SetOverrideTag("RenderType", "Transparent");
                }
                CoreUtils.SetKeyword(material, Keyword.SG_SurfaceTypeTransparent, surfaceType == SurfaceType.Transparent);

                // check for override enum
                var zwriteProp = Property.ZWriteControl();
                if (material.HasProperty(zwriteProp))
                {
                    var zwriteControl = (UnityEditor.Rendering.BuiltIn.ShaderGraph.ZWriteControl)material.GetFloat(zwriteProp);
                    if (zwriteControl == UnityEditor.Rendering.BuiltIn.ShaderGraph.ZWriteControl.ForceEnabled)
                        zwrite = true;
                    else if (zwriteControl == UnityEditor.Rendering.BuiltIn.ShaderGraph.ZWriteControl.ForceDisabled)
                        zwrite = false;
                }
                SetMaterialZWriteProperty(material, zwrite);
            }

            // must always apply queue offset, even if not set to material control
            if (material.HasProperty(Property.QueueOffset()))
                renderQueue += (int)material.GetFloat(Property.QueueOffset());

            // apply automatic render queue
            bool automaticRenderQueue = GetAutomaticQueueControlSetting(material);
            if (automaticRenderQueue && (renderQueue != material.renderQueue))
                material.renderQueue = renderQueue;
        }

        static void SetMaterialZWriteProperty(Material material, bool state)
        {
            var zWriteProp = Property.ZWrite();
            if (material.HasProperty(zWriteProp))
            {
                material.SetFloat(zWriteProp, state == true ? 1.0f : 0.0f);
            }
        }

        static void SetBlendMode(Material material, UnityEngine.Rendering.BlendMode srcBlendMode, UnityEngine.Rendering.BlendMode dstBlendMode)
        {
            var srcBlendProp = Property.SrcBlend();
            if (material.HasProperty(srcBlendProp))
                material.SetFloat(srcBlendProp, (int)srcBlendMode);
            var dstBlendProp = Property.DstBlend();
            if (material.HasProperty(dstBlendProp))
                material.SetFloat(dstBlendProp, (int)dstBlendMode);
        }

        public static void DoPopup(GUIContent label, MaterialEditor materialEditor, MaterialProperty property, string[] options)
        {
            if (property == null)
                return;

            materialEditor.PopupShaderProperty(property, label, options);
        }

        public static void DoIntPopup(GUIContent label, MaterialEditor materialEditor, MaterialProperty property, string[] options, int[] optionValues)
        {
            if (property == null)
                return;

            materialEditor.IntPopupShaderProperty(property, label.text, options, optionValues);
        }

        public static void DrawFloatToggleProperty(GUIContent styles, MaterialProperty prop)
        {
            if (prop == null)
                return;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            bool newValue = EditorGUILayout.Toggle(styles, prop.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue ? 1.0f : 0.0f;
            EditorGUI.showMixedValue = false;
        }
    }
}

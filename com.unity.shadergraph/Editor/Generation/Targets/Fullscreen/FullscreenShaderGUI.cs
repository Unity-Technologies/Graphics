using System;
using UnityEngine;
using UnityEngine.Rendering;
using RenderQueue = UnityEngine.Rendering.RenderQueue;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    public class FullscreenShaderGUI : ShaderGUI
    {
        [Flags]
        protected enum Expandable
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
        }

        protected class Styles
        {
            // Categories
            public static readonly GUIContent SurfaceOptions =
                EditorGUIUtility.TrTextContent("Surface Options", "Controls the rendering states of the fullscreen material.");
            public static readonly GUIContent SurfaceInputs = EditorGUIUtility.TrTextContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            public static readonly GUIContent blendingMode = EditorGUIUtility.TrTextContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");
            public static readonly GUIContent srcColorBlendMode = EditorGUIUtility.TrTextContent("Src Color",
                "TODO");
            public static readonly GUIContent dstColorBlendMode = EditorGUIUtility.TrTextContent("Dst Color",
                "TODO");
            public static readonly GUIContent colorBlendOperation = EditorGUIUtility.TrTextContent("Color Blend Op",
                "TODO");
            public static readonly GUIContent srcAlphaBlendMode = EditorGUIUtility.TrTextContent("Src Alpha",
                "TODO");
            public static readonly GUIContent dstAlphaBlendMode = EditorGUIUtility.TrTextContent("Dst Alpha",
                "TODO");
            public static readonly GUIContent alphaBlendOperation = EditorGUIUtility.TrTextContent("Alpha Blend Op",
                "TODO");
            public static readonly GUIContent depthWrite = EditorGUIUtility.TrTextContent("Depth Write",
                "Controls whether the shader writes depth.");
            public static readonly GUIContent depthTest = EditorGUIUtility.TrTextContent("Depth Test",
                "Specifies the depth test mode. The default is Always.");

            public static readonly GUIContent stencil = EditorGUIUtility.TrTextContent("Stencil Override", "TODO");
            public static readonly GUIContent stencilRef = EditorGUIUtility.TrTextContent("Reference", "TODO");
            public static readonly GUIContent stencilReadMask = EditorGUIUtility.TrTextContent("Read Mask", "TODO");
            public static readonly GUIContent stencilWriteMask = EditorGUIUtility.TrTextContent("Write Mask", "TODO");
            public static readonly GUIContent stencilComparison = EditorGUIUtility.TrTextContent("Comparison", "TODO");
            public static readonly GUIContent stencilPass = EditorGUIUtility.TrTextContent("Pass", "TODO");
            public static readonly GUIContent stencilFail = EditorGUIUtility.TrTextContent("Fail", "TODO");
            public static readonly GUIContent stencilDepthFail = EditorGUIUtility.TrTextContent("Depth Fail", "TODO");

        }

        public bool m_FirstTimeApply = true;

        // By default, everything is expanded
        readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue);

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

            var blendMode = FindProperty(FullscreenTarget.Uniforms.blendModeProperty, properties, false);
            var srcColorBlend = FindProperty(FullscreenTarget.Uniforms.srcColorBlendProperty, properties, false);
            var dstColorBlend = FindProperty(FullscreenTarget.Uniforms.dstColorBlendProperty, properties, false);
            var srcAlphaBlend = FindProperty(FullscreenTarget.Uniforms.srcAlphaBlendProperty, properties, false);
            var dstAlphaBlend = FindProperty(FullscreenTarget.Uniforms.dstAlphaBlendProperty, properties, false);
            var colorBlendOp = FindProperty(FullscreenTarget.Uniforms.colorBlendOperationProperty, properties, false);
            var alphaBlendOp = FindProperty(FullscreenTarget.Uniforms.alphaBlendOperationProperty, properties, false);
            var depthWrite = FindProperty(FullscreenTarget.Uniforms.depthWriteProperty, properties, false);
            var depthTest = FindProperty(FullscreenTarget.Uniforms.depthTestProperty, properties, false);
            var stencilEnable = FindProperty(FullscreenTarget.Uniforms.stencilEnableProperty, properties, false);
            var stencilRef = FindProperty(FullscreenTarget.Uniforms.stencilReferenceProperty, properties, false);
            var stencilReadMask = FindProperty(FullscreenTarget.Uniforms.stencilReadMaskProperty, properties, false);
            var stencilWriteMask = FindProperty(FullscreenTarget.Uniforms.stencilWriteMaskProperty, properties, false);
            var stencilComp = FindProperty(FullscreenTarget.Uniforms.stencilComparisonProperty, properties, false);
            var stencilPass = FindProperty(FullscreenTarget.Uniforms.stencilPassProperty, properties, false);
            var stencilFail = FindProperty(FullscreenTarget.Uniforms.stencilFailProperty, properties, false);
            var stencilDepthFail = FindProperty(FullscreenTarget.Uniforms.stencilDepthFailProperty, properties, false);

            if (material.HasProperty(FullscreenTarget.Uniforms.blendModeProperty))
            {
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.ShaderProperty(blendMode, Styles.blendingMode);
                FullscreenBlendMode blendModeValue = (FullscreenBlendMode)blendMode.floatValue;
                if (EditorGUI.EndChangeCheck())
                    SetBlendMode(blendModeValue);

                if (blendModeValue == FullscreenBlendMode.Custom)
                {
                    m_MaterialEditor.ShaderProperty(srcColorBlend, Styles.srcColorBlendMode, 1);
                    m_MaterialEditor.ShaderProperty(dstColorBlend, Styles.dstColorBlendMode, 1);
                    m_MaterialEditor.ShaderProperty(colorBlendOp, Styles.colorBlendOperation, 1);
                    m_MaterialEditor.ShaderProperty(srcAlphaBlend, Styles.srcAlphaBlendMode, 1);
                    m_MaterialEditor.ShaderProperty(dstAlphaBlend, Styles.dstAlphaBlendMode, 1);
                    m_MaterialEditor.ShaderProperty(alphaBlendOp, Styles.alphaBlendOperation, 1);
                }
            }

            if (material.HasProperty(FullscreenTarget.Uniforms.depthWriteProperty))
                m_MaterialEditor.ShaderProperty(depthWrite, Styles.depthWrite);
            if (material.HasProperty(FullscreenTarget.Uniforms.depthTestProperty))
                m_MaterialEditor.ShaderProperty(depthTest, Styles.depthTest);

            if (material.HasProperty(FullscreenTarget.Uniforms.stencilEnableProperty))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.ShaderProperty(stencilEnable, Styles.stencil);
                bool stencilEnableValue = stencilEnable.floatValue > 0.5f;
                if (EditorGUI.EndChangeCheck())
                    SetStencilEnable(stencilEnableValue);
                if (stencilEnableValue)
                {
                    m_MaterialEditor.ShaderProperty(stencilRef, Styles.stencilRef, 1);
                    m_MaterialEditor.ShaderProperty(stencilReadMask, Styles.stencilReadMask, 1);
                    m_MaterialEditor.ShaderProperty(stencilWriteMask, Styles.stencilWriteMask, 1);
                    m_MaterialEditor.ShaderProperty(stencilComp, Styles.stencilComparison, 1);
                    m_MaterialEditor.ShaderProperty(stencilPass, Styles.stencilPass, 1);
                    m_MaterialEditor.ShaderProperty(stencilFail, Styles.stencilFail, 1);
                    m_MaterialEditor.ShaderProperty(stencilDepthFail, Styles.stencilDepthFail, 1);
                }
            }

            void SetStencilEnable(bool enabled)
            {
                if (!enabled)
                {
                    stencilComp.floatValue = (float)CompareFunction.Always;
                    stencilPass.floatValue = (float)StencilOp.Keep;
                }
            }

            void SetBlendMode(FullscreenBlendMode blendMode)
            {
                // Note that we can't disable the blend mode from here
                if (blendMode == FullscreenBlendMode.Alpha || blendMode == FullscreenBlendMode.Disabled)
                {
                    srcColorBlend.floatValue = (float)BlendMode.SrcAlpha;
                    dstColorBlend.floatValue = (float)BlendMode.OneMinusSrcAlpha;
                    srcAlphaBlend.floatValue = (float)BlendMode.One;
                    dstAlphaBlend.floatValue = (float)BlendMode.OneMinusSrcAlpha;
                }
                else if (blendMode == FullscreenBlendMode.Premultiply)
                {
                    srcColorBlend.floatValue = (float)BlendMode.One;
                    dstColorBlend.floatValue = (float)BlendMode.OneMinusSrcAlpha;
                    srcAlphaBlend.floatValue = (float)BlendMode.One;
                    dstAlphaBlend.floatValue = (float)BlendMode.OneMinusSrcAlpha;
                }
                else if (blendMode == FullscreenBlendMode.Additive)
                {
                    srcColorBlend.floatValue = (float)BlendMode.SrcAlpha;
                    dstColorBlend.floatValue = (float)BlendMode.One;
                    srcAlphaBlend.floatValue = (float)BlendMode.One;
                    dstAlphaBlend.floatValue = (float)BlendMode.One;
                }
                else if (blendMode == FullscreenBlendMode.Multiply)
                {
                    srcColorBlend.floatValue = (float)BlendMode.DstColor;
                    dstColorBlend.floatValue = (float)BlendMode.Zero;
                    srcAlphaBlend.floatValue = (float)BlendMode.One;
                    dstAlphaBlend.floatValue = (float)BlendMode.OneMinusSrcAlpha;
                }

                colorBlendOp.floatValue = (float)BlendOp.Add;
                alphaBlendOp.floatValue = (float)BlendOp.Add;
            }
        }

        protected virtual void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(m_MaterialEditor, material, m_Properties);
        }

        static void DrawShaderGraphProperties(MaterialEditor materialEditor, Material material, MaterialProperty[] properties)
        {
            if (properties == null)
                return;

            ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, properties);
        }

        public override void ValidateMaterial(Material material) => SetupSurface(material);

        public static void SetupSurface(Material material)
        {
            // For now there is no keyword in FullScreenShader.
        }
    }
}

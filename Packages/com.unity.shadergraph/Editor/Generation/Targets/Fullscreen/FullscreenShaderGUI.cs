using System;
using UnityEngine;
using UnityEngine.Rendering;
using RenderQueue = UnityEngine.Rendering.RenderQueue;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    /// <summary>
    /// The base class to implement the fullscreen Material GUI in a render pipeline.
    /// </summary>
    public class FullscreenShaderGUI : ShaderGUI
    {
        /// <summary>Enum used to store the expanded state of the drawer in the material GUI</summary>
        [Flags]
        protected enum Expandable
        {
            /// <summary>Surface Option key for the MaterialHeaderScopeList.RegisterHeaderScope call</summary>
            SurfaceOptions = 1 << 0,
            /// <summary>Surface Inputs key for the MaterialHeaderScopeList.RegisterHeaderScope call</summary>
            SurfaceInputs = 1 << 1,
        }

        protected class Styles
        {
            // Categories
            /// <summary>Surface Option header name</summary>
            public static readonly GUIContent SurfaceOptions =
                EditorGUIUtility.TrTextContent("Surface Options", "Controls the rendering states of the fullscreen material.");
            /// <summary>Surface Inputs header name</summary>
            public static readonly GUIContent SurfaceInputs = EditorGUIUtility.TrTextContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            /// <summary>Name and tooltip for the blending mode property in the material GUI</summary>
            public static readonly GUIContent blendingMode = EditorGUIUtility.TrTextContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");
            /// <summary>Name and tooltip for the source color blend mode property in the material GUI</summary>
            public static readonly GUIContent srcColorBlendMode = EditorGUIUtility.TrTextContent("Src Color",
                "Describes how the input color will be blended.");
            /// <summary>Name and tooltip for the destination color blend mode property in the material GUI</summary>
            public static readonly GUIContent dstColorBlendMode = EditorGUIUtility.TrTextContent("Dst Color",
                "Describes how the destination color will be blended.");
            /// <summary>Name and tooltip for the color blend operation property in the material GUI</summary>
            public static readonly GUIContent colorBlendOperation = EditorGUIUtility.TrTextContent("Color Blend Op",
                "Tell which operation to use when blending the colors. Default is Add.");
            /// <summary>Name and tooltip for the source alpha blend mode property in the material GUI</summary>
            public static readonly GUIContent srcAlphaBlendMode = EditorGUIUtility.TrTextContent("Src Alpha",
                "Describes how the input alpha will be blended.");
            /// <summary>Name and tooltip for the destination alpha blend mode property in the material GUI</summary>
            public static readonly GUIContent dstAlphaBlendMode = EditorGUIUtility.TrTextContent("Dst Alpha",
                "Describes how the input alpha will be blended.");
            /// <summary>Name and tooltip for the alpha blend operation property in the material GUI</summary>
            public static readonly GUIContent alphaBlendOperation = EditorGUIUtility.TrTextContent("Alpha Blend Op",
                "Tell which operation to use when blending the alpha channel. Default is Add.");
            /// <summary>Name and tooltip for the depth write property in the material GUI</summary>
            public static readonly GUIContent depthWrite = EditorGUIUtility.TrTextContent("Depth Write",
                "Controls whether the shader writes depth.");
            /// <summary>Name and tooltip for the depth test property in the material GUI</summary>
            public static readonly GUIContent depthTest = EditorGUIUtility.TrTextContent("Depth Test",
                "Specifies the depth test mode. The default is Always.");

            /// <summary>Name and tooltip for the stencil override property in the material GUI</summary>
            public static readonly GUIContent stencil = EditorGUIUtility.TrTextContent("Stencil Override", "Enable the stencil block in the shader.");
            /// <summary>Name and tooltip for the stencil reference property in the material GUI</summary>
            public static readonly GUIContent stencilRef = EditorGUIUtility.TrTextContent("Reference", "Reference value use for comparison and operations.");
            /// <summary>Name and tooltip for the stencil read mask property in the material GUI</summary>
            public static readonly GUIContent stencilReadMask = EditorGUIUtility.TrTextContent("Read Mask", "Tells which bit are allowed to be read during the stencil test.");
            /// <summary>Name and tooltip for the stencil write mask property in the material GUI</summary>
            public static readonly GUIContent stencilWriteMask = EditorGUIUtility.TrTextContent("Write Mask", "Tells which bit are allowed to be written during the stencil test.");
            /// <summary>Name and tooltip for the stencil comparison property in the material GUI</summary>
            public static readonly GUIContent stencilComparison = EditorGUIUtility.TrTextContent("Comparison", "Tells which function to use when doing the stencil test.");
            /// <summary>Name and tooltip for the stencil pass operation property in the material GUI</summary>
            public static readonly GUIContent stencilPass = EditorGUIUtility.TrTextContent("Pass", "Tells what to do when the stencil test succeed.");
            /// <summary>Name and tooltip for the stencil fail operation property in the material GUI</summary>
            public static readonly GUIContent stencilFail = EditorGUIUtility.TrTextContent("Fail", "Tells what to do when the stencil test fails.");
            /// <summary>Name and tooltip for the stencil depth fail operation property in the material GUI</summary>
            public static readonly GUIContent stencilDepthFail = EditorGUIUtility.TrTextContent("Depth Fail", "Tells what to do when the depth test fails.");
        }

        bool m_FirstTimeApply = true;

        // By default, everything is expanded
        readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue);

        // These have to be stored due to how MaterialHeaderScopeList callbacks work (they don't provide this data in the callbacks)
        MaterialEditor m_MaterialEditor;
        MaterialProperty[] m_Properties;

        private const int queueOffsetRange = 50;

        /// <summary>
        /// Unity calls this function when it displays the GUI. To implement your custom GUI, override this function..
        /// </summary>
        /// <param name="materialEditor">Material editor instance.</param>
        /// <param name="properties">The list of properties in the inspected material(s).</param>
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

        /// <summary>
        /// Unity calls this function when it displays the GUI. To implement your custom GUI, override this function..
        /// </summary>
        /// <param name="materialEditor">Material editor instance.</param>
        /// <param name="properties">The list of properties in the inspected material(s).</param>
        /// <param name="material">The target materials for this GUI.</param>
        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Generate the foldouts
            m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceOptions, (uint)Expandable.SurfaceOptions, DrawSurfaceOptions);
            m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceInputs, (uint)Expandable.SurfaceInputs, DrawSurfaceInputs);
        }

        /// <summary>
        /// Assign a new FullscreenShader to the target material.
        /// </summary>
        /// <param name="material">A valid material using a Fullscreen Shader Graph.</param>
        /// <param name="oldShader"></param>
        /// <param name="newShader"></param>
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // Clear all keywords for fresh start
            // Note: this will nuke user-selected custom keywords when they change shaders
            material.shaderKeywords = null;

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // Setup keywords based on the new shader
            ValidateMaterial(material);
        }

        void ShaderPropertiesGUI(MaterialEditor materialEditor, Material material, MaterialProperty[] properties)
        {
            m_MaterialScopeList.DrawHeaders(materialEditor, material);
        }

        /// <summary>
        /// Draw the Surface Options section of the fullscreen shader GUI.
        /// </summary>
        /// <param name="material">A valid material using a Fullscreen Shader Graph.</param>
        protected virtual void DrawSurfaceOptions(Material material)
        {
            var materialEditor = m_MaterialEditor;
            var properties = m_Properties;

            var blendMode = FindProperty(FullscreenUniforms.blendModeProperty, properties, false);
            var srcColorBlend = FindProperty(FullscreenUniforms.srcColorBlendProperty, properties, false);
            var dstColorBlend = FindProperty(FullscreenUniforms.dstColorBlendProperty, properties, false);
            var srcAlphaBlend = FindProperty(FullscreenUniforms.srcAlphaBlendProperty, properties, false);
            var dstAlphaBlend = FindProperty(FullscreenUniforms.dstAlphaBlendProperty, properties, false);
            var colorBlendOp = FindProperty(FullscreenUniforms.colorBlendOperationProperty, properties, false);
            var alphaBlendOp = FindProperty(FullscreenUniforms.alphaBlendOperationProperty, properties, false);
            var depthWrite = FindProperty(FullscreenUniforms.depthWriteProperty, properties, false);
            var depthTest = FindProperty(FullscreenUniforms.depthTestProperty, properties, false);
            var stencilEnable = FindProperty(FullscreenUniforms.stencilEnableProperty, properties, false);
            var stencilRef = FindProperty(FullscreenUniforms.stencilReferenceProperty, properties, false);
            var stencilReadMask = FindProperty(FullscreenUniforms.stencilReadMaskProperty, properties, false);
            var stencilWriteMask = FindProperty(FullscreenUniforms.stencilWriteMaskProperty, properties, false);
            var stencilComp = FindProperty(FullscreenUniforms.stencilComparisonProperty, properties, false);
            var stencilPass = FindProperty(FullscreenUniforms.stencilPassProperty, properties, false);
            var stencilFail = FindProperty(FullscreenUniforms.stencilFailProperty, properties, false);
            var stencilDepthFail = FindProperty(FullscreenUniforms.stencilDepthFailProperty, properties, false);

            if (material.HasProperty(FullscreenUniforms.blendModeProperty))
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

            if (material.HasProperty(FullscreenUniforms.depthWriteProperty))
                m_MaterialEditor.ShaderProperty(depthWrite, Styles.depthWrite);
            if (material.HasProperty(FullscreenUniforms.depthTestProperty))
                m_MaterialEditor.ShaderProperty(depthTest, Styles.depthTest);

            if (material.HasProperty(FullscreenUniforms.stencilEnableProperty))
            {
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
                    stencilFail.floatValue = (float)StencilOp.Keep;
                    stencilDepthFail.floatValue = (float)StencilOp.Keep;
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

        /// <summary>
        /// Draw the Surface Inputs section of the fullscreen shader GUI.
        /// </summary>
        /// <param name="material">A valid material using a Fullscreen Shader Graph.</param>
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

        /// <summary>
        /// Ensures that the material is correctly setup.
        /// </summary>
        /// <param name="material">A valid material using a Fullscreen Shader Graph.</param>
        public override void ValidateMaterial(Material material) => SetupSurface(material);

        /// <summary>
        /// Setup the fullscreen shader keywords from the material properties.
        /// </summary>
        /// <param name="material">A valid material using a Fullscreen Shader Graph.</param>
        public static void SetupSurface(Material material)
        {
            // For now there is no keyword in FullScreenShader.
        }
    }
}

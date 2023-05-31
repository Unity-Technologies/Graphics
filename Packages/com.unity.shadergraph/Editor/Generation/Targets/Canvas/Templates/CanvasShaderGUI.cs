using System;
using UnityEngine;
using UnityEngine.Rendering;
using RenderQueue = UnityEngine.Rendering.RenderQueue;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.Rendering.Canvas.ShaderGraph
{
    public class CanvasShaderGUI : ShaderGUI
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
                EditorGUIUtility.TrTextContent("Surface Options",
                    "Controls the rendering states of the fullscreen material.");

            public static readonly GUIContent SurfaceInputs = EditorGUIUtility.TrTextContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");
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

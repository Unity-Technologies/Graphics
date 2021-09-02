using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    // We don't reuse the other surface option ui block, AxF is too different
    class AxfMainSurfaceInputsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Main Mapping Configuration");

            public static GUIContent mappingModeText = new GUIContent("Mapping Mode");
            public static GUIContent planarSpaceText = new GUIContent("Planar Space");

            public static GUIContent materialTilingOffsetText = new GUIContent("Main Tiling (XY scales) and Offset (ZW)", "The XY scales the texture coordinates while the ZW are additive offsets");

            public static GUIContent rayTracingTexFilteringScaleText = new GUIContent("Texture Filtering In Raytracing", "Texture filtering works differently in raytracing. To help with aliasing you can adjust this from 0 (no filtering) to 1 (maximum filtering)");
        }
        static readonly string[] MappingModeNames = Enum.GetNames(typeof(AxFMappingMode));

        static string m_MappingModeText = "_MappingMode";
        MaterialProperty m_MappingMode = null;

        static string m_MappingMaskText = "_MappingMask";
        MaterialProperty m_MappingMask = null;

        static string m_PlanarSpaceText = "_PlanarSpace";
        MaterialProperty m_PlanarSpace = null;

        static string m_MaterialTilingOffsetText = "_Material_SO";
        MaterialProperty m_MaterialTilingOffset = null;

        static string m_RayTracingTexFilteringScaleText = "_RayTracingTexFilteringScale";
        MaterialProperty m_RayTracingTexFilteringScale = null;

        public AxfMainSurfaceInputsUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        public override void LoadMaterialProperties()
        {
            m_MappingMode = FindProperty(m_MappingModeText);
            m_MappingMask = FindProperty(m_MappingMaskText);
            m_PlanarSpace = FindProperty(m_PlanarSpaceText);

            m_MaterialTilingOffset = FindProperty(m_MaterialTilingOffsetText);
            m_RayTracingTexFilteringScale = FindProperty(m_RayTracingTexFilteringScaleText);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            materialEditor.PopupShaderProperty(m_MappingMode, Styles.mappingModeText, MappingModeNames);

            AxFMappingMode mappingMode = (AxFMappingMode)m_MappingMode.floatValue;
            m_MappingMask.vectorValue = AxFGUI.AxFMappingModeToMask(mappingMode);

            if (mappingMode >= AxFMappingMode.PlanarXY)
            {
                ++EditorGUI.indentLevel;
                materialEditor.ShaderProperty(m_PlanarSpace, Styles.planarSpaceText);
                --EditorGUI.indentLevel;
            }

            materialEditor.ShaderProperty(m_MaterialTilingOffset, Styles.materialTilingOffsetText);

            // We only display the ray tracing option if the asset supports it
            if ((RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported)
            {
                materialEditor.ShaderProperty(m_RayTracingTexFilteringScale, Styles.rayTracingTexFilteringScaleText);
            }
        }
    }
}

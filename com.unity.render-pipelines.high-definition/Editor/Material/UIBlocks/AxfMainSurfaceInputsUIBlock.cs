using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    // We don't reuse the other surface option ui block, AxF is too different
    class AxfMainSurfaceInputsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Main Mapping Configuration";

            public static GUIContent mappingModeText = new GUIContent("Mapping Mode");
            public static GUIContent planarSpaceText = new GUIContent("Planar Space");

            public static GUIContent materialTilingOffsetText = new GUIContent("Main Tiling (XY scales) and Offset (ZW)", "The XY scales the texture coordinates while the ZW are additive offsets");
        }
        static readonly string[]    MappingModeNames = Enum.GetNames(typeof(AxFMappingMode));

        static string m_MappingModeText = "_MappingMode";
        MaterialProperty m_MappingMode = null;

        static string m_MappingMaskText = "_MappingMask";
        MaterialProperty m_MappingMask = null;

        static string m_PlanarSpaceText = "_PlanarSpace";
        MaterialProperty m_PlanarSpace = null;

        static string m_MaterialTilingOffsetText = "_Material_SO";
        MaterialProperty m_MaterialTilingOffset = null;

        Expandable  m_ExpandableBit;

        public AxfMainSurfaceInputsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            m_MappingMode = FindProperty(m_MappingModeText);
            m_MappingMask = FindProperty(m_MappingMaskText);
            m_PlanarSpace = FindProperty(m_PlanarSpaceText);

            m_MaterialTilingOffset = FindProperty(m_MaterialTilingOffsetText);    
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawMainAxfSurfaceInputsGUI();
                }
            }
        }

        void DrawMainAxfSurfaceInputsGUI()
        {
            EditorGUI.BeginChangeCheck();
            float val = EditorGUILayout.Popup(Styles.mappingModeText, (int)m_MappingMode.floatValue, MappingModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                Material material = materialEditor.target as Material;
                Undo.RecordObject(material, "Change Mapping Mode");
                m_MappingMode.floatValue = val;
            }

            AxFMappingMode mappingMode = (AxFMappingMode)m_MappingMode.floatValue;
            m_MappingMask.vectorValue = AxFGUI.AxFMappingModeToMask(mappingMode);

            if (mappingMode >= AxFMappingMode.PlanarXY)
            {
                ++EditorGUI.indentLevel;
                materialEditor.ShaderProperty(m_PlanarSpace, Styles.planarSpaceText);
                --EditorGUI.indentLevel;
            }

            materialEditor.ShaderProperty(m_MaterialTilingOffset, Styles.materialTilingOffsetText);
        }
    }
}

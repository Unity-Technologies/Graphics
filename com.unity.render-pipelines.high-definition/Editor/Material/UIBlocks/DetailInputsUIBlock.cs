using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class DetailInputsUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None                = 0,
            EnableEmissionForGI = 1 << 0,
            SubHeader           = 1 << 1,
            All                 = ~0 ^ SubHeader // By default we don't want to have a sub-header
        }

        public class Styles
        {
            public const string header = "Detail Inputs";

            public static GUIContent UVDetailMappingText = new GUIContent("Detail UV Mapping", "");
            public static GUIContent detailMapNormalText = new GUIContent("Detail Map", "Specifies the Detail Map albedo (R) Normal map y-axis (G) Smoothness (B) Normal map x-axis (A) - Neutral value is (0.5, 0.5, 0.5, 0.5)");
            public static GUIContent detailAlbedoScaleText = new GUIContent("Detail Albedo Scale", "Controls the scale factor for the Detail Map's Albedo.");
            public static GUIContent detailNormalScaleText = new GUIContent("Detail Normal Scale", "Controls the scale factor for the Detail Map's Normal map.");
            public static GUIContent detailSmoothnessScaleText = new GUIContent("Detail Smoothness Scale", "Controls the scale factor for the Detail Map's Smoothness.");
            public static GUIContent linkDetailsWithBaseText = new GUIContent("Lock to Base Tiling/Offset", "When enabled, HDRP locks the Detail's Tiling/Offset values to the Base Tiling/Offset.");

            public static GUIContent perPixelDisplacementDetailsWarning = new GUIContent("For pixel displacement to work correctly, details and base map must use the same UV mapping.");
        }

        protected MaterialProperty[] UVDetail = new MaterialProperty[kMaxLayerCount];
        protected const string kUVDetail = "_UVDetail";
        protected MaterialProperty[] UVDetailsMappingMask = new MaterialProperty[kMaxLayerCount];
        protected const string kUVDetailsMappingMask = "_UVDetailsMappingMask";
        protected MaterialProperty[] detailMap = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailMap = "_DetailMap";
        protected MaterialProperty[] linkDetailsWithBase = new MaterialProperty[kMaxLayerCount];
        protected const string kLinkDetailsWithBase = "_LinkDetailsWithBase";
        protected MaterialProperty[] detailAlbedoScale = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailAlbedoScale = "_DetailAlbedoScale";
        protected MaterialProperty[] detailNormalScale = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailNormalScale = "_DetailNormalScale";
        protected MaterialProperty[] detailSmoothnessScale = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailSmoothnessScale = "_DetailSmoothnessScale";
        protected MaterialProperty[] UVBase = new MaterialProperty[kMaxLayerCount];
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty displacementMode = null;
        protected const string kDisplacementMode = "_DisplacementMode";

        Expandable  m_ExpandableBit;
        Features    m_Features;
        int         m_LayerIndex;
        int         m_LayerCount;
        Color       m_DotColor;

        bool        isLayeredLit => m_LayerCount > 1;

        public DetailInputsUIBlock(Expandable expandableBit, int layerCount = 1, int layerIndex = 0, Features features = Features.All, Color dotColor = default(Color))
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
            m_LayerIndex = layerIndex;
            m_LayerCount = layerCount;
            m_DotColor = dotColor;
        }

        public override void LoadMaterialProperties()
        {
            UVDetail = FindPropertyLayered(kUVDetail, m_LayerCount);
            UVDetailsMappingMask = FindPropertyLayered(kUVDetailsMappingMask, m_LayerCount);
            linkDetailsWithBase = FindPropertyLayered(kLinkDetailsWithBase, m_LayerCount);
            detailMap = FindPropertyLayered(kDetailMap, m_LayerCount);
            detailAlbedoScale = FindPropertyLayered(kDetailAlbedoScale, m_LayerCount);
            detailNormalScale = FindPropertyLayered(kDetailNormalScale, m_LayerCount);
            detailSmoothnessScale = FindPropertyLayered(kDetailSmoothnessScale, m_LayerCount);
            UVBase = FindPropertyLayered(kUVBase, m_LayerCount);
            displacementMode = FindProperty(kDisplacementMode);
        }

        public override void OnGUI()
        {
            bool subHeader = (m_Features & Features.SubHeader) != 0;

            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor, subHeader: subHeader, colorDot: m_DotColor))
            {
                if (header.expanded)
                    DrawDetailsGUI();
            }
        }

        void DrawDetailsGUI()
        {
            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase[m_LayerIndex].floatValue;
            float X, Y, Z, W;

            materialEditor.TexturePropertySingleLine(Styles.detailMapNormalText, detailMap[m_LayerIndex]);

            if (materials.All(m => m.GetTexture(isLayeredLit ? kDetailMap + m_LayerIndex : kDetailMap)))
            {
                EditorGUI.indentLevel++;

                // When Planar or Triplanar is enable the UVDetail use the same mode, so we disable the choice on UVDetail
                if (uvBaseMapping == UVBaseMapping.Planar)
                {
                    EditorGUILayout.LabelField(Styles.UVDetailMappingText.text + ": Planar");
                }
                else if (uvBaseMapping == UVBaseMapping.Triplanar)
                {
                    EditorGUILayout.LabelField(Styles.UVDetailMappingText.text + ": Triplanar");
                }
                else
                {
                    materialEditor.ShaderProperty(UVDetail[m_LayerIndex], Styles.UVDetailMappingText);
                }

                // Setup the UVSet for detail, if planar/triplanar is use for base, it will override the mapping of detail (See shader code)
                X = ((UVDetailMapping)UVDetail[m_LayerIndex].floatValue == UVDetailMapping.UV0) ? 1.0f : 0.0f;
                Y = ((UVDetailMapping)UVDetail[m_LayerIndex].floatValue == UVDetailMapping.UV1) ? 1.0f : 0.0f;
                Z = ((UVDetailMapping)UVDetail[m_LayerIndex].floatValue == UVDetailMapping.UV2) ? 1.0f : 0.0f;
                W = ((UVDetailMapping)UVDetail[m_LayerIndex].floatValue == UVDetailMapping.UV3) ? 1.0f : 0.0f;
                UVDetailsMappingMask[m_LayerIndex].colorValue = new Color(X, Y, Z, W);

                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(linkDetailsWithBase[m_LayerIndex], Styles.linkDetailsWithBaseText);
                EditorGUI.indentLevel--;

                materialEditor.TextureScaleOffsetProperty(detailMap[m_LayerIndex]);
                if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Pixel && (UVDetail[m_LayerIndex].floatValue != UVBase[m_LayerIndex].floatValue))
                {
                    if (materials.All(m => m.GetTexture(isLayeredLit ? kDetailMap + m_LayerIndex : kDetailMap)))
                        EditorGUILayout.HelpBox(Styles.perPixelDisplacementDetailsWarning.text, MessageType.Warning);
                }
                materialEditor.ShaderProperty(detailAlbedoScale[m_LayerIndex], Styles.detailAlbedoScaleText);
                materialEditor.ShaderProperty(detailNormalScale[m_LayerIndex], Styles.detailNormalScaleText);
                materialEditor.ShaderProperty(detailSmoothnessScale[m_LayerIndex], Styles.detailSmoothnessScaleText);
                EditorGUI.indentLevel--;
            }
        }
    }
}

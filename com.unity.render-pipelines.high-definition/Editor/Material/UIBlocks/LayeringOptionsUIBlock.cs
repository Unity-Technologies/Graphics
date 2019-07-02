using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class LayeringOptionsUIBlock : MaterialUIBlock
    {
        public static class Styles
        {
            public const string header = "Layering Options";
            public static readonly GUIContent layerInfluenceMapMaskText = EditorGUIUtility.TrTextContent("Layer Influence Mask", "Specifies the Layer Influence Mask for this Material.");
            public static readonly GUIContent opacityAsDensityText = EditorGUIUtility.TrTextContent("Use Opacity map as Density map", "When enabled, HDRP uses the opacity map (alpha channel of Base Color) as the Density map.");
            public static readonly GUIContent inheritBaseNormalText = EditorGUIUtility.TrTextContent("Normal influence", "Controls the strength of the normals inherited from the base layer.");
            public static readonly GUIContent inheritBaseHeightText = EditorGUIUtility.TrTextContent("Heightmap influence", "Controls the strength of the height map inherited from the base layer.");
            public static readonly GUIContent inheritBaseColorText = EditorGUIUtility.TrTextContent("BaseColor influence", "Controls the strength of the Base Color inherited from the base layer.");
        }

        // Influence
        MaterialProperty[] inheritBaseNormal = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseNormal = "_InheritBaseNormal";
        MaterialProperty[] inheritBaseHeight = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseHeight = "_InheritBaseHeight";
        MaterialProperty[] inheritBaseColor = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseColor = "_InheritBaseColor";

        // Layer Options
        MaterialProperty layerInfluenceMaskMap = null;
        const string kLayerInfluenceMaskMap = "_LayerInfluenceMaskMap";
        MaterialProperty useMainLayerInfluence = null;
        const string kkUseMainLayerInfluence = "_UseMainLayerInfluence";

        Expandable  m_ExpandableBit;
        int         m_LayerIndex;

        MaterialUIBlockList transparencyBlocks = new MaterialUIBlockList
        {
            new RefractionUIBlock(kMaxLayerCount),
            new DistortionUIBlock(),
        };

        // Density/opacity mode
        MaterialProperty[] opacityAsDensity = new MaterialProperty[kMaxLayerCount];
        const string kOpacityAsDensity = "_OpacityAsDensity";

        public LayeringOptionsUIBlock(Expandable expandableBit, int layerIndex)
        {
            m_ExpandableBit = expandableBit;
            m_LayerIndex = layerIndex;
        }

        public override void LoadMaterialProperties()
        {
            useMainLayerInfluence = FindProperty(kkUseMainLayerInfluence);
            layerInfluenceMaskMap = FindProperty(kLayerInfluenceMaskMap);
            // Density/opacity mode
            opacityAsDensity = FindPropertyLayered(kOpacityAsDensity, kMaxLayerCount);

            for (int i = 1; i < kMaxLayerCount; ++i)
            {
                // Influence
                inheritBaseNormal[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseNormal, i));
                inheritBaseHeight[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseHeight, i));
                inheritBaseColor[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseColor, i));
            }
        }

        public override void OnGUI()
        {
            // We're using a subheader here because we know that layering options are only used within layers
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor, colorDot: kLayerColors[m_LayerIndex], subHeader: true))
            {
                if (header.expanded)
                {
                    DrawLayeringOptionsGUI();
                }
            }
        }

        void DrawLayeringOptionsGUI()
        {
            bool mainLayerInfluenceEnable = useMainLayerInfluence.floatValue > 0.0f;
            // Main layer does not have any options but height base blend.
            if (m_LayerIndex > 0)
            {
                materialEditor.ShaderProperty(opacityAsDensity[m_LayerIndex], Styles.opacityAsDensityText);

                if (mainLayerInfluenceEnable)
                {
                    materialEditor.ShaderProperty(inheritBaseColor[m_LayerIndex - 1], Styles.inheritBaseColorText);
                    materialEditor.ShaderProperty(inheritBaseNormal[m_LayerIndex - 1], Styles.inheritBaseNormalText);
                    // Main height influence is only available if the shader use the heightmap for displacement (per vertex or per level)
                    // We always display it as it can be tricky to know when per pixel displacement is enabled or not
                    materialEditor.ShaderProperty(inheritBaseHeight[m_LayerIndex - 1], Styles.inheritBaseHeightText);
                }
            }
            else
            {
                materialEditor.TexturePropertySingleLine(Styles.layerInfluenceMapMaskText, layerInfluenceMaskMap);
            }
        }
    }
}
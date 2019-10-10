using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class ShadowMatteUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Shadow Tint";

            public static GUIContent colorText          = new GUIContent("Shadow Tint", " Shadow Tint (RGB) and Transparency (A).");
            public static GUIContent pointLightShadow   = new GUIContent("Point Light Shadow", " Enable Point Light Shadow.");
            public static GUIContent dirLightShadow     = new GUIContent("Directional Light Shadow", " Enable Directional Light Shadow.");
            public static GUIContent rectLightShadow    = new GUIContent("Rectangular Light Shadow", " Enable Rectangular Light Shadow.");
        }

        Expandable m_ExpandableBit;

        protected MaterialProperty color = null;
        public static string kColor = "_ShadowTint";
        protected MaterialProperty colorMap = null;
        public static string kColorMap = "_ShadowTintMap";
        protected MaterialProperty shadowFilterPoint = null;
        public static string kShadowFilterPoint = "_ShadowFilterPoint";
        protected MaterialProperty shadowFilterDir = null;
        public static string kShadowFilterDir = "_ShadowFilterDir";
        protected MaterialProperty shadowFilterRect = null;
        public static string kShadowFilterRect = "_ShadowFilterRect";

        public ShadowMatteUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            color               = FindProperty(kColor);
            colorMap            = FindProperty(kColorMap);
            shadowFilterPoint   = FindProperty(kShadowFilterPoint);
            shadowFilterDir     = FindProperty(kShadowFilterDir);
            shadowFilterRect    = FindProperty(kShadowFilterRect);
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawSurfaceInputsGUI();
            }
        }

        void DrawSurfaceInputsGUI()
        {
            materialEditor.TexturePropertySingleLine(Styles.colorText, colorMap, color);
            materialEditor.TextureScaleOffsetProperty(colorMap);
            //uint shadowFlags = unchecked((uint)shadowFilter.floatValue);
            //materialEditor.ShaderProperty(Styles.pointLightShadow, "Pts");
            //materialEditor.ShaderProperty(Styles.dirLightShadow, "Dir");
            //materialEditor.ShaderProperty(Styles.rectLightShadow, "Rect");
            //EditorGUILayout.Toggle("BRDF Color Table Diagonal Clamping", (shadowFlags & 16) != 0);
            //bool usePointLightShadow    = EditorGUILayout.Toggle("Point Light Shadow", (shadowFlags & unchecked((uint)LightFeatureFlags.Punctual) != 0));
            shadowFilterPoint.floatValue    = EditorGUILayout.Toggle("Point Light Shadow", shadowFilterPoint.floatValue == 1.0f ? true : false) ? 1.0f : 0.0f;
            shadowFilterDir.floatValue      = EditorGUILayout.Toggle("Directional Light Shadow", shadowFilterDir.floatValue == 1.0f ? true : false) ? 1.0f : 0.0f;
            shadowFilterRect.floatValue     = EditorGUILayout.Toggle("Areaa Light Shadow", shadowFilterRect.floatValue == 1.0f ? true : false) ? 1.0f : 0.0f;
            //uint shadowFilter = 0u;
            //uint finalFlag = 0x00000000;
            //if (usePointLightShadow)
            //    finalFlag |= unchecked((uint)LightFeatureFlags.Punctual);
            //if (useDirLightShadow)
            //    finalFlag |= unchecked((uint)LightFeatureFlags.Directional);
            //if (useRecLightShadow)
            //    finalFlag |= unchecked((uint)LightFeatureFlags.Area);
            //m_SkyHDRIMaterial.SetInt(HDShaderIDs._BackplateShadowFilter, unchecked((int)shadowFilter));
        }
    }
}

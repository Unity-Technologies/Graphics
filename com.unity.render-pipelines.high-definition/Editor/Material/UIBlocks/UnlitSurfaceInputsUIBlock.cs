using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class UnlitSurfaceInputsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Surface Inputs";

            public static GUIContent colorText = new GUIContent("Color", " Albedo (RGB) and Transparency (A).");
        }

        Expandable  m_ExpandableBit;

        protected MaterialProperty color = null;
        protected const string kColor = "_UnlitColor";
        protected MaterialProperty colorMap = null;
        protected const string kColorMap = "_UnlitColorMap";

        public UnlitSurfaceInputsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            color = FindProperty(kColor);
            colorMap = FindProperty(kColorMap);
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
        }
    }
}

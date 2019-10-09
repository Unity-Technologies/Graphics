using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class ShadowMatteShadowTintUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Shadow Tint";

            public static GUIContent colorText = new GUIContent("Shadow Tint", " Shadow Tint (RGB) and Transparency (A).");
        }

        Expandable  m_ExpandableBit;

        protected MaterialProperty color = null;
        public static string kColor = "_ShadowTint";
        protected MaterialProperty colorMap = null;
        public static string kColorMap = "_ShadowTintMap";

        public ShadowMatteShadowTintUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            color       = FindProperty(kColor);
            colorMap    = FindProperty(kColorMap);
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

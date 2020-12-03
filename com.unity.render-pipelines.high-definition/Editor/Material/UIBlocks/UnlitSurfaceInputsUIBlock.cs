using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents surface inputs for unlit materials.
    /// </summary>
    public class UnlitSurfaceInputsUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public const string header = "Surface Inputs";

            public static GUIContent colorText = new GUIContent("Color", " Albedo (RGB) and Transparency (A).");
        }

        ExpandableBit  m_ExpandableBit;

        MaterialProperty color = null;
        const string kColor = "_UnlitColor";
        MaterialProperty colorMap = null;
        const string kColorMap = "_UnlitColorMap";

        /// <summary>
        /// Constructs an UnlitSurfaceInputsUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        public UnlitSurfaceInputsUIBlock(ExpandableBit expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            color = FindProperty(kColor);
            colorMap = FindProperty(kColorMap);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
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

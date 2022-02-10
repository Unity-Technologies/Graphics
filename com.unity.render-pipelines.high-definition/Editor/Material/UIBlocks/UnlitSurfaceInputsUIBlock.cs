using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents surface inputs for unlit materials.
    /// </summary>
    public class UnlitSurfaceInputsUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Surface Inputs");

            public static GUIContent colorText = new GUIContent("Color", " Albedo (RGB) and Transparency (A).");
            public static GUIContent alphaRemappingText = new GUIContent("Alpha Remapping", "Controls a remap for the alpha channel in the Base Color.");
        }

        MaterialProperty color = null;
        const string kColor = "_UnlitColor";
        MaterialProperty colorMap = null;
        const string kColorMap = "_UnlitColorMap";
        MaterialProperty alphaRemapMin = null;
        const string kAlphaRemapMin = "_AlphaRemapMin";
        MaterialProperty alphaRemapMax = null;
        const string kAlphaRemapMax = "_AlphaRemapMax";

        /// <summary>
        /// Constructs an UnlitSurfaceInputsUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        public UnlitSurfaceInputsUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            color = FindProperty(kColor);
            colorMap = FindProperty(kColorMap);

            alphaRemapMin = FindProperty(kAlphaRemapMin);
            alphaRemapMax = FindProperty(kAlphaRemapMax);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            materialEditor.TexturePropertySingleLine(Styles.colorText, colorMap, color);

            if (colorMap.textureValue != null)
            {
                materialEditor.MinMaxShaderProperty(alphaRemapMin, alphaRemapMax, 0.0f, 1.0f, Styles.alphaRemappingText);
            }

            materialEditor.TextureScaleOffsetProperty(colorMap);
        }
    }
}

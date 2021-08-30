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
        }

        MaterialProperty color = null;
        const string kColor = "_UnlitColor";
        MaterialProperty colorMap = null;
        const string kColorMap = "_UnlitColorMap";

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
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            materialEditor.TexturePropertySingleLine(Styles.colorText, colorMap, color);
            materialEditor.TextureScaleOffsetProperty(colorMap);
        }
    }
}

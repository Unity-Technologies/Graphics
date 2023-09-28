using System;
using UnityEngine;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents Transparency properties for materials.
    /// </summary>
    public class SixWayUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Six-way Options");
            public static GUIContent receiveShadowsText = new GUIContent("Receive Shadows", "Receive Shadows");
            public static GUIContent useColorAbsorptionText = new GUIContent("Use Color Absorption", "Use Color Absorption");
        }

        MaterialProperty receiveShadows = new MaterialProperty();
        MaterialProperty useColorAbsorption = new MaterialProperty();

        /// <summary>
        /// Constructs a SixWayUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        public SixWayUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            receiveShadows = FindProperty(kReceiveShadows);
            useColorAbsorption = FindProperty(kUseColorAbsorption);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            if(receiveShadows != null)
                materialEditor.ShaderProperty(receiveShadows, Styles.receiveShadowsText);
            if(useColorAbsorption != null)
                materialEditor.ShaderProperty(useColorAbsorption, Styles.useColorAbsorptionText);
        }
    }
}

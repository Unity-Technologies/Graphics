using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using UnityEditor.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents Transparency properties for materials.
    /// </summary>
    public class TransparencyUIBlock : MaterialUIBlock
    {
        /// <summary>Transparency UI Block features. Use this to select which field you want to show.</summary>
        [Flags]
        public enum Features
        {
            /// <summary>Hides all the fields.</summary>
            None = 0,
            /// <summary>Displays the distortion fields.</summary>
            Distortion = 1 << 0,
            /// <summary>Displays the refraction fields.</summary>
            Refraction = 1 << 1,
            /// <summary>Displays all the fields.</summary>
            All = ~0
        }

        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Transparency Inputs");
        }

        Features m_Features;
        MaterialUIBlockList m_TransparencyBlocks;

        /// <summary>
        /// Constructs a TransparencyUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        /// <param name="features">Features of the Transparency block.</param>
        public TransparencyUIBlock(ExpandableBit expandableBit, Features features = Features.All)
            : base(expandableBit, Styles.header)
        {
            m_Features = features;

            m_TransparencyBlocks = new MaterialUIBlockList(parent);
            if ((features & Features.Refraction) != 0)
                m_TransparencyBlocks.Add(new RefractionUIBlock(1));  // This block will not be used in by a layered shader so we can safely set the layer count to 1
            if ((features & Features.Distortion) != 0)
                m_TransparencyBlocks.Add(new DistortionUIBlock());
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties() { }

        /// <summary>
        /// If the section should be shown
        /// </summary>
        protected override bool showSection
        {
            get
            {
                // Disable the block if one of the materials is not transparent:
                if (materials.Any(material => material.GetSurfaceType() != SurfaceType.Transparent))
                    return false;

                // If refraction model is not enabled in SG, we don't show the section
                if (materials[0].IsShaderGraph())
                {
                    var shader = materials[0].shader;

                    var propertyIndex = shader.FindPropertyIndex(kRefractionModel);
                    if (propertyIndex == -1)
                        return false;

                    var defaultRefractionModel = shader.GetPropertyDefaultFloatValue(propertyIndex);
                    if (defaultRefractionModel == 0)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            m_TransparencyBlocks.OnGUI(materialEditor, properties);
        }
    }
}

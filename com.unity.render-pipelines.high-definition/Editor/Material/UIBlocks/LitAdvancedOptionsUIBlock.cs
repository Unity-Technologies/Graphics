using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Represents an advanced options material UI block.
    /// </summary>
    public class LitAdvancedOptionsUIBlock : AdvancedOptionsUIBlock
    {
        internal new class Styles
        {
            public static readonly GUIContent forceForwardEmissiveText = new GUIContent("Force Forward Emissive", "When in Lit shader mode: Deferred. It force the emissive part of the material to be render into an additional forward pass. This can improve quality and solve artifact with effects (SSGI) but have additional CPU and GPU cost.");
        }

        MaterialProperty forceForwardEmissive = null;
        const string kForceForwardEmissive = HDMaterialProperties.kForceForwardEmissive;

        /// <summary>
        /// Constructs the AdvancedOptionsUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        /// <param name="features">Features of the block.</param>
        public LitAdvancedOptionsUIBlock(ExpandableBit expandableBit, Features features = Features.All) : base(expandableBit, features) {}

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            base.LoadMaterialProperties();
            forceForwardEmissive = FindProperty(kForceForwardEmissive);            
        }

        protected override void DrawAdvancedOptionsGUI()
        {
            base.DrawAdvancedOptionsGUI();

            if (forceForwardEmissive != null)
                materialEditor.ShaderProperty(forceForwardEmissive, Styles.forceForwardEmissiveText);
        }
    }
}

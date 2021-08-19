using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.AdvancedOptionsUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HairSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        class Styles
        {
            public static GUIContent materialType = new GUIContent("Material Type", "TODO");
        }

        HairData hairData;

        public HairSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, HairData hairData) : base(features)
            => this.hairData = hairData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(Styles.materialType, () => hairData.materialType, (newValue) => hairData.materialType = newValue);

            base.CreatePropertyGUI();
        }
    }

    class HairAdvancedOptionsPropertyBlock : AdvancedOptionsPropertyBlock
    {
        class Styles
        {
            public static GUIContent geometryType = new GUIContent("Geometry Type", "Indicates the type of geometry being used to represent the hair, allowing the shading model to make informed approximations.");
            public static GUIContent scatteringMode = new GUIContent("Scattering Mode", "TODO");
            public static GUIContent useRoughenedAzimuthalScattering = new GUIContent("Allow Radial Smoothness", "Adds a Radial Smoothness block to the target, controlling the internal scattering of the light paths and absorption that occurs within the fiber.");
        }

        HairData hairData;

        public HairAdvancedOptionsPropertyBlock(HairData hairData) => this.hairData = hairData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // Hair specific properties GUI
            AddProperty(Styles.geometryType, () => hairData.geometryType, (newValue) => hairData.geometryType = newValue);

            if (hairData.materialType == HairData.MaterialType.Marschner)
            {
                // Note: Un-hide me when the improved multiple scattering approximation is available.
                // AddProperty(Styles.scatteringMode, () => hairData.scatteringMode, (newValue) => hairData.scatteringMode = newValue);

                AddProperty(Styles.useRoughenedAzimuthalScattering, () => hairData.useRoughenedAzimuthalScattering, (newValue) => hairData.useRoughenedAzimuthalScattering = newValue);
            }
        }
    }
}

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
            public static GUIContent materialType = new GUIContent("Material Type", "Indicates the type of Shading Model used to evaluate lighting.");
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
            public static GUIContent colorParameterization = new GUIContent("Color Mode", "Indicates the way the hair fiber cortex color is parameterized.");
            public static GUIContent geometryType = new GUIContent("Geometry Type", "Indicates the type of geometry being used to represent the hair, allowing the shading model to make informed approximations.");
            public static GUIContent scatteringMode = new GUIContent("Scattering Mode", "Indicates the light scattering method in a volume of hair.");
        }

        HairData hairData;

        public HairAdvancedOptionsPropertyBlock(HairData hairData) => this.hairData = hairData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // Hide the color mode for now and let it silently default to Base Color. We will discuss with artists before we expose it.
            // AddProperty(Styles.colorParameterization, () => hairData.colorParameterization, (newValue) => hairData.colorParameterization = newValue);

            // Hair specific properties GUI
            AddProperty(Styles.geometryType, () => hairData.geometryType, (newValue) => hairData.geometryType = newValue);

            if (hairData.materialType == HairData.MaterialType.Physical)
            {
                // For now only allow scattering mode for strands, as the multiple scattering was developed against this for 21.2.
                if (hairData.geometryType == HairData.GeometryType.Strands)
                    AddProperty(Styles.scatteringMode, () => hairData.scatteringMode, (newValue) => hairData.scatteringMode = newValue);
            }
        }
    }
}

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

            if (hairData.materialType == HairData.MaterialType.PhysicalCinematic)
            {
            #if !HAS_UNITY_HAIR_PACKAGE
                context.AddHelpBox(MessageType.Error, "Cinematic physically-based hair shading\nrequires the com.unity.demoteam.hair package.");
                return;
            #endif
            }

            base.CreatePropertyGUI();
        }
    }

    class HairAdvancedOptionsPropertyBlock : AdvancedOptionsPropertyBlock
    {
        new class Styles
        {
            public static GUIContent colorParameterization = new GUIContent("Color Mode", "Indicates the way the hair fiber cortex color is parameterized.");
            public static GUIContent geometryType = new GUIContent("Geometry Type", "Indicates the type of geometry being used to represent the hair, allowing the shading model to make informed approximations.");
            public static GUIContent multipleScatteringVisibility = new GUIContent("Visibility Source", "Choose the method by which self-shadowing is inferred, this affects the scattering result.");
            public static GUIContent environmentSamples = new GUIContent("Environment Light Samples", ".");
            public static GUIContent areaLightSamples = new GUIContent("Area Light Samples", ".");
        }

        HairData hairData;

        public HairAdvancedOptionsPropertyBlock(HairData hairData) => this.hairData = hairData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // Hide the color mode for now and let it silently default to Base Color. We will discuss with artists before we expose it.
            // AddProperty(Styles.colorParameterization, () => hairData.colorParameterization, (newValue) => hairData.colorParameterization = newValue);

            if (hairData.materialType == HairData.MaterialType.Approximate)
            {
                // Light-facing normal only affects diffuse-components of the kajiya model (marschner has no diffuse component).
                AddProperty(Styles.geometryType, () => hairData.geometryType, (newValue) => hairData.geometryType = newValue);
            }

            if (hairData.materialType == HairData.MaterialType.PhysicalCinematic)
            {
                AddProperty(Styles.multipleScatteringVisibility, () => hairData.directionalFractionMode, (newValue) => hairData.directionalFractionMode = newValue);

                AddProperty(Styles.environmentSamples, () => hairData.environmentSamples, (newValue) => hairData.environmentSamples = newValue);
                AddProperty(Styles.areaLightSamples, () => hairData.areaLightSamples, (newValue) => hairData.areaLightSamples = newValue);
            }
        }
    }
}

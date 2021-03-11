using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.LitSurfaceInputsUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.RefractionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class EyeSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        class Styles
        {
            public static GUIContent materialType = new GUIContent("Material Type", "Allow to select the type of lighting model used with this Eye Material.");
            public static GUIContent irisNormalType = new GUIContent("Iris Normal", "Override the iris normal");
        }

        EyeData eyeData;

        public EyeSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, EyeData eyeData) : base(features)
            => this.eyeData = eyeData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(Styles.materialType, () => eyeData.materialType, (newValue) => eyeData.materialType = newValue);

            base.CreatePropertyGUI();

            // Eye specific properties:
            AddProperty(subsurfaceEnableText, () => eyeData.subsurfaceScattering, (newValue) => eyeData.subsurfaceScattering = newValue);
            AddProperty(Styles.irisNormalType, () => eyeData.irisNormal, (newValue) => eyeData.irisNormal = newValue);
        }
    }
}

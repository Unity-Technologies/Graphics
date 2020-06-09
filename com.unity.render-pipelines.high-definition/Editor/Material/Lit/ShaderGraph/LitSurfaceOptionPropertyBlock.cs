using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.LitSurfaceInputsUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.RefractionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class LitSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        HDLitData litData;
    
        class Styles
        {
            public static GUIContent enableClearCoat = new GUIContent("Clear Coat", "Enable Clear Coat");
        }

        public LitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, HDLitData litData) : base(features)
            => this.litData = litData;

        protected override void CreatePropertyGUI()
        {
            // Lit specific properties:
            AddProperty(materialIDText, () => litData.materialType, (newValue) => litData.materialType = newValue);
            AddProperty(rayTracingText, () => litData.rayTracing, (newValue) => litData.rayTracing = newValue);

            base.CreatePropertyGUI();

            AddProperty(Styles.enableClearCoat, () => litData.clearCoat, (newValue) => litData.clearCoat = newValue);
            AddProperty(transmissionEnableText, () => litData.sssTransmission, (newValue) => litData.sssTransmission = newValue);
            AddProperty(refractionModelText, () => litData.refractionModel, (newValue) => litData.refractionModel = newValue);
            AddProperty(energyConservingSpecularColorText, () => litData.energyConservingSpecular, (newValue) => litData.energyConservingSpecular = newValue);
        }
    }
}

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
    class FabricSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        class Styles
        {
            public static GUIContent materialType = new GUIContent("Material Type", "Allow to select the type of lighting model used with this Fabric Material. Either for cooton wood or for Silk.");
        }

        FabricData fabricData;

        public FabricSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, FabricData fabricData) : base(features)
            => this.fabricData = fabricData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(Styles.materialType, () => fabricData.materialType, (newValue) => fabricData.materialType = newValue);

            base.CreatePropertyGUI();

            // Fabric specific properties:
            AddProperty(energyConservingSpecularColorText, () => fabricData.energyConservingSpecular, (newValue) => fabricData.energyConservingSpecular = newValue);
            AddProperty(subsurfaceEnableText, () => fabricData.subsurfaceScattering, (newValue) => fabricData.subsurfaceScattering = newValue);
            AddProperty(transmissionEnableText, () => fabricData.transmission, (newValue) => fabricData.transmission = newValue);
        }
    }
}

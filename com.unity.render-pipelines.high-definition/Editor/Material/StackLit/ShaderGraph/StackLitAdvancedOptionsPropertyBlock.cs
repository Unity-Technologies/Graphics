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
    class StackLitAdvancedOptionsPropertyBlock : AdvancedOptionsPropertyBlock
    {
        StackLitData stackLitData;

        public StackLitAdvancedOptionsPropertyBlock(StackLitData stackLitData) : base(AdvancedOptionsPropertyBlock.Features.StackLit)
            => this.stackLitData = stackLitData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // StackLit specific advanced properties GUI
            //context.AddLabel("Advanced Options", 0);
            AddProperty("Anisotropy For Area Lights", () => stackLitData.anisotropyForAreaLights, (newValue) => stackLitData.anisotropyForAreaLights = newValue, 0);

            // Per Punctual/Directional Lights
            context.AddLabel("Per Punctual/Directional Lights:", 0);
            if (stackLitData.coat)
                AddProperty("Base Layer Uses Refracted Angles", () => stackLitData.shadeBaseUsingRefractedAngles, (newValue) => stackLitData.shadeBaseUsingRefractedAngles = newValue, 1);
            if (stackLitData.coat || stackLitData.iridescence)
                AddProperty("Recompute Stack & Iridescence", () => stackLitData.recomputeStackPerLight, (newValue) => stackLitData.recomputeStackPerLight = newValue, 1);
            AddProperty("Honor Per Light Max Smoothness", () => stackLitData.honorPerLightMinRoughness, (newValue) => stackLitData.honorPerLightMinRoughness = newValue, 1);
        }
    }
}

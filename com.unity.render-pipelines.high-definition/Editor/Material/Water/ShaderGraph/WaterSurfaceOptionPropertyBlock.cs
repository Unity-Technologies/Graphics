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
    class WaterSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        class Styles
        {
            public static GUIContent materialType = new GUIContent("Material Type", "Allow to select the type of lighting model used with this Water Material.");
        }

        WaterData waterData;

        public WaterSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, WaterData waterData) : base(features)
            => this.waterData = waterData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(Styles.materialType, () => waterData.materialType, (newValue) => waterData.materialType = newValue);
            base.CreatePropertyGUI();
        }
    }
}

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
        WaterData waterData;

        public WaterSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, WaterData waterData) : base(features)
            => this.waterData = waterData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();
        }
    }
}

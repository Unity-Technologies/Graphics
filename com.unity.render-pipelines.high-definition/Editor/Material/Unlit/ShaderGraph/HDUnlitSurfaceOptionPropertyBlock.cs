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
    class HDUnlitSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        class Styles
        {
            public static GUIContent shadowMatte = new GUIContent("Shadow Matte", "When enabled, shadow matte inputs are exposed on the master node.");
        }

        HDUnlitData unlitData;

        public HDUnlitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, HDUnlitData unlitData) : base(features)
            => this.unlitData = unlitData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // HDUnlit specific properties:
            AddProperty(Styles.shadowMatte, () => unlitData.enableShadowMatte, (newValue) => unlitData.enableShadowMatte = newValue);
        }
    }
}

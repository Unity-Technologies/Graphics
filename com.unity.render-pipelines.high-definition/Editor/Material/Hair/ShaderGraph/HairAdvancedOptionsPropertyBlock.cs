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
    class HairAdvancedOptionsPropertyBlock : AdvancedOptionsPropertyBlock
    {
        class Styles
        {
            public static GUIContent useLightFacingNormal = new GUIContent("Use Light Facing Normal", "TODO");
        }

        HairData hairData;

        public HairAdvancedOptionsPropertyBlock(HairData hairData) => this.hairData = hairData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // Hair specific properties GUI
            AddProperty(Styles.useLightFacingNormal, () => hairData.useLightFacingNormal, (newValue) => hairData.useLightFacingNormal = newValue);
        }
    }
}

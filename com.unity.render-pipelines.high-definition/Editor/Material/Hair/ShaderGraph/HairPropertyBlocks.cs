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
            public static GUIContent materialType = new GUIContent("Material Type", "TODO");
        }

        HairData hairData;

        public HairSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, HairData hairData) : base(features)
            => this.hairData = hairData;

        protected override void CreatePropertyGUI()
        {
            // TODO: Un-hide me when Marschner BSDF is available.
            // AddProperty(Styles.materialType, () => hairData.materialType, (newValue) => hairData.materialType = newValue);

            base.CreatePropertyGUI();
        }
    }

    class HairAdvancedOptionsPropertyBlock : AdvancedOptionsPropertyBlock
    {
        class Styles
        {
            public static GUIContent useLightFacingNormal = new GUIContent("Use Light Facing Normal", "TODO");
            public static GUIContent scatteringMode = new GUIContent("Scattering Mode", "");
        }

        HairData hairData;

        public HairAdvancedOptionsPropertyBlock(HairData hairData) => this.hairData = hairData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // Hair specific properties GUI
            AddProperty(Styles.useLightFacingNormal, () => hairData.useLightFacingNormal, (newValue) => hairData.useLightFacingNormal = newValue);

            if (hairData.materialType == HairData.MaterialType.Marschner)
                AddProperty(Styles.scatteringMode, () => hairData.scatteringMode, (newValue) => hairData.scatteringMode = newValue);
        }
    }
}

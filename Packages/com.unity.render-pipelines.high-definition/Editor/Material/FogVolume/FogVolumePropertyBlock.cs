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
    class FogVolumePropertyBlock : SubTargetPropertyBlock
    {
        internal class Styles
        {
            public static GUIContent blendMode = new GUIContent("Blend Mode", "Determines how the fog volume will blend with other fogs in the scene.");
            public static GUIContent singleScatteringAlbedo = new GUIContent("Single Scattering Albedo", "The color this fog scatters light to.");
            public static GUIContent fogDistance = new GUIContent("Fog Distance", "Density at the base of the fog. Determines how far you can see through the fog in meters.");
            public static GUIContent debugSymbolsText = new GUIContent("Debug Symbols", "When enabled, HDRP activates d3d11 debug symbols for this Shader.");
        }

        protected override string title => "Fog Volume Options";
        protected override int foldoutIndex => 0;

        readonly FogVolumeData fogData;

        public FogVolumePropertyBlock(FogVolumeData fogData) => this.fogData = fogData;

        protected override void CreatePropertyGUI()
        {
            // TODO: enable these controls when the color picker will work in the ShaderGraph settings
            // AddProperty(Styles.singleScatteringAlbedo, () => fogData.singleScatteringAlbedo, (newValue) => fogData.singleScatteringAlbedo = newValue);
            // AddProperty(Styles.fogDistance, () => fogData.fogDistance, (newValue) => fogData.fogDistance = newValue);
            AddProperty(Styles.blendMode, () => fogData.blendMode, (newValue) => fogData.blendMode = newValue);

            if (Unsupported.IsDeveloperMode())
                AddProperty(Styles.debugSymbolsText, () => systemData.debugSymbols, (newValue) => systemData.debugSymbols = newValue);
        }
    }
}

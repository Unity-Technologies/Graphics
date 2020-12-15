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
    class AdvancedOptionsPropertyBlock : SubTargetPropertyBlock
    {
        /// <summary>Options that define the visibility of fields in the block.</summary>
        [Flags]
        public enum Features
        {
            None                    = 0,
            SpecularOcclusion       = 1 << 0,

            StackLit                = All ^ SpecularOcclusion,
            All                     = ~0
        }

        class Styles
        {
            public static GUIContent overrideBakedGI = new GUIContent("Override Baked GI", "When enabled, inputs to override the current GI are exposed on the master node.");
            public static GUIContent supportLodCrossFade = new GUIContent("Support LOD CrossFade", "When enabled, allow to use the animated transition for LOD feature on this material.");
        }

        Features enabledFeatures;

        protected override string title => "Advanced Options";
        protected override int foldoutIndex => 3;

        public AdvancedOptionsPropertyBlock(Features features = Features.All) => enabledFeatures = features;

        protected override void CreatePropertyGUI()
        {
            if (lightingData != null)
            {
                if ((enabledFeatures & Features.SpecularOcclusion) != 0)
                    AddProperty(specularOcclusionModeText, () => lightingData.specularOcclusionMode, (newValue) => lightingData.specularOcclusionMode = newValue);
                AddProperty(Styles.overrideBakedGI, () => lightingData.overrideBakedGI, (newValue) => lightingData.overrideBakedGI = newValue);
            }
            AddProperty(Styles.supportLodCrossFade, () => builtinData.supportLodCrossFade, (newValue) => builtinData.supportLodCrossFade = newValue);
            AddProperty(addPrecomputedVelocityText, () => builtinData.addPrecomputedVelocity, (newValue) => builtinData.addPrecomputedVelocity = newValue);
        }
    }
}

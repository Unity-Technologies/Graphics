using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.DistortionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class DistortionPropertyBlock : SubTargetPropertyBlock
    {
        protected override string title => "Distortion";
        protected override int foldoutIndex => 1;

        protected override void CreatePropertyGUI()
        {
            AddProperty(distortionEnableText, () => builtinData.distortion, (newValue) => builtinData.distortion = newValue);
            if (builtinData.distortion)
            {
                context.globalIndentLevel++;
                AddProperty(distortionBlendModeText, () => builtinData.distortionMode, (newValue) => builtinData.distortionMode = newValue);
                AddProperty(distortionDepthTestText, () => builtinData.distortionDepthTest, (newValue) => builtinData.distortionDepthTest = newValue);
                context.globalIndentLevel--;
            }
        }
    }
}

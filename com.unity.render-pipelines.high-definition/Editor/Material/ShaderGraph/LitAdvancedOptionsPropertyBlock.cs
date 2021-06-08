using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.LitAdvancedOptionsUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class LitAdvancedOptionsPropertyBlock : AdvancedOptionsPropertyBlock
    {
        HDLitData litData;

        public LitAdvancedOptionsPropertyBlock(HDLitData litData) => this.litData = litData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            AddProperty(forceForwardEmissiveText, () => litData.forceForwardEmissive, (newValue) => litData.forceForwardEmissive = newValue);
        }
    }
}

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
    class LitPropertyBlock : SubTargetPropertyBlock
    {
        protected override string title => "Lit Properties";
        protected override int foldoutIndex => 2;

        HDLitData litData;

        public LitPropertyBlock(HDLitData litData)
        {
            this.litData = litData;
        }

        protected override void CreatePropertyGUI()
        {
            
        }
    }
}
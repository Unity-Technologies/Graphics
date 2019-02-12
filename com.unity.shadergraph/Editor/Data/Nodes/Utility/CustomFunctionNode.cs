using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Custom Function")]
    class CustomFunctionNode : AbstractMaterialNode, IHasSettings
    {
        public CustomFunctionNode()
        {
            name = "Custom Function";
        }

        public override bool hasPreview => true;
        
        public VisualElement CreateSettingsElement()
        {
            PropertySheet ps = new PropertySheet();
            ps.Add(new ShaderValueDescriptorListView(this, SlotType.Input));
            ps.Add(new ShaderValueDescriptorListView(this, SlotType.Output));
            return ps;
        }
    }
}

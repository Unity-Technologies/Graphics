using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class LabelSlotControlView : VisualElement
    {
        public LabelSlotControlView(string label)
        {
            var labelField = new Label (label);
            Add(labelField);
        }
    }
}

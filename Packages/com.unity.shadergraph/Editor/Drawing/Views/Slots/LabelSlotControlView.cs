using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class LabelSlotControlView : VisualElement
    {
        public LabelSlotControlView(string label)
        {
            var labelField = new Label(label);
            Add(labelField);
        }
    }
}

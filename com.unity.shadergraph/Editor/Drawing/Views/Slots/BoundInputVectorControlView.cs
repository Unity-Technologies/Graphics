using System;
using UnityEditor.Graphing;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class BoundInputVectorControlView : VisualElement
    {
        public BoundInputVectorControlView(string label)
        {
            AddStyleSheetPath("Styles/Controls/BoundInputVectorSlotControlView");
            Add(new Label(label));
        }
    }
}

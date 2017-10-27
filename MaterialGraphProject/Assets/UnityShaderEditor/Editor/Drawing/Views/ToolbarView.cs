using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class ToolbarView : VisualElement
    {
        public ToolbarView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }

    public class ToolbarButtonView : VisualElement
    {
        public ToolbarButtonView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }

    public class ToolbarSeparatorView : VisualElement
    {
        public ToolbarSeparatorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }

    public class ToolbarSpaceView : VisualElement
    {
        public ToolbarSpaceView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }
}

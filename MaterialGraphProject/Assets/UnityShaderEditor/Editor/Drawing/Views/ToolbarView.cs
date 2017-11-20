using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class ToolbarView : BaseTextElement
    {
        public ToolbarView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }

    public class ToolbarButtonView : BaseTextElement
    {
        public ToolbarButtonView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }

    public class ToolbarSeparatorView : BaseTextElement
    {
        public ToolbarSeparatorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }

    public class ToolbarSpaceView : BaseTextElement
    {
        public ToolbarSpaceView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
        }
    }
}

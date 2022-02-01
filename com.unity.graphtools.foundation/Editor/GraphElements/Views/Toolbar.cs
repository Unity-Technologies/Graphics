using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for toolbars in GraphTools Foundation.
    /// </summary>
    public class Toolbar : UIElements.Toolbar
    {
        public new static readonly string ussClassName = "ge-toolbar";

        /// <summary>
        /// The graph view associated with this toolbar.
        /// </summary>
        protected GraphView GraphView { get; }

        /// <summary>
        /// The graph tool.
        /// </summary>
        protected BaseGraphTool GraphTool { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Toolbar"/> class.
        /// </summary>
        /// <param name="graphTool">The <see cref="BaseGraphTool"/> of the toolbar.</param>
        /// <param name="graphView">The associated graph view.</param>
        public Toolbar(BaseGraphTool graphTool, GraphView graphView)
        {
            AddToClassList(ussClassName);
            this.AddStylesheet("Toolbar.uss");

            GraphTool = graphTool;
            GraphView = graphView;
        }
    }
}

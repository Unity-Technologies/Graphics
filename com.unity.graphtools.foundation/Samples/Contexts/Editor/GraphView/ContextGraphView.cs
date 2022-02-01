using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class ContextGraphView : GraphView
    {
        ContextGraphViewWindow m_SimpleGraphViewWindow;

        public ContextGraphViewWindow window
        {
            get { return m_SimpleGraphViewWindow; }
        }

        public ContextGraphView(ContextGraphViewWindow simpleGraphViewWindow, BaseGraphTool graphTool, string name)
            : base(simpleGraphViewWindow, graphTool, name)
        {
            m_SimpleGraphViewWindow = simpleGraphViewWindow;
        }

        private VisualElement m_LeftElement;

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            evt.menu.AppendAction("Internal/Toggle Element On Left of graphview",
                _ =>
                {
                    if (m_LeftElement != null)
                    {
                        m_LeftElement.RemoveFromHierarchy();
                        m_LeftElement = null;
                    }
                    else
                    {
                        m_LeftElement = new VisualElement() {name = "Test"};

                        m_LeftElement.style.width = 100;
                        m_LeftElement.style.flexGrow = 1;
                        parent.Insert(0, m_LeftElement);
                    }
                });
            evt.menu.AppendAction("Internal/Toggle graphview Absolute",
                _ =>
                {
                    if (this.resolvedStyle.position == Position.Absolute)
                    {
                        this.style.position = Position.Relative;
                        this.style.top = new StyleLength(StyleKeyword.Null);
                        this.style.left = new StyleLength(StyleKeyword.Null);
                        this.style.bottom = new StyleLength(StyleKeyword.Null);
                        this.style.right = new StyleLength(StyleKeyword.Null);
                    }
                    else
                    {
                        this.style.position = Position.Absolute;
                        this.style.top = 50;
                        this.style.left = 75;
                        this.style.bottom = 50;
                        this.style.right = 75;
                    }
                });
        }
    }
}

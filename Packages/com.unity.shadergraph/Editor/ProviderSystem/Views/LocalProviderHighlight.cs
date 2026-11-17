using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Hovering a local provider node highlights the other nodes of the same provider, the way hovering a property highlights its nodes.
    sealed class LocalProviderHighlight
    {
        // Not Shader Graph's "hovered", whose node style adds a border.
        internal const string kHighlightedClass = "sg-local-provider-highlighted";

        readonly MaterialGraphView m_GraphView;
        readonly List<MaterialNodeView> m_Highlighted = new();

        internal LocalProviderHighlight(MaterialGraphView graphView)
        {
            m_GraphView = graphView;
        }

        internal void Attach(MaterialNodeView view)
        {
            ProviderStyleSheets.Add(view, ProviderStyleSheets.kLocalProviderHighlight);
            view.RegisterCallback<MouseEnterEvent>(_ => Enter(view));
            view.RegisterCallback<MouseLeaveEvent>(_ => Leave());
        }

        void Enter(MaterialNodeView hovered)
        {
            Leave();
            if (hovered.node is not IProviderNode { isLocal: true, Provider: { } provider })
                return;

            m_GraphView.nodes.ForEach(view =>
            {
                if (view != hovered && view is MaterialNodeView { node: IProviderNode { isLocal: true } node } nodeView && node.Provider == provider)
                {
                    nodeView.AddToClassList(kHighlightedClass);
                    m_Highlighted.Add(nodeView);
                }
            });
        }

        void Leave()
        {
            foreach (var view in m_Highlighted)
                view.RemoveFromClassList(kHighlightedClass);
            m_Highlighted.Clear();
        }
    }
}

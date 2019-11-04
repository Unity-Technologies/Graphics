using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.UIElements;
using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;
using ContextualMenuPopulateEvent = UnityEngine.UIElements.ContextualMenuPopulateEvent;
using VisualElementExtensions = UnityEngine.UIElements.VisualElementExtensions;

namespace UnityEditor.ShaderGraph
{
    sealed class ShaderGroup : Group
    {
        GraphData m_Graph;
        public new GroupData userData
        {
            get => (GroupData)base.userData;
            set => base.userData = value;
        }

        public ShaderGroup(GraphData graph)
        {
            m_Graph = graph;
            VisualElementExtensions.AddManipulator(this, new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is ShaderGroup)
            {
                evt.menu.AppendAction("Delete Group and Contents", RemoveNodesInsideGroup, DropdownMenuAction.AlwaysEnabled);
            }
        }

        void RemoveNodesInsideGroup(DropdownMenuAction action)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Delete Group and Contents");
            var groupItems = m_Graph.GetItemsInGroup(userData);
            m_Graph.RemoveElements(groupItems.OfType<AbstractMaterialNode>().ToArray(), new IEdge[] {}, new [] {userData}, groupItems.OfType<StickyNoteData>().ToArray());
        }
    }
}


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
        public new GroupData userData
        {
            get => (GroupData)base.userData;
            set => base.userData = value;
        }

        public ShaderGroup()
        {
            VisualElementExtensions.AddManipulator(this, new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is ShaderGroup)
            {
                evt.menu.AppendAction("Ungroup All Nodes", RemoveNodesInsideGroup, DropdownMenuAction.AlwaysEnabled);
            }
        }

        void RemoveNodesInsideGroup(DropdownMenuAction action)
        {
            var elements = containedElements.ToList();
            foreach (GraphElement element in elements)
            {
                var node = element.userData as AbstractMaterialNode;
                if (node == null)
                    continue;

                RemoveElement(element);
            }
        }
    }
}


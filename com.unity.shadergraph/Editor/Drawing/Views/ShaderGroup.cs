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

        public ShaderGroup()
        {
            VisualElementExtensions.AddManipulator(this, new ContextualMenuManipulator(BuildContextualMenu));
            style.backgroundColor = new StyleColor(new Color(25/255f, 25/255f, 25/255f, 25/255f));
            capabilities |= Capabilities.Ascendable;
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }
    }
}


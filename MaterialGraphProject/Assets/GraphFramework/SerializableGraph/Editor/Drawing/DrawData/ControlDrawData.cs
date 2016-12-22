using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    public abstract class ControlDrawData : GraphElementPresenter
    {
        public INode node { get; private set; }

        protected ControlDrawData()
        {}

        public void Initialize(INode inNode)
        {
            node = inNode;
        }

        public virtual void OnGUIHandler()
        {
            if (node == null)
                return;

            GUIUtility.GetControlID(node.guid.GetHashCode(), FocusType.Passive);
        }

        public abstract float GetHeight();
    }
}

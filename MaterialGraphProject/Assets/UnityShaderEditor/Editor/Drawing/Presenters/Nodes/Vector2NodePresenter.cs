using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class Vector2ControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.Vector2Node;
            if (tNode == null)
                return;

            tNode.value = EditorGUILayout.Vector2Field("", tNode.value);
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class Vector2NodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<Vector2ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }

    public class Vector2NodeView : PropertyNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<Vector2ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class IfControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.IfNode;
            if (tNode == null)
                return;

            tNode.ComparisonOperation = (IfNode.ComparisonOperationType)EditorGUILayout.EnumPopup(tNode.ComparisonOperation);
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class IfNodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<IfControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter>(base.GetControlData()) { instance };
        }
    }
}

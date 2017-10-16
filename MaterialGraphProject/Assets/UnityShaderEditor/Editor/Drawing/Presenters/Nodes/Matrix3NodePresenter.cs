using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class Matrix3ControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.Matrix3Node;
            if (tNode == null)
                return;

            tNode[0] = EditorGUILayout.Vector3Field("", tNode[0]);
            tNode[1] = EditorGUILayout.Vector3Field("", tNode[1]);
            tNode[2] = EditorGUILayout.Vector3Field("", tNode[2]);
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    public class Matrix3NodeView : PropertyNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<Matrix3ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }
}

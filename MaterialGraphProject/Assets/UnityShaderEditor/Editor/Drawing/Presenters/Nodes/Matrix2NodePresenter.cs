using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class Matrix2ControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.Matrix2Node;
            if (tNode == null)
                return;

            tNode[0] = EditorGUILayout.Vector2Field("", tNode[0]);
            tNode[1] = EditorGUILayout.Vector2Field("", tNode[1]);
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

#if WITH_PRESENTER
    [Serializable]
    public class Matrix2NodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<Matrix2ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }
#endif

    public class Matrix2NodeView : PropertyNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<Matrix2ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }
}

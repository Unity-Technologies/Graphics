using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;


namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class ScatterContolPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var cNode = node as UnityEngine.MaterialGraph.ScatterNode;
            if (cNode == null)
                return;

            cNode.num = EditorGUILayout.IntField(cNode.num, "Number", null);
            cNode.num = Math.Min(cNode.num, 50); //prevent infinite => hang!
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + 10 * EditorGUIUtility.standardVerticalSpacing;
        }

    }

    [Serializable]
    public class ScatterNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<ScatterContolPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }


}

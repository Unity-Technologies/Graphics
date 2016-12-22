using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class SubgraphContolDrawData : ControlDrawData
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var subGraphNode = node as SubGraphNode;
            if (subGraphNode == null)
                return;

            subGraphNode.subGraphAsset = (MaterialSubGraphAsset)EditorGUILayout.MiniThumbnailObjectField(
                    new GUIContent("Subgraph"),
                    subGraphNode.subGraphAsset,
                    typeof(MaterialSubGraphAsset), null);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class SubgraphNodeDrawData : MaterialNodeDrawData
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<SubgraphContolDrawData>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class SubgraphContolPresenter : GraphControlPresenter
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
    public class SubgraphNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<SubgraphContolPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }

    public class SubgraphNodeView : MaterialNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<SubgraphContolPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }
}

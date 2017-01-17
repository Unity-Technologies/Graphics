using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class SurfaceMasterContolPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var cNode = node as AbstractSurfaceMasterNode;
            if (cNode == null)
                return;

            cNode.options.lod = EditorGUILayout.IntField("LOD", cNode.options.lod);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class SurfaceMasterPresenter : MasterNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<SurfaceMasterContolPresenter>();
            instance.Initialize(node);
            var controls = new List<GraphElementPresenter>(base.GetControlData());
            controls.Add(instance);
            return controls;
        }
    }
}

using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class SurfaceMasterContolDrawData : ControlDrawData
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
    public class SurfaceMasterDrawData : MasterNodeDrawData
    {
        protected override IEnumerable<GraphElementData> GetControlData()
        {
            var instance = CreateInstance<SurfaceMasterContolDrawData>();
            instance.Initialize(node);
            var controls = new List<GraphElementData>(base.GetControlData());
            controls.Add(instance);
            return controls;
        }
    }
}

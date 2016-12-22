using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class Vector4ControlDrawData : ControlDrawData
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.Vector4Node;
            if (tNode == null)
                return;

            tNode.value = EditorGUILayout.Vector4Field("", tNode.value);
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class Vector4NodeDrawData : PropertyNodeDrawData
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<Vector4ControlDrawData>();
            instance.Initialize(node);
            return new List<GraphElementPresenter>(base.GetControlData()) { instance };
        }
    }
}

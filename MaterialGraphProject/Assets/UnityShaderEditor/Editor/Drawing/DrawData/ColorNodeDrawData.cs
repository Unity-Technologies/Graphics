using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class ColorContolDrawData : ControlDrawData
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var cNode = node as UnityEngine.MaterialGraph.ColorNode;
            if (cNode == null)
                return;

            cNode.color = EditorGUILayout.ColorField("Color", cNode.color);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class ColorNodeDrawData : MaterialNodeDrawData
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<ColorContolDrawData>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}

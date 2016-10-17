using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class Vector1ContolDrawData : ControlDrawData
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

			var tNode = node as UnityEngine.MaterialGraph.Vector1Node;
            if (tNode == null)
                return;

            tNode.exposedState = (PropertyNode.ExposedState)EditorGUILayout.EnumPopup(new GUIContent("Exposed"), tNode.exposedState);
			tNode.value = EditorGUILayout.FloatField ("Value:", tNode.value);
        }

        public override float GetHeight()
        {
            return 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class Vector1NodeDrawData : MaterialNodeDrawData
    {
        protected override IEnumerable<GraphElementData> GetControlData()
        {
			var instance = CreateInstance<Vector1ContolDrawData>();
            instance.Initialize(node);
            return new List<GraphElementData> { instance };
        }
    }
}

using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class PropertyControlDrawData : ControlDrawData
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.PropertyNode;
            if (tNode == null)
                return;

            tNode.exposedState = (PropertyNode.ExposedState)EditorGUILayout.EnumPopup(new GUIContent("Exposed"), tNode.exposedState);
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public abstract class PropertyNodeDrawData : MaterialNodeDrawData
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<PropertyControlDrawData>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}

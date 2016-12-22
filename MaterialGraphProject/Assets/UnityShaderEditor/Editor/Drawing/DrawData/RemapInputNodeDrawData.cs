using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class RemapInputControlDrawData : ControlDrawData
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var remapNode = node as MasterRemapInputNode;
            if (remapNode == null)
                return;

            if (GUILayout.Button("Add Slot"))
                remapNode.AddSlot();
            if (GUILayout.Button("Remove Slot"))
                remapNode.RemoveSlot();
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2 + 3 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class RemapInputNodeDrawData : MaterialNodeDrawData
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<RemapInputControlDrawData>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}

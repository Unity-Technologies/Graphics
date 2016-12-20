using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class RemapMasterControlDrawData : ControlDrawData
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var remapNode = node as RemapMasterNode;
            if (remapNode == null)
                return;

            remapNode.remapAsset = (MaterialRemapAsset)EditorGUILayout.MiniThumbnailObjectField(
                new GUIContent("Remap Asset"), 
                remapNode.remapAsset, 
                typeof(MaterialRemapAsset), null);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class RemapMasterNodeDrawData : MasterNodeDrawData
    {
        protected override IEnumerable<GraphElementData> GetControlData()
        {
            var instance = CreateInstance<RemapMasterControlDrawData>();
            instance.Initialize(node);
            var controls = new List<GraphElementData>(base.GetControlData());
            controls.Add(instance);
            return controls;
        }
    }
}

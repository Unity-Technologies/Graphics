using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
	class SubgraphIONodeControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

			var ioNode = node as AbstractSubGraphIONode;
			if (ioNode == null)
                return;

            if (GUILayout.Button("Add Slot"))
				ioNode.AddSlot();
            if (GUILayout.Button("Remove Slot"))
				ioNode.RemoveSlot();
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2 + 3 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
	public class SubgraphIONodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
			var instance = CreateInstance<SubgraphIONodeControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}

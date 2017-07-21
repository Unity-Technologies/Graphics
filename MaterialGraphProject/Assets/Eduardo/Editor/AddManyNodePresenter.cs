using System;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
  /*  [Serializable]
    class AddManyContolPresenter : GraphControlPresenter
    {

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var addNode = node as UnityEngine.MaterialGraph.AddManyNode;
            if (addNode == null)
                return;

            if (GUILayout.Button("Add Input"))
            {
                addNode.AddInputSlot();
                addNode.OnModified();
            }
            if (GUILayout.Button("Remove Input"))
            {
                addNode.RemoveInputSlot();
                addNode.OnModified();
            }
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2 + 3 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class AddManyNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<AddManyContolPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }*/
}

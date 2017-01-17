using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    class MasterNodeControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var masterNode = node as AbstractMasterNode;
            if (masterNode == null || !masterNode.canBeActiveMaster)
                return;

            using (new EditorGUI.DisabledScope(masterNode.isActiveMaster))
                masterNode.isActiveMaster |= GUILayout.Button("Set active");
        }

        public override float GetHeight()
        {
            var masterNode = node as AbstractMasterNode;
            if (masterNode == null || !masterNode.canBeActiveMaster)
                return 0f;
            return EditorGUIUtility.singleLineHeight + 3 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class MasterNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<MasterNodeControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}

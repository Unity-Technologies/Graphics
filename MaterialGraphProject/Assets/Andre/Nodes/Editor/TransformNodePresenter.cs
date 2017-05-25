using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    class TransformControlPresenter : GraphControlPresenter
    {

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as TransformNode;
            if (tNode == null)
                return;

			//tNode.floatType = (FloatPropertyChunk.FloatType)EditorGUILayout.EnumPopup ("Float", tNode.floatType);

        }

        public override float GetHeight()
        {
			return (EditorGUIUtility.singleLineHeight + 16 * EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class TransformNodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<TransformControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter>(base.GetControlData()) { instance };
        }
    }
}

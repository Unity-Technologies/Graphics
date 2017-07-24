using System;
using System.Collections.Generic;
using UIElements.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class ColorContolPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var cNode = node as UnityEngine.MaterialGraph.ColorNode;
            if (cNode == null)
                return;

			cNode.exposedState = (PropertyNode.ExposedState)EditorGUILayout.EnumPopup(new GUIContent("Exposed"), cNode.exposedState);
			cNode.color = EditorGUILayout.ColorField(new GUIContent ("Color"), cNode.color, true, true, cNode.HDR, new ColorPickerHDRConfig(0f, 8f, 0.125f, 3f));
			cNode.HDR = EditorGUILayout.Toggle("HDR", cNode.HDR);
        }

        public override float GetHeight()
        {
			return EditorGUIUtility.singleLineHeight + 18 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class ColorNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<ColorContolPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}

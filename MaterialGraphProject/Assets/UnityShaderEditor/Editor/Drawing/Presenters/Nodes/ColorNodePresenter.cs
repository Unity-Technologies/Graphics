using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
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

            cNode.color = EditorGUILayout.ColorField(new GUIContent("Color"), cNode.color, true, true, cNode.HDR, new ColorPickerHDRConfig(0f, 8f, 0.125f, 3f));
            cNode.HDR = EditorGUILayout.Toggle("HDR", cNode.HDR);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + 18 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    public class ColorNodeView : MaterialNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<ColorContolPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }
}

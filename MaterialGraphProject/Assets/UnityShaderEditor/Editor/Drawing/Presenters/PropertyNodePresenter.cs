using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class PropertyControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as PropertyNode;
            if (tNode == null)
                return;

            var graph = node.owner as AbstractMaterialGraph;

            var currentGUID = tNode.propertyGuid;
            var properties = graph.properties.ToList();
            var propertiesGUID = properties.Select(x => x.guid).ToList();
            var currentSelectedIndex = propertiesGUID.IndexOf(currentGUID);

            var newIndex = EditorGUILayout.Popup("Property", currentSelectedIndex, properties.Select(x => x.displayName).ToArray());

            if (newIndex != currentSelectedIndex)
                tNode.propertyGuid = propertiesGUID[newIndex];
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    public class PropertyNodeView : MaterialNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<PropertyControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }
}

using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(Vector1Node))]
    public class Vector1NodeUI : PropertyNodeUI
    {
        public override float GetNodeUiHeight(float width)
        {
            return base.GetNodeUiHeight(width) + EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType Render(Rect area)
        {
            var localNode = node as Vector1Node;
            if (localNode == null)
                return base.Render(area);

            EditorGUI.BeginChangeCheck();
            localNode.value = EditorGUI.FloatField(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight), "Value", localNode.value);
            
            var toReturn = GUIModificationType.None;

            if (EditorGUI.EndChangeCheck())
            {
                //TODO:tidy this shit.
                //EditorUtility.SetDirty(materialGraphOwner.owner);
                toReturn |= GUIModificationType.Repaint;
            }

            area.y += EditorGUIUtility.singleLineHeight;
            area.height -= EditorGUIUtility.singleLineHeight;
            toReturn |= base.Render(area);
            return toReturn;
        }
    }
}

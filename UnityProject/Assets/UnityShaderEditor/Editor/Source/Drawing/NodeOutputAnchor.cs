using System;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class NodeOutputAnchor : NodeAnchor
    {
        public NodeOutputAnchor(Vector3 position, Type type, Slot slot, MaterialGraphDataSource data)
            : base(position, type, slot, data)
        {
            m_Direction = Direction.eOutput;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            var anchorColor = Color.yellow;
            anchorColor.a = 0.7f;
            base.Render(parentRect, canvas);
            EditorGUI.DrawRect(new Rect(translation.x, translation.y, scale.x, scale.y), anchorColor);
            Vector2 sizeOfText = GUIStyle.none.CalcSize(new GUIContent(m_Type.Name));

            Rect labelRect = new Rect(translation.x - sizeOfText.x - 4.0f, translation.y, sizeOfText.x + 4.0f, sizeOfText.y + 4.0f);
            GUI.Label(labelRect, m_Slot.name);
        }

    };
}

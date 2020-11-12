using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LightAnchor))]
    public class LightAnchorEditor : Editor
    {
        const float k_ArcRadius = 5;
        const float k_AxisLength = 10;

        float m_Yaw;
        float m_Pitch;
        float m_Roll;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("test");
            if (targets != null)
            {
                GameObject firstGO = (targets[0] as MonoBehaviour).gameObject;
                float curY = firstGO.transform.position.y;

                EditorGUI.BeginChangeCheck();
                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                rect.y += 0f;
                rect = EditorGUI.IndentedRect(rect);
                curY = EditorGUI.FloatField(rect, curY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets.Select(t => (t as MonoBehaviour).transform).ToArray(), "Reset Position");
                    foreach (UnityEngine.Object curTarget in targets)
                    {
                        Transform go = (curTarget as MonoBehaviour).transform;
                        //Undo.RecordObjects(new UnityEngine.Object[] { go.transform }, "Reset Position");
                        go.position = new Vector3(go.position.x, curY, go.position.z);
                    }
                }
            }
        }
    }
}

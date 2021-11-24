using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditorInternal;

namespace UnityEngine.Rendering.HighDefinition
{
    [CustomEditor(typeof(CapsuleOccluder))]
    public class CapsuleOccluderEditor : Editor {
        internal static void DrawWireCapsule(float radius, float height)
        {
            var offset = Mathf.Max(0.0f, 0.5f * height - radius);
            var capCenter = new Vector3(0.0f, 0.0f, offset);

            Handles.DrawWireDisc(capCenter, Vector3.forward, radius);
            Handles.DrawWireDisc(-capCenter, Vector3.forward, radius);
            Handles.DrawLine(new Vector3(-radius, 0, -offset), new Vector3(-radius, 0, offset));
            Handles.DrawLine(new Vector3( radius, 0, -offset), new Vector3( radius, 0, offset));
            Handles.DrawLine(new Vector3(0, -radius, -offset), new Vector3(0, -radius, offset));
            Handles.DrawLine(new Vector3(0,  radius, -offset), new Vector3(0,  radius, offset));
            Handles.DrawWireArc(capCenter, Vector3.right, Vector3.up, 180, radius);
            Handles.DrawWireArc(-capCenter, Vector3.right, Vector3.up, -180, radius);
            Handles.DrawWireArc(capCenter, Vector3.up, Vector3.right, -180, radius);
            Handles.DrawWireArc(-capCenter, Vector3.up, Vector3.right, 180, radius);
        }

        public void OnSceneGUI()
        {
            var t = target as CapsuleOccluder;

            Handles.matrix = t.capsuleToWorld;

            Handles.color = Color.white;
            Handles.zTest = CompareFunction.LessEqual;
            DrawWireCapsule(t.radius, t.height);
            Handles.color = Color.grey;
            Handles.zTest = CompareFunction.Greater;
            DrawWireCapsule(t.radius, t.height);

            Handles.zTest = CompareFunction.Always;
        }
    }
}

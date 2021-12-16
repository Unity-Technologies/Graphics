using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using WaterSystem.Data;

namespace WaterSystem
{
    [CustomEditor(typeof(Water))]
    public class WaterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Water w = (Water)target;

            var waterSettingsData = serializedObject.FindProperty("settingsData");
            EditorGUILayout.PropertyField(waterSettingsData, true);
            if(waterSettingsData.objectReferenceValue != null)
            {
                CreateEditor((WaterSettingsData)waterSettingsData.objectReferenceValue).OnInspectorGUI();
            }

            var waterSurfaceData = serializedObject.FindProperty("surfaceData");
            EditorGUILayout.PropertyField(waterSurfaceData, true);
            if(waterSurfaceData.objectReferenceValue != null)
            {
                CreateEditor((WaterSurfaceData)waterSurfaceData.objectReferenceValue).OnInspectorGUI();
            }

            serializedObject.ApplyModifiedProperties();

            if(GUI.changed)
            {
                w.Init();
            }
        }

        void OnSceneGUI()
        {
/*            Water w = target as Water;
            Camera cam = SceneView.currentDrawingSceneView.camera;
            var waveDebug = serializedObject.FindProperty("_debugMode");
            if (cam && waveDebug.intValue != 0)
            {
                Vector3 pos = Vector3.zero;
                float dist = 10f;
                if (waveDebug.intValue == 2)
                {
                    Plane p = new Plane(Vector3.zero, Vector3.forward, Vector3.right);
                    
                    Ray r = Camera.current.ViewportPointToRay(new Vector2(0.25f, 0.25f));
                    if (p.Raycast(r, out dist))
                    {
                        pos = Camera.current.ViewportToWorldPoint(new Vector3(0.25f, 0.25f, dist));
                    }
                }

                for (int i = w._waves.Length - 1; i >= 0; i--)
                {
                    Wave wave = w._waves[i];
                    Random.InitState(i);
                    Color c = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
                    c.a = Mathf.Clamp01(0.01f * dist - 0.1f) * 0.5f;
                    Handles.color = c;
                    pos.y = wave.amplitude;
                    DrawWaveGizmo(pos, wave.direction, wave.amplitude, wave.wavelength);
                }
            }*/
        }
        
        void DrawWaveGizmo(Vector3 pos, float angle, float size, float length)
        {
            Handles.DrawSolidDisc(pos, Vector3.up, length / 2f);
            Handles.ArrowHandleCap(0, pos, Quaternion.AngleAxis(angle, Vector3.up), -length / 1.75f, EventType.Repaint);
        }
    }
}

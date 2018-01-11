using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Object = UnityEngine.Object;

[CustomEditor(typeof(EditorPlayModeTests))]
class EditorPlayModeTests_Editor : Editor
{
    EditorPlayModeTests typedTarget;

    private void OnEnable()
    {
        typedTarget = (EditorPlayModeTests)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Object newScene = EditorGUILayout.ObjectField("Add Scene", null, typeof(Object), false);
        if (newScene != null)
        {
            string newPath = AssetDatabase.GetAssetPath(newScene);
            Debug.Log("New Path : " + newPath);
            if (newPath.EndsWith("unity"))
            {
                Debug.Log("Add the scene to the list");
                //int i = p_scenesPath.arraySize;
                //p_scenesPath.InsertArrayElementAtIndex(i);
                //p_scenesPath.GetArrayElementAtIndex(i).stringValue = newPath;
                if (typedTarget.scenesPath == null) typedTarget.scenesPath = new string[] { newPath };
                else
                {
                    Array.Resize(ref typedTarget.scenesPath, typedTarget.scenesPath.Length + 1);
                    typedTarget.scenesPath[typedTarget.scenesPath.Length - 1] = newPath;
                }

                EditorUtility.SetDirty(typedTarget);
            }
        }
    }
}

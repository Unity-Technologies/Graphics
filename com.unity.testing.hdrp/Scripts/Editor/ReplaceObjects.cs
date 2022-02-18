using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ReplaceObjects : EditorWindow
{
    public GameObject newObject;

    [MenuItem("Internal/GraphicTest Tools/Replace Objects")]
    public static void OpenReplaceObjects()
    {
        GetWindow<ReplaceObjects>();
    }

    void OnGUI()
    {
        newObject = EditorGUILayout.ObjectField("New Object", newObject, typeof(GameObject), false) as GameObject;

        if (GUILayout.Button("Replace selection with object")) ReplaceSelectionWithPrefab();
    }

    void ReplaceSelectionWithPrefab()
    {
        List<GameObject> selected = new List<GameObject>(Selection.gameObjects);

        if (selected.Count == 0 || newObject == null) return;

        for (int i = 0; i < selected.Count; ++i)
        {
            GameObject go = selected[i];
            GameObject newGo = PrefabUtility.InstantiatePrefab(newObject) as GameObject;
            newGo.transform.parent = go.transform.parent;
            newGo.transform.localPosition = go.transform.localPosition;
            newGo.transform.localRotation = go.transform.localRotation;
            newGo.transform.localScale = go.transform.localScale;
            newGo.name = go.name;

            DestroyImmediate(go);
            selected[i] = newGo;
        }

        Selection.objects = selected.ToArray() as Object[];
    }
}

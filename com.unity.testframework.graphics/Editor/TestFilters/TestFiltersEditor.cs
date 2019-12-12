using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.Graphics;
using UnityEngine.Rendering;
using System;
using System.Linq;

[CustomEditor(typeof(TestFilters))]
public class TestFiltersEditor : Editor
{
    SerializedProperty filters;

    public void OnEnable()
    {
        filters = serializedObject.FindProperty("filters");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var lastSceneName = string.Empty;
        var fieldLayoutOptions = new GUILayoutOption[]
        {
            GUILayout.MinWidth(40f),
            GUILayout.MaxWidth(180f)
        };


        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(40);

        EditorGUILayout.LabelField(new GUIContent("Scene", "The scene to apply this filter to"), fieldLayoutOptions);
        EditorGUILayout.LabelField(new GUIContent("Reason", "The reason this config is filtered"), fieldLayoutOptions);
        EditorGUILayout.LabelField(new GUIContent("Color Space", "Color space to filter, Unitialized will filter all color spaces."), fieldLayoutOptions);
        EditorGUILayout.LabelField(new GUIContent("Platform", "The build platform to filter, No Target will filter all platforms."), fieldLayoutOptions);
        EditorGUILayout.LabelField(new GUIContent("Graphics API", "The graphics api to filter, Null filters all apis."), fieldLayoutOptions);

        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < filters.arraySize; i++)
        {
            var filterElement = filters.GetArrayElementAtIndex(i);

            var scene = filterElement.FindPropertyRelative("FilteredScene");
            var colorSpace = filterElement.FindPropertyRelative("ColorSpace");
            var buildPlatform = filterElement.FindPropertyRelative("BuildPlatform");
            var graphicsType = filterElement.FindPropertyRelative("GraphicsDevice");
            var reason = filterElement.FindPropertyRelative("Reason");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("del"))
            {
                filters.DeleteArrayElementAtIndex(i);
                continue;
            }

            EditorGUILayout.PropertyField(scene, GUIContent.none, fieldLayoutOptions);
            EditorGUILayout.PropertyField(reason, GUIContent.none, fieldLayoutOptions);
            EditorGUILayout.PropertyField(colorSpace, GUIContent.none, fieldLayoutOptions);
            EditorGUILayout.PropertyField(buildPlatform, GUIContent.none, fieldLayoutOptions);
            EditorGUILayout.PropertyField(graphicsType, GUIContent.none, fieldLayoutOptions);
        
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button(new GUIContent("New Filter", "Add new filter"), EditorStyles.miniButtonMid))
        {
            filters.arraySize += 1;
            var lastFilter = filters.GetArrayElementAtIndex(filters.arraySize - 1);
            lastFilter.serializedObject.FindProperty("filters.Array.data[" + (filters.arraySize - 1) + "].FilteredScene").objectReferenceValue = null;
            lastFilter.serializedObject.FindProperty("filters.Array.data[" + (filters.arraySize - 1) + "].Reason").stringValue = "";
            lastFilter.serializedObject.FindProperty("filters.Array.data[" + (filters.arraySize - 1) + "].ColorSpace").intValue = (int)ColorSpace.Uninitialized;
            lastFilter.serializedObject.FindProperty("filters.Array.data[" + (filters.arraySize - 1) + "].BuildPlatform").intValue = (int)BuildTarget.NoTarget;
            lastFilter.serializedObject.FindProperty("filters.Array.data[" + (filters.arraySize - 1) + "].GraphicsDevice").intValue = (int)GraphicsDeviceType.Null;
        }

        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button(new GUIContent("Sort", "Sort filters by scene name.")))
        {
            var filterObjectPath = AssetDatabase.GetAssetPath(target.GetInstanceID());
            var filterObject = AssetDatabase.LoadAssetAtPath<TestFilters>(filterObjectPath);
            filterObject.SortBySceneName();
            EditorUtility.SetDirty(filterObject);
            AssetDatabase.SaveAssets();
        }
    }
}

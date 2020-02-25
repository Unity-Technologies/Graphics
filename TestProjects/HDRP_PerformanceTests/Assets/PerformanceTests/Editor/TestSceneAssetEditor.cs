using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Compilation;
using System;
using System.Linq;
using static PerformanceMetricNames;
using Object = UnityEngine.Object;

[CustomEditor(typeof(TestSceneAsset))]
class TestSceneAssetEditor : Editor
{
    ReorderableList counterSceneList;
    ReorderableList counterHDAssets;
    ReorderableList memorySceneList;
    ReorderableList memoryHDAssets;
    ReorderableList buildSceneList;
    ReorderableList buildHDAssets;

    ReorderableList hdAssetAliasesList;

    static float fieldHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    public void OnEnable()
    {
        SerializedProperty counterProperty = serializedObject.FindProperty(nameof(TestSceneAsset.counterTestSuite));
        SerializedProperty memoryProperty = serializedObject.FindProperty(nameof(TestSceneAsset.memoryTestSuite));
        SerializedProperty buildProperty = serializedObject.FindProperty(nameof(TestSceneAsset.buildTestSuite));

        InitReorderableListFromProperty(counterProperty, out counterSceneList, out counterHDAssets);
        InitReorderableListFromProperty(memoryProperty, out memorySceneList, out memoryHDAssets);
        InitReorderableListFromProperty(buildProperty, out buildSceneList, out buildHDAssets);

        void InitReorderableListFromProperty(SerializedProperty testSuite, out ReorderableList sceneList, out ReorderableList hdAssetList)
        {
            sceneList = new ReorderableList(serializedObject, testSuite.FindPropertyRelative(nameof(TestSceneAsset.TestSuiteData.scenes)));
            hdAssetList = new ReorderableList(serializedObject, testSuite.FindPropertyRelative(nameof(TestSceneAsset.TestSuiteData.hdAssets)));
            InitSceneDataReorderableList(sceneList, "Scenes");
            InitHDAssetReorderableList(hdAssetList, "HDRP Assets, keep to none for default HDRP asset");
        }

        hdAssetAliasesList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.hdAssetAliases)));
        InitHDAssetAliasesReorderableList(hdAssetAliasesList, "HDRP Asset Aliases");
    }

    void InitSceneDataReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            EditorGUI.BeginChangeCheck();
            var elem = list.serializedProperty.GetArrayElementAtIndex(index);
            var sceneName = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scene));
            var scenePath = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scenePath));
            var sceneLabels = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.sceneLabels));
            var enabled = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.enabled));
            rect.height = EditorGUIUtility.singleLineHeight;

            // Scene field
            var sceneGUID = AssetDatabase.FindAssets($"t:Scene {sceneName.stringValue}", new [] {"Assets"}).FirstOrDefault();
            var sceneAsset = String.IsNullOrEmpty(sceneGUID) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneGUID));
            sceneAsset = EditorGUI.ObjectField(rect, "Test Scene", sceneAsset, typeof(SceneAsset), false) as SceneAsset;
            sceneName.stringValue = sceneAsset?.name;
            scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            sceneLabels.stringValue = GetLabelForAsset(sceneAsset);

            // Enabled field
            rect.y += fieldHeight;
            EditorGUI.PropertyField(rect, enabled);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        };

        list.elementHeight = fieldHeight * 2;

        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
    }

    void InitHDAssetReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            rect.height = EditorGUIUtility.singleLineHeight;
            var elem = list.serializedProperty.GetArrayElementAtIndex(index);
            var hdAsset = elem.FindPropertyRelative(nameof(TestSceneAsset.HDAssetData.asset));
            var assetLabels = elem.FindPropertyRelative(nameof(TestSceneAsset.HDAssetData.assetLabels));
            var alias = elem.FindPropertyRelative(nameof(TestSceneAsset.HDAssetData.alias));

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(rect, hdAsset, new GUIContent("HDRP Asset"));
            assetLabels.stringValue = GetLabelForAsset(hdAsset.objectReferenceValue);
            alias.stringValue = PerformanceTestUtils.testScenesAsset.GetHDAssetAlias(hdAsset.objectReferenceValue as HDRenderPipelineAsset);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        };
        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
    }

    void InitHDAssetAliasesReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            rect.height = EditorGUIUtility.singleLineHeight;
            var elem = list.serializedProperty.GetArrayElementAtIndex(index);
            var hdAsset = elem.FindPropertyRelative(nameof(TestSceneAsset.HDAssetData.asset));
            var assetLabels = elem.FindPropertyRelative(nameof(TestSceneAsset.HDAssetData.assetLabels));
            var alias = elem.FindPropertyRelative(nameof(TestSceneAsset.HDAssetData.alias));

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(rect, hdAsset, new GUIContent("HDRP Asset"));
            rect.y += fieldHeight;
            EditorGUI.PropertyField(rect, alias, new GUIContent("Alias"));
            assetLabels.stringValue = GetLabelForAsset(hdAsset.objectReferenceValue);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        };
        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
        list.elementHeight = fieldHeight * 2;
    }

    string GetLabelForAsset(Object asset)
    {
        if (asset == null)
            return kDefault;

        var labels = AssetDatabase.GetLabels(asset);
        if (labels.Length > 0)
            return String.Join("_", labels);
        else
            return kDefault;
    }

    void DefaultListAdd(ReorderableList list)
    {
        ReorderableList.defaultBehaviours.DoAddButton(list);

        // Enable the scene by default
        var element = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
        var enable = element.FindPropertyRelative(nameof(TestSceneAsset.SceneData.enabled));
        if (enable != null)
            enable.boolValue = true;

        serializedObject.ApplyModifiedProperties();
    }

    void DefaultListDelete(ReorderableList list)
    {
        list.serializedProperty.DeleteArrayElementAtIndex(list.index);
        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        if (Resources.Load(PerformanceTestUtils.testSceneResourcePath) == null)
            EditorGUILayout.HelpBox($"Test Scene Asset have been moved from it's expected location, please move it back to Resources/{PerformanceTestUtils.testSceneResourcePath}", MessageType.Error);

        EditorGUIUtility.labelWidth = 100;

        DrawTestBlock(counterSceneList, counterHDAssets, "Performance Counters Tests");
        DrawTestBlock(memorySceneList, memoryHDAssets, "Memory Tests");
        DrawTestBlock(buildSceneList, buildHDAssets, "Build Time Tests");

        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh Test Runner List (can take up to ~20s)"))
            CompilationPipeline.RequestScriptCompilation();
        
        EditorGUILayout.Space();

        DrawHDAssetAliasList();
    }

    GUIStyle windowStyle => new GUIStyle("Window"){
        fontStyle = FontStyle.Bold,
        fontSize = 15,
        margin = new RectOffset(0, 0, 20, 10)
    };

    void DrawTestBlock(ReorderableList sceneList, ReorderableList hdrpAssetList, string title)
    {
        GUILayout.BeginHorizontal(title, windowStyle);
        {
            GUILayout.BeginVertical();
            EditorGUILayout.Space();
            sceneList.DoLayoutList();
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            EditorGUILayout.Space();
            hdrpAssetList.DoLayoutList();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }

    void DrawHDAssetAliasList()
    {
        GUILayout.BeginHorizontal("HDAsset Aliases (Used in the performance Database)", windowStyle);
        {
            GUILayout.BeginVertical();
            hdAssetAliasesList.DoLayoutList();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }
}

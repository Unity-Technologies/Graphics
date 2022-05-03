using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEditor.Compilation;
using System;
using System.Linq;
using static PerformanceMetricNames;
using Object = UnityEngine.Object;
using System.IO;

[CustomEditor(typeof(TestSceneAsset))]
class TestSceneAssetEditor : Editor
{
    ReorderableList m_CounterSceneList;
    ReorderableList m_CounterSRPAssets;
    ReorderableList m_MemorySceneList;
    ReorderableList m_MemorySRPAssets;
    ReorderableList m_BuildSceneList;
    ReorderableList m_BuildSRPAssets;

    ReorderableList srpAssetAliasesList;

    static float fieldHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    static class Styles
    {
        public static readonly string countersText = "Performance Counters Tests";
        public static readonly string memoryText = "Memory Tests";
        public static readonly string buildTimeText = "Build Time Tests";
        public static readonly string refreshTestRunner = "Refresh Test Runner List (can take up to ~20s)";
    }

    public void OnEnable()
    {
        SerializedProperty counterProperty = serializedObject.FindProperty(nameof(TestSceneAsset.counterTestSuite));
        SerializedProperty memoryProperty = serializedObject.FindProperty(nameof(TestSceneAsset.memoryTestSuite));
        SerializedProperty buildProperty = serializedObject.FindProperty(nameof(TestSceneAsset.buildTestSuite));

        InitReorderableListFromProperty(counterProperty, out m_CounterSceneList, out m_CounterSRPAssets);
        InitReorderableListFromProperty(memoryProperty, out m_MemorySceneList, out m_MemorySRPAssets);
        InitReorderableListFromProperty(buildProperty, out m_BuildSceneList, out m_BuildSRPAssets);

        void InitReorderableListFromProperty(SerializedProperty testSuite, out ReorderableList sceneList, out ReorderableList srpAssetList)
        {
            sceneList = new ReorderableList(serializedObject, testSuite.FindPropertyRelative(nameof(TestSceneAsset.TestSuiteData.scenes)));
            srpAssetList = new ReorderableList(serializedObject, testSuite.FindPropertyRelative(nameof(TestSceneAsset.TestSuiteData.srpAssets)));
            InitSceneDataReorderableList(sceneList, "Scenes");
            InitSRPAssetReorderableList(srpAssetList, "SRP Assets");
        }

        srpAssetAliasesList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.srpAssetAliases)));
        InitSRPAssetAliasesReorderableList(srpAssetAliasesList, "SRP Asset Aliases");
    }

    void InitSceneDataReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            var elem = list.serializedProperty.GetArrayElementAtIndex(index);
            var sceneName = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scene));
            var scenePath = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scenePath));
            var sceneLabels = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.sceneLabels));
            var enabled = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.enabled));
            rect.height = EditorGUIUtility.singleLineHeight;

            // Scene field
            var sceneGUID = AssetDatabase.FindAssets($"t:Scene {sceneName.stringValue}", new [] {"Assets", "Packages"})
                .FirstOrDefault(guid => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == sceneName.stringValue);
            SceneAsset sceneAsset = null;
            if (!String.IsNullOrEmpty(sceneGUID))
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGUID);

                // Only if the scene we found is the correct one, we assign it correctly
                if (Path.GetFileNameWithoutExtension(path) == sceneName.stringValue)
                {
                    sceneAsset = String.IsNullOrEmpty(sceneGUID) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneGUID));
                }
            }

            EditorGUI.BeginChangeCheck();
            sceneAsset = EditorGUI.ObjectField(rect, "Test Scene", sceneAsset, typeof(SceneAsset), false) as SceneAsset;
            if (EditorGUI.EndChangeCheck())
            {
                sceneName.stringValue = sceneAsset != null && !sceneAsset.Equals(null) ? sceneAsset.name : null;
                scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
                sceneLabels.stringValue = GetLabelForAsset(sceneAsset);
            }

            // Enabled field
            rect.y += fieldHeight;
            EditorGUI.PropertyField(rect, enabled);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        };

        list.elementHeight = fieldHeight * 2;

        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
    }

    void InitSRPAssetReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            serializedObject.Update();
            rect.height = EditorGUIUtility.singleLineHeight;
            var elem = list.serializedProperty.GetArrayElementAtIndex(index);
            var srpAsset = elem.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.asset));
            var assetLabels = elem.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.assetLabels));
            var alias = elem.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.alias));

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(rect, srpAsset, new GUIContent("SRP Asset"));
            assetLabels.stringValue = GetLabelForAsset(srpAsset.objectReferenceValue);
            alias.stringValue = PerformanceTestUtils.testScenesAsset.GetSRPAssetAlias(srpAsset.objectReferenceValue as RenderPipelineAsset);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        };
        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
    }

    void InitSRPAssetAliasesReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            serializedObject.Update();
            rect.height = EditorGUIUtility.singleLineHeight;
            var elem = list.serializedProperty.GetArrayElementAtIndex(index);
            var srpAsset = elem.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.asset));
            var assetLabels = elem.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.assetLabels));
            var alias = elem.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.alias));

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(rect, srpAsset, new GUIContent("SRP Asset"));
            rect.y += fieldHeight;
            EditorGUI.PropertyField(rect, alias, new GUIContent("Alias"));
            assetLabels.stringValue = GetLabelForAsset(srpAsset.objectReferenceValue);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        };
        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
        list.elementHeight = fieldHeight * 2;
    }

    static string GetLabelForAsset(Object asset)
    {
        if (asset == null)
            return k_Default;

        var labels = AssetDatabase.GetLabels(asset);
        if (labels.Length > 0)
            return String.Join("_", labels);
        else
            return k_Default;
    }

    void DefaultListAdd(ReorderableList list)
    {
        serializedObject.Update();
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
        serializedObject.Update();
        list.serializedProperty.DeleteArrayElementAtIndex(list.index);
        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        if (PerformanceTestSettings.GetTestSceneDescriptionAsset() == null)
            EditorGUILayout.HelpBox($"Test Scene Asset is null, please set it in Project Settings / Performance Tests", MessageType.Error);

        EditorGUIUtility.labelWidth = 100;

        DrawTestBlock(m_CounterSceneList, m_CounterSRPAssets, Styles.countersText);
        DrawTestBlock(m_MemorySceneList, m_MemorySRPAssets, Styles.memoryText);
        DrawTestBlock(m_BuildSceneList, m_BuildSRPAssets, Styles.buildTimeText);

        EditorGUILayout.Space();

        if (GUILayout.Button(Styles.refreshTestRunner))
            CompilationPipeline.RequestScriptCompilation();

        EditorGUILayout.Space();

        DrawSRPAssetAliasList();
    }

    GUIStyle windowStyle => new GUIStyle("Window"){
        fontStyle = FontStyle.Bold,
        fontSize = 15,
        margin = new RectOffset(0, 0, 20, 10)
    };

    void DrawTestBlock(ReorderableList sceneList, ReorderableList srpAssetList, string title)
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
            srpAssetList.DoLayoutList();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }

    void DrawSRPAssetAliasList()
    {
        GUILayout.BeginHorizontal("SRP Asset Aliases (Used in the performance Database)", windowStyle);
        {
            GUILayout.BeginVertical();
            srpAssetAliasesList.DoLayoutList();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }
}

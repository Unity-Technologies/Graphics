using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Compilation;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using static UnityEngine.TestTools.Graphics.Performance.PerformanceMetricNames;

namespace UnityEngine.TestTools.Graphics.Performance.Editor
{
    [CustomEditor(typeof(TestSceneAsset))]
    class TestSceneAssetEditor : UnityEditor.Editor
    {
        static class Styles
        {
            public const string countersText = "Performance Counters Tests";
            public const string memoryText = "Memory Tests";
            public const string buildTimeText = "Build Time Tests";
            public const string refreshTestRunner = "Refresh Test Runner List (can take up to ~20s)";
            public const string replaceBuildSceneList = "Update build scene list (Replace with Scenes from this asset)";
            public const string additionalInfosText = "Additional Informations";
            public const string scenesText = "Scenes";
            public const string srpAssetsText = "SRP Assets";
            public const string srpAssetAliaseText = "SRP Asset Aliases (Used in the performance Database)";
            public const string additionalLoadableScenesText =
                "Additional Loadable Scene (Additional scenes to include in build)";
        }

        SerializedProperty counterSuiteProperty;
        SerializedProperty memorySuiteProperty;
        SerializedProperty buildSuiteProperty;
        SerializedProperty srpAssetAliasProperty;
        SerializedProperty additionalScenesProperty;

        public override VisualElement CreateInspectorGUI()
        {
            counterSuiteProperty = serializedObject.FindProperty(nameof(TestSceneAsset.counterTestSuite));
            memorySuiteProperty = serializedObject.FindProperty(nameof(TestSceneAsset.memoryTestSuite));
            buildSuiteProperty = serializedObject.FindProperty(nameof(TestSceneAsset.buildTestSuite));
            srpAssetAliasProperty = serializedObject.FindProperty(nameof(TestSceneAsset.srpAssetAliases));
            additionalScenesProperty = serializedObject.FindProperty(nameof(TestSceneAsset.additionalLoadableScenes));

            VisualElement root = new();
            root.Add(new Boxed(Styles.countersText, counterSuiteProperty));
            root.Add(new Boxed(Styles.memoryText, memorySuiteProperty));
            root.Add(new Boxed(Styles.buildTimeText, buildSuiteProperty));
            root.Add(new Button(RefreshTestRunner) { text = Styles.refreshTestRunner });
            root.Add(new Button(ReplaceBuildSceneList) { text = Styles.replaceBuildSceneList });
            root.Add(new Boxed(Styles.additionalInfosText,
                new List<SerializedProperty>() { additionalScenesProperty, srpAssetAliasProperty }));
            return root;
        }

        void RefreshTestRunner() => CompilationPipeline.RequestScriptCompilation();

        void ReplaceBuildSceneList() =>  UnityEditor.EditorBuildSettings.scenes = (target as TestSceneAsset).ConvertTestDataScenesToBuildSettings();
    }

    [CustomPropertyDrawer(typeof(TestSceneAsset.SceneData))]
    class SceneDataPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            public const string name = "scene-data";
            public const string sceneFieldLabel = "Test Scene";
        }

        SerializedProperty sceneName;
        SerializedProperty scenePath;
        SerializedProperty labels;
        SerializedProperty enabled;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            sceneName = property.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scene));
            scenePath = property.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scenePath));
            labels = property.FindPropertyRelative(nameof(TestSceneAsset.SceneData.sceneLabels));
            enabled = property.FindPropertyRelative(nameof(TestSceneAsset.SceneData.enabled));

            VisualElement root = new() { name = Styles.name };

            ObjectField sceneField = new(Styles.sceneFieldLabel);
            sceneField.objectType = typeof(SceneAsset);
            sceneField.RegisterValueChangedCallback(OnChangeSceneAsset);
            sceneField.SetValueWithoutNotify(string.IsNullOrEmpty(scenePath.stringValue)
                ? null
                : AssetDatabase.LoadMainAssetAtPath(scenePath.stringValue));
            sceneField.AddToClassList(Helper.inspectorAlignmentClass);
            root.Add(sceneField);

            root.Add(new PropertyField(enabled));
            return root;
        }

        void OnChangeSceneAsset(ChangeEvent<UnityEngine.Object> evt)
        {
            var sceneAsset = evt.newValue as SceneAsset;
            sceneName.stringValue = sceneAsset?.name;
            scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            labels.stringValue = Helper.GetLabelForAsset(sceneAsset);
            sceneName.serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(TestSceneAsset.SRPAssetData))]
    class SRPAssetDataPropertyDrawer : PropertyDrawer
    {
        protected static class Styles
        {
            public const string name = "srp-asset-data";
            public const string assetFieldLabel = "SRP Asset";
            public const string empty = "Empty";
            public const string alias = "Alias";
        }

        SerializedProperty srpAsset;
        SerializedProperty labels;
        protected SerializedProperty alias;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            srpAsset = property.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.asset));
            labels = property.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.assetLabels));
            alias = property.FindPropertyRelative(nameof(TestSceneAsset.SRPAssetData.alias));

            VisualElement root = new() { name = Styles.name };
            PropertyField assetField = new(srpAsset, Styles.assetFieldLabel);
            assetField.RegisterValueChangeCallback(OnChangeSRPAsset);
            root.Add(assetField);
            return root;
        }

        void OnChangeSRPAsset(SerializedPropertyChangeEvent evt)
        {
            var srpAsset = evt.changedProperty.objectReferenceValue as RenderPipelineAsset;
            labels.stringValue = Helper.GetLabelForAsset(srpAsset);
            AutoUpdateAlias(srpAsset);
            labels.serializedObject.ApplyModifiedProperties();
        }

        protected virtual void AutoUpdateAlias(RenderPipelineAsset srpAsset)
            => alias.stringValue = srpAsset == null
                ? Styles.empty
                : PerformanceTestUtils.testScenesAsset?.GetSRPAssetAlias(srpAsset);
    }

    [CustomPropertyDrawer(typeof(TestSceneAsset.SRPAssetDataAliasByHand))]
    class SRPAssetDataAliasByHandPropertyDrawer : SRPAssetDataPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = base.CreatePropertyGUI(property);
            PropertyField aliasField = new(alias, Styles.alias);
            root.Add(aliasField);
            return root;
        }

        protected override void AutoUpdateAlias(RenderPipelineAsset srpAsset)
        {
        }
    }

    static class Helper
    {
        public const string inspectorAlignmentClass = "unity-base-field__aligned";
        public const string boxFouldoutClass = "box";

        public static string GetLabelForAsset(Object asset)
        {
            if (asset == null)
                return k_Default;

            var labels = AssetDatabase.GetLabels(asset);
            if (labels.Length > 0)
                return string.Join("_", labels);
            else
                return k_Default;
        }
    }

    class Boxed : Foldout
    {
        const string k_Stylesheet = "Packages/com.unity.testing.graphics-performance/Editor/Common/Boxed.uss";

        public Boxed(string title, List<SerializedProperty> contentList) : base()
        {
            Initialize(title, contentList);
        }

        public Boxed(string title, SerializedProperty parentProperty) : base()
        {
            List<SerializedProperty> contentList = new();
            SerializedProperty iterator = parentProperty.Copy();
            SerializedProperty endIterator = iterator.GetEndProperty();
            iterator.Next(enterChildren: true);
            while (!SerializedProperty.EqualContents(iterator, endIterator))
            {
                contentList.Add(iterator.Copy());
                iterator.Next(enterChildren: false);
            }

            Initialize(title, contentList);
        }

        void Initialize(string title, List<SerializedProperty> contentList)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Stylesheet));
            text = title;
            foreach (var property in contentList)
                contentContainer.Add(new PropertyField(property));
        }
    }

    class ScenePathUpdater : AssetPostprocessor
    {
        static TestSceneAsset testSceneAsset = null;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths, bool didDomainReload)
        {
            for (int i = 0; i < movedAssets.Length; i++)
            {
                if (!movedFromAssetPaths[i].EndsWith(".unity"))
                    continue;

                string oldPath = movedFromAssetPaths[i];
                string newPath = movedAssets[i];
                testSceneAsset ??= PerformanceTestSettings.GetTestSceneDescriptionAsset();

                void UpdateScenePathInSceneList(ref List<TestSceneAsset.SceneData> sceneList)
                {
                    var index = sceneList.FindIndex(s => s.scenePath == oldPath);
                    if (index < 0)
                        return;

                    sceneList[index].scenePath = newPath;
                    var pos = newPath.LastIndexOf('/') + 1;
                    sceneList[index].scene = newPath.Substring(pos, newPath.Length - pos - ".unity".Length);
                }

                UpdateScenePathInSceneList(ref testSceneAsset.counterTestSuite.scenes);
                UpdateScenePathInSceneList(ref testSceneAsset.memoryTestSuite.scenes);
                UpdateScenePathInSceneList(ref testSceneAsset.buildTestSuite.scenes);
                UpdateScenePathInSceneList(ref testSceneAsset.additionalLoadableScenes);
            }
        }
    }
}

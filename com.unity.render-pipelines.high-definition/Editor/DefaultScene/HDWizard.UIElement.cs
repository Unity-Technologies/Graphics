using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDWizard : EditorWindow
    {
        #region OBJECT_SELECTOR

        //utility class to show only non scene object selection
        static class ObjectSelectorUtility
        {
            public static bool opened
                => Resources.FindObjectsOfTypeAll(typeof(UnityEditor.ObjectSelector)).Length > 0;
        }

        void CreateDefaultSceneFromPackageAnsAssignIt(bool forDXR)
        {
            string subPath = forDXR ? "/DXR/" : "/";

            if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);
            if (forDXR && !AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath + subPath))
                AssetDatabase.CreateFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath, "DXR");

            var hdrpAssetEditorResources = HDRenderPipeline.defaultAsset.renderPipelineEditorResources;
            
            GameObject originalDefaultSceneAsset = forDXR ? hdrpAssetEditorResources.defaultDXRScene : hdrpAssetEditorResources.defaultScene;
            string defaultScenePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + subPath + originalDefaultSceneAsset.name + ".prefab";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(originalDefaultSceneAsset), defaultScenePath);

            VolumeProfile originalDefaultSkyAndFogProfileAsset = forDXR ? hdrpAssetEditorResources.defaultDXRSkyAndFogProfile : hdrpAssetEditorResources.defaultSkyAndFogProfile;
            string defaultSkyAndFogProfilePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + subPath + originalDefaultSkyAndFogProfileAsset.name + ".asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(originalDefaultSkyAndFogProfileAsset), defaultSkyAndFogProfilePath);

            VolumeProfile originalDefaultPostProcessingProfileAsset = forDXR ? hdrpAssetEditorResources.defaultDXRPostProcessingProfile : hdrpAssetEditorResources.defaultPostProcessingProfile;
            string defaultPostProcessingProfilePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + subPath + originalDefaultPostProcessingProfileAsset.name + ".asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(originalDefaultPostProcessingProfileAsset), defaultPostProcessingProfilePath);

            GameObject defaultScene = AssetDatabase.LoadAssetAtPath<GameObject>(defaultScenePath);
            VolumeProfile defaultSkyAndFogProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultSkyAndFogProfilePath);
            VolumeProfile defaultPostProcessingProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultPostProcessingProfilePath);

            foreach (var volume in defaultScene.GetComponentsInChildren<Volume>())
            {
                if (volume.sharedProfile.name.StartsWith(originalDefaultSkyAndFogProfileAsset.name))
                    volume.sharedProfile = defaultSkyAndFogProfile;
                else if (volume.sharedProfile.name.StartsWith(originalDefaultPostProcessingProfileAsset.name))
                    volume.sharedProfile = defaultPostProcessingProfile;
            }

            if (forDXR)
                HDProjectSettings.defaultDXRScenePrefab = defaultScene;
            else
                HDProjectSettings.defaultScenePrefab = defaultScene;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void CreateOrLoad<T>(Action onCancel, Action<T> onObjectChanged)
            where T : ScriptableObject
        {
            string title;
            string content;
            UnityEngine.Object target;
            if (typeof(T) == typeof(HDRenderPipelineAsset))
            {
                title = Style.hdrpAssetDisplayDialogTitle;
                content = Style.hdrpAssetDisplayDialogContent;
                target = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            }
            else
                throw new ArgumentException("Unknown type used");

            switch (EditorUtility.DisplayDialogComplex(title, content, Style.displayDialogCreate, "Cancel", Style.displayDialogLoad))
            {
                case 0: //create
                    if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                        AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);
                    var asset = ScriptableObject.CreateInstance<T>();
                    asset.name = typeof(T).Name;
                    AssetDatabase.CreateAsset(asset, "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + asset.name + ".asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    if (typeof(T) == typeof(HDRenderPipelineAsset))
                        GraphicsSettings.renderPipelineAsset = asset as HDRenderPipelineAsset;
                    break;
                case 1: //cancel
                    onCancel?.Invoke();
                    break;
                case 2: //Load
                    ObjectSelector.get.Show(
                        obj: target,
                        requiredType: typeof(T),
                        objectBeingEdited: null,
                        allowSceneObjects: false,
                        allowedInstanceIDs: null,
                        onObjectSelectorClosed: null,
                        onObjectSelectedUpdated: o => onObjectChanged?.Invoke((T)o));
                    break;
                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        void CreateOrLoadDefaultScene(Action onCancel, Action<GameObject> onObjectChanged, bool forDXR)
        {
            switch (EditorUtility.DisplayDialogComplex(
                forDXR ? Style.dxrScenePrefabTitle : Style.scenePrefabTitle,
                forDXR ? Style.dxrScenePrefabContent : Style.scenePrefabContent,
                Style.displayDialogCreate,
                Style.displayDialogCancel,
                Style.displayDialogLoad))
            {
                case 0: //create
                    CreateDefaultSceneFromPackageAnsAssignIt(forDXR);
                    break;
                case 1: //cancel
                    onCancel?.Invoke();
                    break;
                case 2: //Load
                    ObjectSelector.get.Show(
                        obj: forDXR ? HDProjectSettings.defaultDXRScenePrefab : HDProjectSettings.defaultScenePrefab,
                        requiredType: typeof(GameObject),
                        objectBeingEdited: null,
                        allowSceneObjects: false,
                        allowedInstanceIDs: null,
                        onObjectSelectorClosed: null,
                        onObjectSelectedUpdated: o => onObjectChanged?.Invoke((GameObject)o));
                    break;
                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        void Repopulate()
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

            var hdrpAsset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            hdrpAsset.name = "HDRenderPipelineAsset";

            AssetDatabase.CreateAsset(hdrpAsset, "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAsset.name + ".asset");

            GraphicsSettings.renderPipelineAsset = hdrpAsset;
            if (!IsHdrpAssetRuntimeResourcesCorrect())
                FixHdrpAssetRuntimeResources(true);
            if (!IsHdrpAssetEditorResourcesCorrect())
                FixHdrpAssetEditorResources(true);

            CreateDefaultSceneFromPackageAnsAssignIt(forDXR: false);
        }

        #endregion

        #region UIELEMENT

        class ToolbarRadio : UIElements.Toolbar, INotifyValueChanged<int>
        {
            public new class UxmlFactory : UxmlFactory<ToolbarRadio, UxmlTraits> { }
            public new class UxmlTraits : Button.UxmlTraits { }

            List<ToolbarToggle> radios = new List<ToolbarToggle>();

            public new static readonly string ussClassName = "unity-toolbar-radio";

            public int radioLength => radios.Count;

            int m_Value;
            public int value
            {
                get => m_Value;
                set
                {
                    if (value == m_Value)
                        return;

                    if (panel != null)
                    {
                        using (ChangeEvent<int> evt = ChangeEvent<int>.GetPooled(m_Value, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }

            public ToolbarRadio()
            {
                AddToClassList(ussClassName);
            }

            void AddRadio(string label = null, string tooltip = null)
            {
                var toggle = new ToolbarToggle()
                {
                    text = label,
                    tooltip = tooltip
                };
                toggle.RegisterValueChangedCallback(InnerValueChanged(radioLength));
                toggle.SetValueWithoutNotify(radioLength == 0);
                if (radioLength == 0)
                    toggle.AddToClassList("SelectedRadio");
                radios.Add(toggle);
                Add(toggle);
                toggle.AddToClassList("Radio");
            }

            public void AddRadios((string label, string tooltip)[] tabs)
            {
                if (tabs.Length == 0)
                    return;

                if (radioLength > 0)
                {
                    radios[radioLength - 1].RemoveFromClassList("LastRadio");
                }
                foreach (var (label, tooltip) in tabs)
                    AddRadio(label, tooltip);

                radios[radioLength - 1].AddToClassList("LastRadio");
            }

            EventCallback<ChangeEvent<bool>> InnerValueChanged(int radioIndex)
            {
                return (ChangeEvent<bool> evt) =>
                {
                    if (radioIndex == m_Value)
                    {
                        if (!evt.newValue)
                        {
                            //cannot deselect in a radio
                            radios[m_Value].RemoveFromClassList("SelectedRadio");
                            radios[radioIndex].AddToClassList("SelectedRadio");
                            radios[radioIndex].SetValueWithoutNotify(true);
                        }
                        else
                            value = -1;
                    }
                    else
                        value = radioIndex;
                };
            }

            public void SetValueWithoutNotify(int newValue)
            {
                if (m_Value != newValue)
                {
                    if (newValue < 0 || newValue >= radioLength)
                        throw new System.IndexOutOfRangeException();

                    if (m_Value != newValue)
                    {
                        radios[m_Value].RemoveFromClassList("SelectedRadio");
                        radios[newValue].AddToClassList("SelectedRadio");
                        radios[newValue].SetValueWithoutNotify(true);
                        m_Value = newValue;
                    }
                }
            }
        }

        abstract class VisualElementUpdatable : VisualElement
        {
            protected Func<bool> m_Tester;
            public bool currentStatus { get; private set; }

            protected VisualElementUpdatable(Func<bool> tester)
                => m_Tester = tester;

            public virtual void CheckUpdate()
            {
                bool wellConfigured = m_Tester();
                if (wellConfigured ^ currentStatus)
                {
                    UpdateDisplay(wellConfigured);
                    currentStatus = wellConfigured;
                }
            }

            protected void Init() => UpdateDisplay(currentStatus);

            protected abstract void UpdateDisplay(bool statusOK);
        }

        class HiddableUpdatableContainer : VisualElementUpdatable
        {
            public HiddableUpdatableContainer(Func<bool> tester) : base(tester) { }

            public override void CheckUpdate()
            {
                base.CheckUpdate();
                if (currentStatus)
                {
                    foreach (VisualElementUpdatable updatable in Children())
                        updatable.CheckUpdate();
                }
            }

            new public void Init() => base.Init();

            protected override void UpdateDisplay(bool visible)
                => style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        class ConfigInfoLine : VisualElementUpdatable
        {
            static class Style
            {
                const string k_IconFolder = @"Packages/com.unity.render-pipelines.high-definition/Editor/DefaultScene/WizardResources/";
                public static readonly Texture ok = CoreEditorUtils.LoadIcon(k_IconFolder, "OK");
                public static readonly Texture error = CoreEditorUtils.LoadIcon(k_IconFolder, "Error");

                public const int k_IndentStepSize = 15;
            }

            public ConfigInfoLine(string label, string error, string resolverButtonLabel, Func<bool> tester, Action resolver, int indent = 0)
                : base(tester)
            {
                var testLabel = new Label(label)
                {
                    name = "TestLabel"
                };
                var statusOK = new Image()
                {
                    image = Style.ok,
                    name = "StatusOK"
                };
                var statusKO = new Image()
                {
                    image = Style.error,
                    name = "StatusError"
                };
                var fixer = new Button(resolver)
                {
                    text = resolverButtonLabel,
                    name = "Resolver"
                };
                var testRow = new VisualElement() { name = "TestRow" };
                testRow.Add(testLabel);
                testRow.Add(statusOK);
                testRow.Add(statusKO);
                testRow.Add(fixer);
                
                Add(testRow);
                Add(new HelpBox(HelpBox.Kind.Error, error));

                testLabel.style.paddingLeft = style.paddingLeft.value.value + indent * Style.k_IndentStepSize;

                Init();
            }

            protected override void UpdateDisplay(bool statusOK)
            {
                if (!((hierarchy.parent as HiddableUpdatableContainer)?.currentStatus ?? true))
                {
                    this.Q(name: "StatusOK").style.display = DisplayStyle.None;
                    this.Q(name: "StatusError").style.display = DisplayStyle.None;
                    this.Q(name: "Resolver").style.display = DisplayStyle.None;
                    this.Q(name: "HelpBox").style.display = DisplayStyle.None;
                }
                else
                {
                    this.Q(name: "StatusOK").style.display = statusOK ? DisplayStyle.Flex : DisplayStyle.None;
                    this.Q(name: "StatusError").style.display = statusOK ? DisplayStyle.None : DisplayStyle.Flex;
                    this.Q(name: "Resolver").style.display = statusOK ? DisplayStyle.None : DisplayStyle.Flex;
                    this.Q(name: "HelpBox").style.display = statusOK ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        class HelpBox : VisualElement
        {
            public enum Kind
            {
                None,
                Info,
                Warning,
                Error
            }
            
            readonly Label label;
            readonly Image icon;

            public string text
            {
                get => label.text;
                set => label.text = value;
            }

            Kind m_Kind = Kind.None;
            public Kind kind
            {
                get => m_Kind;
                set
                {
                    if (m_Kind != value)
                    {
                        m_Kind = value;

                        string iconName;
                        switch (kind)
                        {
                            default:
                            case Kind.None:
                                icon.style.display = DisplayStyle.None;
                                return;
                            case Kind.Info:
                                iconName = "console.infoicon";
                                break;
                            case Kind.Warning:
                                iconName = "console.warnicon";
                                break;
                            case Kind.Error:
                                iconName = "console.erroricon";
                                break;
                        }
                        icon.image = EditorGUIUtility.IconContent(iconName).image;
                        icon.style.display = DisplayStyle.Flex;
                    }
                }
            }

            public HelpBox(Kind kind, string message)
            {
                this.label = new Label(message);
                icon = new Image();

                name = "HelpBox";
                Add(icon);
                Add(this.label);

                this.kind = kind;
            }
        }

        class FixAllButton : VisualElementUpdatable
        {
            public FixAllButton(string label, Func<bool> tester, Action resolver)
                : base(tester)
            {
                Add(new Button(resolver)
                {
                    text = label,
                    name = "FixAll"
                });

                Init();
            }

            protected override void UpdateDisplay(bool statusOK)
                => this.Q(name: "FixAll").style.display = statusOK ? DisplayStyle.None : DisplayStyle.Flex;
        }

        #endregion
    }
}

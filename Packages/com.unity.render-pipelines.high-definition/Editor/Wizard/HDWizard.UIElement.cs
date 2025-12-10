using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor.Rendering.Analytics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDWizard
    {
        void CreateHDRPAsset(Action<HDRenderPipelineAsset> onObjectChanged)
        {
            var asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            asset.name = typeof(HDRenderPipelineAsset).Name;

            string path = $"Assets/{HDProjectSettings.projectSettingsFolderPath}/{asset.name}.asset";
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            
            var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(asset, uniqueAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onObjectChanged?.Invoke(asset);
        }

        void CreateOrLoad(Action onCancel, Action<HDRenderPipelineAsset> onObjectChanged)
        {
            var hdrpAssets = AssetDatabaseHelper.FindAssets<HDRenderPipelineAsset>();
            if (hdrpAssets.Count() == 0)
                CreateHDRPAsset(onObjectChanged);

            switch (EditorUtility.DisplayDialogComplex(Style.hdrpAssetDisplayDialogTitle, Style.hdrpAssetDisplayDialogContent, Style.displayDialogCreate, "Cancel", Style.displayDialogLoad))
            {
                case 0:
                    CreateHDRPAsset(onObjectChanged);
                    break;
                case 1: //cancel
                    onCancel?.Invoke();
                    break;
                case 2: //Load
                {
                    m_Fixer.Pause();
                    ObjectSelector.Show(GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset, typeof(HDRenderPipelineAsset), null, false, null, o =>
                    {
                        m_Fixer.Unpause();
                        onObjectChanged?.Invoke(o as HDRenderPipelineAsset);
                    },
                    o => { }, false);
                    break;
                }

                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        #region UIELEMENT

        abstract class VisualElementUpdatable : VisualElement
        {
            protected Func<Result> m_Tester;
            bool m_HaveFixer;
            public Result currentStatus { get; private set; }

            protected VisualElementUpdatable(Func<Result> tester, bool haveFixer)
            {
                m_Tester = tester;
                m_HaveFixer = haveFixer;
            }

            public virtual void CheckUpdate()
            {
                var wellConfigured = m_Tester();
                if (wellConfigured != currentStatus)
                    currentStatus = wellConfigured;

                UpdateDisplay(wellConfigured, m_HaveFixer);
            }

            public void Init() => UpdateDisplay(currentStatus, m_HaveFixer);

            public abstract void UpdateDisplay(Result status, bool haveFixer);
        }

        class ConfigInfoLine : VisualElementUpdatable
        {
            static class Style
            {
                public const int k_IndentStepSize = 15;
            }

            readonly bool m_VisibleStatus;
            readonly bool m_SkipErrorIcon;
            private Image m_StatusOk;
            private Image m_StatusKO;
            private Image m_StatusPending;
            private Button m_Resolver;
            private HelpBox m_HelpBox;
            public ConfigInfoLine(Entry entry)
                : base(() => entry.check(), entry.fix != null)
            {

                m_VisibleStatus = entry.configStyle.messageType == MessageType.Error || entry.forceDisplayCheck;
                m_SkipErrorIcon = entry.skipErrorIcon;
                var testLabel = new Label(entry.configStyle.label)
                {
                    name = "TestLabel",
                    style =
                    {
                        paddingLeft = style.paddingLeft.value.value + entry.indent * Style.k_IndentStepSize
                    }
                };

                var testRow = new VisualElement()
                {
                    name = "TestRow",
                    style =
                    {
                        marginBottom = 2,
                    }
                };
                testRow.Add(testLabel);

                m_StatusOk = new Image()
                {
                    image = CoreEditorStyles.iconComplete,
                    name = "StatusOK",
                    style =
                    {
                        height = 16,
                        width = 16
                    }
                };
                m_StatusKO = new Image()
                {
                    image = CoreEditorStyles.iconFail,
                    name = "StatusError",
                    style =
                    {
                        height = 16,
                        width = 16
                    }
                };
                m_StatusPending = new Image()
                {
                    image = CoreEditorStyles.iconPending,
                    name = "StatusPending",
                    style =
                    {
                        height = 16,
                        width = 16
                    }
                };
                testRow.Add(m_StatusOk);
                testRow.Add(m_StatusKO);
                testRow.Add(m_StatusPending);

                Add(testRow);
                var kind = entry.configStyle.messageType switch
                {
                    MessageType.Error => HelpBoxMessageType.Error,
                    MessageType.Warning => HelpBoxMessageType.Warning,
                    MessageType.Info => HelpBoxMessageType.Info,
                    _ => HelpBoxMessageType.None,
                };

                string error = entry.configStyle.error;

                // If it is necessary, append tht name of the current asset.
                var hdrpAsset = HDRenderPipeline.currentAsset;
                if (entry.displayAssetName && hdrpAsset != null)
                {
                    error += " (" + hdrpAsset.name + ").";
                }

                m_HelpBox = new HelpBox(error, kind);
                m_HelpBox.Q<VisualElement>(className: "unity-help-box__top-container").style.flexGrow = 1;
                m_HelpBox.style.flexDirection = FlexDirection.Row;

                m_Resolver = new Button(() =>
                {
                    if (entry.fix != null)
                    {
                        string context = "{" + $"\"id\" : \"{entry.configStyle.label}\"" + "}";
                        GraphicsToolUsageAnalytic.ActionPerformed<HDWizard>("Fix", new string[] { context });
                        entry.fix.Invoke(false);
                    }
                })
                {
                    text = entry.configStyle.button,
                    name = "Resolver",
                    style =
                    {
                        position = Position.Relative,
                        alignContent = Align.Center,
                    }
                };

                m_HelpBox.Add(m_Resolver);
                Add(m_HelpBox);

                Init();
            }

            public override void UpdateDisplay(Result status, bool haveFixer)
            {
                m_StatusOk.style.display = DisplayStyle.None;
                m_StatusKO.style.display = DisplayStyle.None;
                m_StatusPending.style.display = DisplayStyle.None;

                if (m_VisibleStatus)
                {
                    m_StatusOk.style.display = status == Result.OK ? DisplayStyle.Flex : DisplayStyle.None;
                    m_StatusPending.style.display = status == Result.Pending ? DisplayStyle.Flex : DisplayStyle.None;
                    if (!m_SkipErrorIcon)
                        m_StatusKO.style.display = status == Result.Failed ? DisplayStyle.Flex : DisplayStyle.None;
                }

                m_Resolver.style.display = status != Result.Failed || !haveFixer ? DisplayStyle.None : DisplayStyle.Flex;
                m_HelpBox.style.display = status != Result.Failed ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        class FixAllButton : VisualElementUpdatable
        {
            public FixAllButton(string label, Func<Result> tester, Action resolver)
                : base(tester, resolver != null)
            {
                Add(new Button(resolver)
                {
                    text = label,
                    name = "FixAll"
                });

                AddToClassList("FixAllButton");

                Init();
            }

            public override void UpdateDisplay(Result status, bool haveFixer)
                => this.Q(name: "FixAll").style.display = status != Result.Failed ? DisplayStyle.None : DisplayStyle.Flex;
        }

        class ScopeBox : VisualElementUpdatable
        {
            readonly Label label;

            public ScopeBox(string title) : base(null, false)
            {
                label = new Label(title);
                label.AddToClassList("ScopeBoxLabel");
                AddToClassList("ScopeBox");
                Add(label);
            }

            public override void CheckUpdate()
            {
                foreach (VisualElementUpdatable updatable in Children().Where(e => e is VisualElementUpdatable))
                    updatable.CheckUpdate();
            }

            public override void UpdateDisplay(Result status, bool haveFixer)
            {
                bool hasChildren = false;
                foreach (VisualElementUpdatable updatable in Children().Where(e => e is VisualElementUpdatable))
                {
                    updatable.UpdateDisplay(status, haveFixer);
                    hasChildren = true;
                }

                style.display = hasChildren ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        class InclusiveModeElement : VisualElementUpdatable
        {
            private InclusiveMode m_Mode;
            private ScopeBox m_GlobalScope;
            private ScopeBox m_CurrentScope;
            private FixAllButton m_FixAllButton;
            private HDWizard m_Wizard;

            private bool m_AvailableInCurrentPlatform = true;

            public InclusiveModeElement(InclusiveMode mode, string label, string tooltip, HDWizard wizard)
                : base(null, false)
            {
                m_Mode = mode;
                m_Wizard = wizard;

                var foldout = new HeaderFoldout
                {
                    text = label,
                    documentationURL = DocumentationInfo.GetPageLink(Documentation.packageName, $"Render-Pipeline-Wizard", $"{mode}Tab")
                };

                m_FixAllButton = new FixAllButton(
                    Style.resolveAll,
                    () => m_Wizard.IsAFixAvailableInScope(m_Mode) ? Result.OK : Result.Failed,
                    () => m_Wizard.FixAllEntryInScope(m_Mode));

                foldout.Add(m_FixAllButton);

                m_GlobalScope = new ScopeBox(Style.global);
                foldout.Add(m_GlobalScope);

                m_CurrentScope = new ScopeBox(Style.currentQuality);
                foldout.Add(m_CurrentScope);

                bool userOnWindows = RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                if (!userOnWindows)
                {
                    // VR and DXR are only supported on windows
                    if (m_Mode == InclusiveMode.VR || m_Mode == InclusiveMode.DXROptional)
                    {
                        m_AvailableInCurrentPlatform = false;
                        foldout.Add(new HelpBox("This section is not available in your current OS platform.", HelpBoxMessageType.Warning));
                    }
                }

                m_FixAllButton.style.display = m_AvailableInCurrentPlatform ? DisplayStyle.Flex : DisplayStyle.None;

                foldout.value = HDUserSettings.IsOpen(mode);
                foldout.RegisterValueChangedCallback(evt => HDUserSettings.SetOpen(mode, evt.newValue));
                foldout.Q(className: "header-foldout__label").tooltip = tooltip; // Tooltip on the label to ensure the position doesn't change when opening the foldout

                Add(foldout);
            }

            public void Add(Entry entry, ConfigInfoLine configLine)
            {
                if (!m_AvailableInCurrentPlatform || entry.inclusiveScope != m_Mode)
                    return;

                if (entry.scope == QualityScope.Global)
                    m_GlobalScope.Add(configLine);
                else
                    m_CurrentScope.Add(configLine);
            }

            public override void CheckUpdate()
            {
                m_GlobalScope.CheckUpdate();
                m_CurrentScope.CheckUpdate();
                m_FixAllButton.CheckUpdate();
            }

            public override void UpdateDisplay(Result status, bool haveFixer)
            {
                m_GlobalScope.UpdateDisplay(status, haveFixer);
                m_CurrentScope.UpdateDisplay(status, haveFixer);
                m_FixAllButton.UpdateDisplay(status, haveFixer);
            }
        }

        #endregion
    }
}

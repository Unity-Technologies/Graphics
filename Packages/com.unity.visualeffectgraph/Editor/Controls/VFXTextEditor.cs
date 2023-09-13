using System;
using System.Collections.Generic;

using UnityEditor.Experimental;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    [Serializable]
    class OpenedTextProvider
    {
        [SerializeField] public int index;
        [SerializeField] public VFXModel model;
    }

    class VFXTextEditor : EditorWindow, ISerializationCallbackReceiver, IComparer<OpenedTextProvider>
    {
        readonly struct TextArea
        {
            readonly VFXTextEditor m_Editor;
            readonly ITextProvider m_TextProvider;
            readonly TextField m_TextField;
            readonly Label m_TitleLabel;
            readonly VisualElement m_Root;

            public TextArea(VFXTextEditor editor, ITextProvider textProvider)
            {
                m_Editor = editor;
                m_TextProvider = textProvider;

                var tpl = VFXView.LoadUXML("VFXTextEditorArea");
                m_Root = tpl.CloneTree();

                m_TextField = m_Root.Q<TextField>("TextEditor");
                m_TextField.selectAllOnFocus = false;
                m_TextField.selectAllOnMouseUp = false;
                m_TextField.SetValueWithoutNotify(textProvider.text);

                m_TitleLabel = m_Root.Q<Label>("Label");
                m_TitleLabel.text = textProvider.title;

                var saveButton = m_Root.Q<ToolbarButton>("Save");
                var icon = new Image { image = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "SaveActive.png") };
                saveButton.Add(icon);

                var closeButton = m_Root.Q<ToolbarButton>("Close");
                icon = new Image { image = EditorGUIUtility.LoadIcon("UIPackageResources/Images/close.png") };
                closeButton.Add(icon);

                m_TextField.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
                m_TextField.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
                m_TextField.RegisterCallback<DragPerformEvent>(OnDragPerformed);
                saveButton.RegisterCallback<ClickEvent>(OnSave);
                closeButton.RegisterCallback<ClickEvent>(OnClose);
                m_TextProvider.titleChanged += OnProviderTitleChanged;
                m_TextProvider.textChanged += OnProviderTextChanged;
            }

            public ITextProvider TextProvider => m_TextProvider;

            public VisualElement GetRoot() => m_Root;

            public void Focus() => m_TextField.Focus();

            private void OnProviderTitleChanged() => m_TitleLabel.text = m_TextProvider.title;
            private void OnProviderTextChanged() => m_TextField.value = m_TextProvider.text;

            private void OnTextChanged(ChangeEvent<string> evt)
            {
                m_TitleLabel.text = evt.newValue != m_TextProvider.text
                    ? $"{m_TextProvider.title}*"
                    : m_TextProvider.title;
            }

            private void OnSave(ClickEvent ev)
            {
                if (m_TextProvider.text != m_TextField.value)
                {
                    m_TextProvider.text = m_TextField.value;
                    m_TitleLabel.text = m_TextProvider.title;
                }
            }

            private void OnClose(ClickEvent evt)
            {
                m_Editor.Close(this);
                m_TextProvider.titleChanged -= OnProviderTitleChanged;
                m_TextProvider.textChanged -= OnProviderTextChanged;
                (m_TextProvider as IDisposable)?.Dispose();
            }

            private void OnDragUpdate(DragUpdatedEvent evt)
            {
                evt.StopImmediatePropagation();
                var selection = DragAndDrop.GetGenericData("DragSelection");
                if (selection is List<IParameterItem> { Count: 1 } parameterItems && parameterItems[0] is AttributeItem)
                {
                    m_TextField.cursorIndex = GetTextIndexFromMousePosition(m_TextField, evt.mousePosition);
                    m_TextField.selectIndex = m_TextField.cursorIndex;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
            }

            private void OnDragPerformed(DragPerformEvent evt)
            {
                evt.StopImmediatePropagation();
                var selection = DragAndDrop.GetGenericData("DragSelection");
                if (selection is List<IParameterItem> { Count: 1 } parameterItems && parameterItems[0] is AttributeItem attributeItem)
                {
                    var index = GetTextIndexFromMousePosition(m_TextField, evt.mousePosition);
                    m_TextField.value = m_TextField.value.Insert(index, attributeItem.title);
                    DragAndDrop.AcceptDrag();
                }
            }

            private int GetTextIndexFromMousePosition(TextField textField, Vector2 mousePosition)
            {
                var localMousePos = textField.WorldToLocal(mousePosition) + new Vector2(textField.resolvedStyle.paddingLeft, textField.resolvedStyle.paddingTop);
                var charHeight = textField.MeasureTextSize("A", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
                var lineNumber = (int)Math.Max(0, Math.Ceiling(localMousePos.y / charHeight) - 1);
                var lines = textField.value.Split("\n");

                if (lines.Length > lineNumber)
                {
                    var index = 0;
                    var curX = 0f;
                    var lineText = lines[lineNumber];
                    while (curX < localMousePos.x && index < lineText.Length)
                    {
                        var charSize = textField.MeasureTextSize(lineText[index++].ToString(), 0, VisualElement.MeasureMode.Undefined, charHeight, VisualElement.MeasureMode.Undefined);
                        curX += charSize.x;
                    }
                    // Add character from all lines above
                    for (int i = 0; i < lineNumber; i++)
                    {
                        index += lines[i].Length + 1; // +1 stands for the line return
                    }

                    return index;
                }

                return m_TextField.value.Length;
            }
        }

        const string VFXTextEditorTitle = "HLSL Editor";

        [NonSerialized] readonly List<TextArea> m_OpenedEditors = new();
        [SerializeField] List<OpenedTextProvider> m_OpenedModels;

        Label m_EmptyMessage;

        public void Show(VFXModel model)
        {
            var textArea = m_OpenedEditors.Find(x => ((IHLSLCodeHolder)x.TextProvider.model).Equals((IHLSLCodeHolder)model));
            if (textArea.TextProvider == null)
            {
                var container = rootVisualElement.Q<VisualElement>("container");
                textArea = new TextArea(this, new VFXHLSLTextProvider(model));
                container.Add(textArea.GetRoot());
                m_OpenedEditors.Add(textArea);
                m_EmptyMessage.style.display = DisplayStyle.None;
            }

            textArea.Focus();
        }

        private void CreateGUI()
        {
            titleContent.text = VFXTextEditorTitle;
            rootVisualElement.styleSheets.Add(VFXView.LoadStyleSheet("VFXTextEditor"));

            var tpl = VFXView.LoadUXML("VFXTextEditor");
            var mainContainer = tpl.CloneTree();
            rootVisualElement.Add(mainContainer);
            m_EmptyMessage = rootVisualElement.Q<Label>("emptyMessage");

            EditorApplication.delayCall += RestoreEditedTextProviders;
        }

        private void RestoreEditedTextProviders()
        {
            if (m_OpenedModels != null && m_OpenedModels.Count > 0)
            {
                m_OpenedModels.Sort(this);
                m_OpenedModels.ForEach(x => Show(x.model));
                m_OpenedModels = null;
            }
            else if (m_OpenedEditors.Count == 0)
            {
                m_EmptyMessage.style.display = DisplayStyle.Flex;
            }
        }

        private void Close(TextArea textArea)
        {
            var container = rootVisualElement.Q<VisualElement>("container");
            container.Remove(textArea.GetRoot());
            m_OpenedEditors.Remove(textArea);
            if (m_OpenedEditors.Count == 0)
            {
                m_EmptyMessage.style.display = DisplayStyle.Flex;
            }
        }

        public void OnBeforeSerialize()
        {
            m_OpenedModels = new List<OpenedTextProvider>();
            for (int i = 0; i < m_OpenedEditors.Count; i++)
            {
                m_OpenedModels.Add(new OpenedTextProvider {model = m_OpenedEditors[i].TextProvider.model, index = i });
            }
        }

        public void OnAfterDeserialize()
        {
        }

        public int Compare(OpenedTextProvider x, OpenedTextProvider y)
        {
            return x.index.CompareTo(y.index);
        }
    }
}

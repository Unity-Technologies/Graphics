using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using Unity.Profiling;
using UnityEditor.Experimental;
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
            public static float s_FontSize = 11f;

            static ProfilerMarker s_TextChangedPerfMarker = new("TextArea.OnTextChanged");

            readonly VFXTextEditor m_Editor;
            readonly ITextProvider m_TextProvider;
            readonly TextField m_TextField;
            readonly Label m_TitleLabel;
            readonly VisualElement m_Root;
            readonly Stack<byte[]> m_UndoStack;
            readonly Stack<byte[]> m_RedoStack;

            public TextArea(VFXTextEditor editor, ITextProvider textProvider)
            {
                m_Editor = editor;
                m_TextProvider = textProvider;
                m_UndoStack = new Stack<byte[]>();
                m_RedoStack = new Stack<byte[]>();

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
                m_TextField.style.fontSize = s_FontSize;
            }

            public ITextProvider TextProvider => m_TextProvider;

            public VisualElement GetRoot() => m_Root;
            public void Save() => OnSave(null);

            public bool HasFocus() => m_TextField.HasFocus();
            public void Focus() => m_TextField.Focus();

            public void UpdateTextSize()
            {
                m_TextField.style.fontSize = new StyleLength(s_FontSize);
            }

            public void Undo()
            {
                if (m_UndoStack.Count > 0)
                {
                    var previousText = Decompress(m_UndoStack.Pop());
                    m_RedoStack.Push(Compress(m_TextField.value));
                    m_TextField.SetValueWithoutNotify(previousText);
                }
            }

            public void Redo()
            {
                if (m_RedoStack.Count > 0)
                {
                    var previousText = Decompress(m_RedoStack.Pop());
                    m_UndoStack.Push(Compress(m_TextField.value));
                    m_TextField.SetValueWithoutNotify(previousText);
                }
            }

            private byte[] Compress(string text)
            {
                using var memoryStream = new MemoryStream();
                using (var gzipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    gzipStream.Write(Encoding.UTF8.GetBytes(text));
                }

                return memoryStream.ToArray();
            }

            private string Decompress(byte[] data)
            {
                using var memoryStream = new MemoryStream(data);
                using var outputStream = new MemoryStream();
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(outputStream);
                }

                return Encoding.UTF8.GetString(outputStream.ToArray());
            }

            private void OnProviderTitleChanged() => m_TitleLabel.text = m_TextProvider.title;
            private void OnProviderTextChanged() => m_TextField.value = m_TextProvider.text;

            private void OnTextChanged(ChangeEvent<string> evt)
            {
                s_TextChangedPerfMarker.Begin();
                try
                {
                    m_UndoStack.Push(Compress(evt.previousValue));
                    m_RedoStack.Clear();
                    m_TitleLabel.text = evt.newValue != m_TextProvider.text
                        ? $"{m_TextProvider.title}*"
                        : m_TextProvider.title;
                }
                finally
                {
                    s_TextChangedPerfMarker.End();
                }
                //Debug.Log($"Undo stack:\n\tmemory occupation: {m_UndoStack.Sum(x => x.Length) * sizeof(byte) / 1000}kb -- Length: {m_UndoStack.Count}");
            }

            private void OnSave(ClickEvent ev)
            {
                if (m_TextProvider.text != m_TextField.value)
                {
                    m_TextProvider.text = m_TextField.value;
                    m_TitleLabel.text = m_TextProvider.title;
                }
            }

            internal void OnClose(ClickEvent evt)
            {
                m_Editor.Close(this);
                m_UndoStack.Clear();
                m_RedoStack.Clear();
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
        [NonSerialized] readonly List<VFXGraph> m_RegisteredGraphs = new();
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

                var graph = model.GetGraph();
                if (!m_RegisteredGraphs.Contains(graph))
                {
                    m_RegisteredGraphs.Add(graph);
                    graph.onInvalidateDelegate += OnGraphInvalidate;
                }
            }

            textArea.Focus();
        }

        private void OnGraphInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            foreach (var textArea in m_OpenedEditors.ToArray())
            {
                if (textArea.TextProvider.model.GetParent() == null)
                {
                    textArea.OnClose(null);
                }
            }
        }

        public void Undo()
        {
            foreach (var textArea in m_OpenedEditors)
            {
                if (textArea.HasFocus())
                {
                    textArea.Undo();
                    break;
                }
            }
        }

        public void Redo()
        {
            foreach (var textArea in m_OpenedEditors)
            {
                if (textArea.HasFocus())
                {
                    textArea.Redo();
                    break;
                }
            }
        }

        public void ChangeTextSize(int delta)
        {
            TextArea.s_FontSize = Mathf.Clamp(TextArea.s_FontSize + delta, 11f, 20f);
            m_OpenedEditors.ForEach(x => x.UpdateTextSize());
        }

        public void Save()
        {
            foreach (var textArea in m_OpenedEditors)
            {
                if (textArea.HasFocus())
                {
                    textArea.Save();
                    break;
                }
            }
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

            var remainingGraphs = new HashSet<VFXGraph>();
            foreach (var editor in m_OpenedEditors)
            {
                remainingGraphs.Add(editor.TextProvider.model.GetGraph());
            }

            foreach (var toRemoveGraphs in m_RegisteredGraphs.ToArray())
            {
                if (!remainingGraphs.Contains(toRemoveGraphs))
                {
                    toRemoveGraphs.onInvalidateDelegate -= OnGraphInvalidate;
                    m_RegisteredGraphs.Remove(toRemoveGraphs);
                }
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

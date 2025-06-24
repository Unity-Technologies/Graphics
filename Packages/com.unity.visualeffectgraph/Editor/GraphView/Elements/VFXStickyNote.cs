using System;
using System.Collections.Generic;

using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXStickyNoteController : VFXUIController<VFXUI.StickyNoteInfo>
    {
        public VFXStickyNoteController(VFXViewController viewController, VFXUI ui, int index) : base(viewController, ui, index)
        {
            // Happens when the sticky note comes from an older version (different UI)
            if (colorTheme == 0)
            {
                // Slightly increase width to prevent the title from being wrapped because of the space taken by the color swatches
                position = new Rect(position.x, position.y, position.width + 6, position.height);
                // Convert old theme to new theme
                colorTheme = string.Compare(m_UI.stickyNoteInfos[m_Index].theme, nameof(StickyNoteTheme.Black), StringComparison.OrdinalIgnoreCase) == 0 ? 2 : 1;
            }
        }

        public string contents
        {
            get
            {
                if (m_Index < 0) return "";

                return m_UI.stickyNoteInfos[m_Index].contents;
            }
            set
            {
                if (m_Index < 0) return;

                m_UI.stickyNoteInfos[m_Index].contents = value;

                Modified();
            }
        }
        protected override VFXUI.StickyNoteInfo[] infos => m_UI.stickyNoteInfos;

        public int colorTheme
        {
            get => m_UI.stickyNoteInfos[m_Index].colorTheme;
            set
            {
                if (value != colorTheme)
                {
                    m_UI.stickyNoteInfos[m_Index].colorTheme = value;
                    Modified();
                }
            }
        }
        public string fontSize
        {
            get => m_UI.stickyNoteInfos[m_Index].textSize;
            set
            {
                m_UI.stickyNoteInfos[m_Index].textSize = value;
                Modified();
            }
        }
    }

    class VFXStickyNote : StickyNote, IControlledElement<VFXStickyNoteController>, IVFXMovable
    {
        const int k_DefaultThemeColor = 1;

        int m_ColorTheme;
        VFXStickyNoteController m_Controller;

        readonly DropdownField m_FontSizeDropdown;

        public VFXStickyNote() : base(VisualEffectAssetEditorUtility.editorResourcesPath + "/uxml/VFXStickyNote.uxml", Vector2.zero)
        {
            this.styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/Selectable.uss") as StyleSheet);
            this.styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/StickyNote.uss") as StyleSheet);

            this.Q<Button>("swatch1").clicked += OnSwatch1;
            this.Q<Button>("swatch2").clicked += OnSwatch2;
            this.Q<Button>("swatch3").clicked += OnSwatch3;
            this.Q<Button>("fitToText").clicked += OnClickFitToText;

            m_FontSizeDropdown = this.Q<DropdownField>("fontSize");
            m_FontSizeDropdown.choices = new List<string> { "Small", "Medium", "Large", "Huge" };
            m_FontSizeDropdown.RegisterValueChangedCallback(OnFontSizeChanged);

            this.AddStyleSheetPath("VFXStickyNote");
            this.capabilities |= Capabilities.Groupable;
            this.RegisterCallback<StickyNoteChangeEvent>(OnUIChange);
        }

        void OnFontSizeChanged(ChangeEvent<string> evt)
        {
            if (Enum.TryParse<StickyNoteFontSize>(evt.newValue, out var newFontSize))
            {
                fontSize = newFontSize;
                UpdateFontSize();
            }
        }

        void OnClickFitToText()
        {
            FitText(false);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.RemoveItemAt(0);
            var vfxView = GetFirstAncestorOfType<VFXView>();
            evt.menu.InsertAction(
                0,
                "Group Selection",
                _ => vfxView.GroupSelection(),
                _ => vfxView.canGroupSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.InsertSeparator(string.Empty, 1);
        }

        public void OnMoved()
        {
            controller.position = new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        Controller IControlledElement.controller => m_Controller;

        public VFXStickyNoteController controller
        {
            get => m_Controller;
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_FontSizeDropdown.SetValueWithoutNotify(m_Controller.fontSize);
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        void OnSwatch1() => SetTheme(1);
        void OnSwatch2() => SetTheme(2);
        void OnSwatch3() => SetTheme(3);

        void SetTheme(int swatch)
        {
            if (swatch != m_ColorTheme)
            {
                // Remove inline color for background
                var nodeBorder = this.Q<VisualElement>("node-border");
                nodeBorder.style.backgroundColor = new StyleColor(StyleKeyword.Null);
                nodeBorder.style.borderTopColor = nodeBorder.style.borderRightColor = nodeBorder.style.borderBottomColor = nodeBorder.style.borderLeftColor = new StyleColor(StyleKeyword.Null);

                // Remove inline color for text
                this.Query<Label>().ForEach(x =>
                {
                    x.style.color = new StyleColor(StyleKeyword.Null);
                });

                RemoveFromClassList($"color-theme-{m_ColorTheme}");
                controller.colorTheme = swatch;
                m_ColorTheme = swatch;
                AddToClassList($"color-theme-{m_ColorTheme}");
            }
        }

        void OnUIChange(StickyNoteChangeEvent e)
        {
            if (m_Controller == null) return;

            switch (e.change)
            {
                case StickyNoteChange.Title:
                    controller.title = title;
                    break;
                case StickyNoteChange.Contents:
                    controller.contents = contents;
                    break;
                case StickyNoteChange.FontSize:
                    m_FontSizeDropdown.SetValueWithoutNotify(fontSize.ToString());
                    UpdateFontSize();
                    break;
                case StickyNoteChange.Position:
                    controller.position = new Rect(resolvedStyle.left, resolvedStyle.top, style.width.value.value, style.height.value.value);
                    break;
            }
        }

        void UpdateFontSize()
        {
            var previousFontSizeString = controller.fontSize;
            controller.fontSize = fontSize.ToString();
            if (Enum.TryParse<StickyNoteFontSize>(previousFontSizeString, out var previousFontSize) && fontSize > previousFontSize)
            {
                // Need to dispatch the fit text after the layout pass has properly taken the font size change
                Dispatcher.Enqueue(() => FitText(false), 0.1f);
            }
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            title = controller.title;
            contents = controller.contents;
            if (controller.colorTheme == 0)
                controller.colorTheme = k_DefaultThemeColor;
            SetTheme(controller.colorTheme);

            if (!string.IsNullOrEmpty(controller.fontSize))
            {
                try
                {
                    fontSize = (StickyNoteFontSize)Enum.Parse(typeof(StickyNoteFontSize), controller.fontSize, true);
                }
                catch (ArgumentException)
                {
                    controller.fontSize = nameof(StickyNoteFontSize.Small);
                    Debug.LogError("Unknown text size name");
                }
            }

            SetPosition(controller.position);
        }
    }
}

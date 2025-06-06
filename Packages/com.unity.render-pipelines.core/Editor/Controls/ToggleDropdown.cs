using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    [UxmlElement]
    internal partial class ToggleDropdown : VisualElement
    {
        const string k_StylesheetPathFormat = "Packages/com.unity.render-pipelines.core/Editor/Controls/ToggleDropdown.uss";
        const string k_MainClass = "toggle-dropdown";
        const string k_ToggleButtonClass = k_MainClass + "__toggle-button";
        const string k_DropdownButtonClass = k_MainClass + "__dropdown-button";
        const string k_SeparatorClass = k_MainClass + "__separator";
        const string k_EnabledClass = k_MainClass + "--enabled";

        private Button m_ToggleButton;
        private Button m_DropdownButton;
        private VisualElement m_DropdownArrow;
        private VisualElement m_Separator;
        private List<string> m_Options = new List<string>();
        private HashSet<int> m_SelectedIndices = new HashSet<int>();
        private int m_SelectedIndex = 0;
        private bool m_IsEnabled = false;
        private string m_Text = "Toggle Dropdown";

        [UxmlAttribute]
        public string text
        {
            get => m_Text;
            set
            {
                if (m_Text == value)
                    return;
                m_Text = value;
                m_ToggleButton.text = m_Text;
            }
        }

        [UxmlAttribute]
        private string options { get; set; } = "";

        [UxmlAttribute("selected-index")]
        private int selectedIndex
        {
            get => m_SelectedIndex;
            set
            {
                if (m_SelectedIndex == value || value < 0)
                    return;
                m_SelectedIndex = value;
            }
        }

        [UxmlAttribute("selected-indices")]
        public string selectedIndices
        {
            get
            {
                var indices = new List<string>();
                foreach (int index in m_SelectedIndices)
                {
                    indices.Add(index.ToString());
                }
                return string.Join(",", indices.ToArray());
            }
            set
            {
                m_SelectedIndices.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    var indices = value.Split(',');
                    foreach (var indexStr in indices)
                    {
                        if (int.TryParse(indexStr.Trim(), out int index) && index >= 0)
                        {
                            m_SelectedIndices.Add(index);
                        }
                    }
                }
            }
        }

        [UxmlAttribute]
        public bool value
        {
            get => m_IsEnabled;
            set
            {
                if (m_IsEnabled == value)
                    return;
                m_IsEnabled = value;
                UpdateEnabledState();
            }
        }

        /// <summary>Get the currently selected option text (first selected)</summary>
        public string selectedOption
        {
            get
            {
                foreach (int index in m_SelectedIndices)
                {
                    if (index < m_Options.Count)
                        return m_Options[index];
                }
                return "";
            }
        }

        /// <summary>Get all selected options</summary>
        public string[] selectedOptions
        {
            get
            {
                var result = new List<string>();
                foreach (int index in m_SelectedIndices)
                {
                    if (index < m_Options.Count)
                        result.Add(m_Options[index]);
                }
                return result.ToArray();
            }
        }

        /// <summary>Get all selected indices</summary>
        public int[] GetSelectedIndices()
        {
            var result = new int[m_SelectedIndices.Count];
            int i = 0;
            foreach (int index in m_SelectedIndices)
            {
                result[i++] = index;
            }
            return result;
        }

        /// <summary>Event fired when selection changes</summary>
        public event System.Action<int[]> selectionChanged;

        /// <summary>Event fired when toggle state changes</summary>
        public event System.Action<bool> toggleChanged;

        /// <summary>Constructor</summary>
        public ToggleDropdown()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylesheetPathFormat));
            AddToClassList(k_MainClass);

            RegisterCallback<AttachToPanelEvent>(DelayedInit);

            m_ToggleButton = new Button(OnToggleClicked);
            m_ToggleButton.AddToClassList(k_ToggleButtonClass);
            Add(m_ToggleButton);

            m_DropdownButton = new Button(ShowCustomDropdown);
            m_DropdownButton.AddToClassList(k_DropdownButtonClass);

            m_DropdownArrow = new VisualElement();
            m_DropdownArrow.AddToClassList("unity-toolbar-menu__arrow");
            m_DropdownButton.Add(m_DropdownArrow);

            m_Separator = new VisualElement();
            m_Separator.AddToClassList(k_SeparatorClass);
            Add(m_Separator);

            Add(m_DropdownButton);
        }

        void DelayedInit(AttachToPanelEvent evt)
        {
            if (!string.IsNullOrEmpty(options))
            {
                var optionArray = options.Split(',');
                for (int i = 0; i < optionArray.Length; i++)
                {
                    optionArray[i] = optionArray[i].Trim();
                }
                SetOptions(optionArray);
            }

            if (selectedIndex >= 0)
            {
                m_SelectedIndices.Add(selectedIndex);
            }

            m_IsEnabled = value;
            m_ToggleButton.text = m_Text;
            UpdateEnabledState();
        }

        /// <summary>Set the available options for the dropdown</summary>
        public void SetOptions(string[] newOptions)
        {
            m_Options.Clear();
            if (newOptions != null)
                m_Options.AddRange(newOptions);

            if (m_SelectedIndex >= m_Options.Count)
                m_SelectedIndex = 0;
        }

        /// <summary>Set the selected indices for multi-select</summary>
        public void SetSelectedIndices(int[] indices)
        {
            m_SelectedIndices.Clear();
            if (indices != null)
            {
                foreach (int index in indices)
                {
                    if (index >= 0 && index < m_Options.Count)
                    {
                        m_SelectedIndices.Add(index);
                    }
                }
            }
            selectionChanged?.Invoke(GetSelectedIndices());
        }

        /// <summary>Toggle selection of a specific index</summary>
        public void ToggleSelection(int index)
        {
            if (index >= 0 && index < m_Options.Count)
            {
                if (m_SelectedIndices.Contains(index))
                {
                    m_SelectedIndices.Remove(index);
                }
                else
                {
                    m_SelectedIndices.Add(index);
                }
                selectionChanged?.Invoke(GetSelectedIndices());
            }
        }

        /// <summary>Check if an index is selected</summary>
        public bool IsSelected(int index)
        {
            return m_SelectedIndices.Contains(index);
        }

        /// <summary>Set the enabled state</summary>
        public new void SetEnabled(bool enabled)
        {
            if (enabled != m_IsEnabled)
            {
                m_IsEnabled = enabled;
                UpdateEnabledState();
                toggleChanged?.Invoke(enabled);
            }
        }

        void OnToggleClicked()
        {
            SetEnabled(!m_IsEnabled);
        }

        void UpdateEnabledState()
        {
            if (m_IsEnabled)
            {
                AddToClassList(k_EnabledClass);
            }
            else
            {
                RemoveFromClassList(k_EnabledClass);
            }

            MarkDirtyRepaint();
        }

        void ShowCustomDropdown()
        {
            var menu = new GenericMenu();

            for (int i = 0; i < m_Options.Count; i++)
            {
                int index = i;
                string optionName = m_Options[i];
                bool isSelected = m_SelectedIndices.Contains(index);

                menu.AddItem(new GUIContent(optionName), isSelected, () => {
                    ToggleSelection(index);
                });
            }

            var rect = this.worldBound;
            menu.DropDown(rect);
        }
    }
}

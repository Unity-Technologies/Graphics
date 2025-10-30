using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Rendering
{
    abstract class AdvancedDropdownItemBridge : AdvancedDropdownItem
    {
        public AdvancedDropdownItemBridge(string name)
            : base(name)
        {
        }
    }

    abstract class AdvancedDropdownWindowBridge : AdvancedDropdownWindow
    {
        private const string kDropDownFilterWindow = "DropDownFilterWindow";

        public static bool Show<T>(Rect rect, AdvancedDropdownDataSource dataSource)
            where T : AdvancedDropdownWindow
        {
            AdvancedDropdownWindowBridge.CloseAllOpenWindows<T>();
            var window = CreateInstance<T>() as AdvancedDropdownWindow;
            window.dataSource = dataSource;
            window.Init(rect);
            window.searchString = EditorPrefs.GetString(kDropDownFilterWindow, "");
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectionChanged += OnItemSelected;
        }

        protected abstract void OnItemSelected(AdvancedDropdownItem item);
    }

    abstract class AdvancedDropdownDataSourceBridge : AdvancedDropdownDataSource
    {
        public AdvancedDropdownDataSourceBridge()
        {
            CurrentFolderContextualSearch = true;
        }

        protected override AdvancedDropdownItem FetchData() { return null; }

        
    }
}

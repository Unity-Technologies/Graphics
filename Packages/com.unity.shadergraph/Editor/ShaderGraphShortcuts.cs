
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphShortcuts
    {
        static MaterialGraphEditWindow GetFocusedShaderGraphEditorWindow()
        {
            MaterialGraphEditWindow[] windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
            foreach(var window in windows)
            {
                if (window.hasFocus)
                    return window;
            }
            return null;
        }


        [Shortcut("ShaderGraph/ShaderGraph: Save", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Action)]
        static void Save(ShortcutArguments args)
        {
            GetFocusedShaderGraphEditorWindow().SaveAsset();
        }

        [Shortcut("ShaderGraph/ShaderGraph: Save As...", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        static void SaveAs(ShortcutArguments args)
        {
            GetFocusedShaderGraphEditorWindow().SaveAs();
        }
    }
}

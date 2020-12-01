using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor.ShortcutManagement;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Input
{
    struct HotkeyDataModel
    {
        Dictionary<string, string> NodeTitleToHotkeyIDMap;
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class ShaderGraphHotkeyAttribute : ShortcutAttribute
    {
        public ShaderGraphHotkeyAttribute(string id)
            : base(id, typeof(MaterialGraphEditWindow)) { }

        public ShaderGraphHotkeyAttribute(string id, KeyCode defaultKeyCode, ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : base(id, typeof(MaterialGraphEditWindow), defaultKeyCode, defaultShortcutModifiers)
        {

        }
    }

    public class SGHotkey<T>
    {
        [ShaderGraphHotkeyAttribute("Shader Graph/Add Node", KeyCode.Period, ShortcutModifiers.Shift)]
        void DoThing()
        {
            Debug.Log(typeof(T));
        }
    }

    [InitializeOnLoad]
    static class ShaderGraphHotkeys
    {
        static ShaderGraphHotkeys()
        {
            var addHotkey = new SGHotkey<AddNode>();
            var multipleHotkey = new SGHotkey<MultiplyNode>();


        }
        public static void InitializeNodeCreationShortcuts(IEnumerable<Type> knownNodeTypes)
        {
        }


    }

}

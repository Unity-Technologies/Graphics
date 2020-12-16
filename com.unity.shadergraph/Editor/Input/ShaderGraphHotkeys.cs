using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor.ShortcutManagement;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Input
{
    [Serializable]
    public sealed class NodeToHotkeyMap : SerializedDictionary<string, KeyCode> {}

    struct HotkeyDataModel
    {
        NodeToHotkeyMap m_NodeToHotkeyMap;

        public NodeToHotkeyMap HotkeyMap
        {
            get => m_NodeToHotkeyMap;
            set => m_NodeToHotkeyMap = value;
        }
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
    [InitializeOnLoad]
    static class ShaderGraphHotkeys
    {
        static HotkeyDataModel s_HotkeyData;

        /*static string s_HotkeyTemplate = @"public class ShaderGraphHotkeyTemplate
        {
            [ShaderGraphHotkeyAttribute(""Create NODETYPE"", KEYCODE)]
            private static void CreateNODETYPE(ShortcutArguments args)
            {
                ExecuteShortcut(args, graphData => { graphData.AddNode(new NODETYPE()); });
            }
        }";*/

        static string s_OutputPath = "Editor/Input/Hotkeys";

        static ShaderGraphHotkeys()
        {
            // Initialize the hotkey map if it is null (i.e. never been created before)
            s_HotkeyData.HotkeyMap ??= new NodeToHotkeyMap();
        }

        /// <summary>
        /// Generate for a simple template. This is a template that has one type variable.
        /// </summary>
        ///
        /// <param name="templateName">
        /// Name of the template file without the ".cs" extension.
        /// </param>
        ///
        /// <param name="type">
        /// Type to replace the type variable with: TYPE and TYPENAMESPACE.
        /// </param>
        static void GenerateHotkeyFromTemplate(
            string hotkeyTemplate,
            Type type,
            KeyCode keyCode
        )
        {
            // Create a copy of the template
            var template = new string(hotkeyTemplate.ToCharArray());

            // Replace variables in the template
            string result = template
                .Replace("NODETYPE", type.Name)
                .Replace("KEYCODE", keyCode.ToString());

            string packagePath = DefaultShaderIncludes.GetAssetsPackagePath();
            string outputPath = Path.Combine(packagePath, s_OutputPath);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            outputPath = Path.Combine(outputPath, type.Name);
            outputPath += ".cs";
            File.WriteAllText(outputPath, result);

            // Refresh the asset database to show the (potentially) new file
            AssetDatabase.Refresh();
        }

        public static void InitializeNodeCreationShortcuts(IEnumerable<Type> knownNodeTypes)
        {
            foreach (var nodeType in knownNodeTypes)
            {
                Debug.Log("Node of type:" + nodeType.Name);
                // Instead of iterating through all nodes, should at first just iterate through nodes marked with an attribute
                // These are the nodes that we want to assign default hotkeys to, can extract keycode and node type from attribute gathering
                // Then we can iterate through the types, find subgraphs and add those
                // The only real issue is differentiating nodes from each other, like which are real nodes we want and which arent
                // Cause there are test nodes, the legacy nodes etc, dont want to create hotkeys for those
                //s_HotkeyData.HotkeyMap.Add(nodeType.Name, KeyCode.RightWindows);
                //GenerateHotkeyFromTemplate(s_HotkeyTemplate, nodeType, KeyCode.RightWindows);
            }
        }
    }
}

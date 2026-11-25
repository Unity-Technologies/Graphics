using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    static class ProviderStyleSheets
    {
        internal const string kLocalProviderHighlight = "LocalProviderHighlight";
        internal const string kProviderInspector = "ProviderInspector";
        internal const string kShaderInterfaceSettings = "ShaderInterfaceSettings";
        internal const string kVirtualFunctionNodeDragger = "VirtualFunctionNodeDragger";

        const string kFolder = "Packages/com.unity.shadergraph/Editor/ProviderSystem/Styles/";

        internal static void Add(VisualElement element, string name)
        {
            var path = kFolder + name + ".uss";
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet != null)
                element.styleSheets.Add(sheet);
            else
                Debug.LogWarning($"Style sheet '{path}' could not be loaded.");
        }
    }
}

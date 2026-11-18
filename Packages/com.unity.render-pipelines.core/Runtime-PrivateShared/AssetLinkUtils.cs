#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.RenderPipelines.Core.Runtime.Shared
{
    internal static class AssetLinkUtils
    {
        const string k_LinkKey = "srpasset";

        internal static string AssetLink(Object asset)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

            string color = EditorGUIUtility.isProSkin ? "#40a0ff" : "#0000FF";

            return $"<link=\"{k_LinkKey}='{guid}'\"><color={color}><u>{asset.name}</u></color></link>";
        }

        [InitializeOnLoadMethod]
        static void RegisterLinkHandler()
        {
            EditorGUI.hyperLinkClicked -= OnLinkClicked;
            EditorGUI.hyperLinkClicked += OnLinkClicked;
        }

        static void OnLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args) => SelectLinkedAsset(args.hyperLinkData);

        internal static void SelectLinkedAsset(Dictionary<string, string> hyperLinkData)
        {
            if (!hyperLinkData.TryGetValue(k_LinkKey, out string guid))
                return;

            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif

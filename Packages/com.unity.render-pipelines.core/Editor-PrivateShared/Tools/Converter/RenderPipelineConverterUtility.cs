using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityEditor.Rendering.Converter
{
    /// <summary>
    /// Utility methods for render pipeline converters
    /// </summary>
    internal static class RenderPipelineConverterUtility
    {
        private static Texture2D s_FolderIconCache;

        /// <summary>
        /// Helper class to represent a folder item with children
        /// </summary>
        [Serializable]
        internal class FolderItem : IFolderRenderPipelineConverterItem
        {
            [field:SerializeField] public string name { get; set; }
            [field:SerializeField] public string info { get; set; }

            /// <summary>
            /// Folder is enabled if at least one child is enabled.
            /// This follows Unity Editor Design System guidelines: disable controls that aren't available right now.
            /// </summary>
            public bool isEnabled
            {
                get
                {
                    // A folder is enabled if any of its children are enabled
                    foreach (var child in m_Children)
                    {
                        if (child.isEnabled)
                            return true;
                    }
                    return false;
                }
                set { /* Computed property, setter is no-op */ }
            }

            [field:SerializeField] public string isDisabledMessage { get; set; }

            [SerializeReference]
            private List<IRenderPipelineConverterItem> m_Children = new List<IRenderPipelineConverterItem>();
            public IList<IRenderPipelineConverterItem> children
            {
                get => m_Children;
                set => m_Children = value == null ? new List<IRenderPipelineConverterItem>() : (value as List<IRenderPipelineConverterItem> ?? new List<IRenderPipelineConverterItem>(value));
            }

            public virtual Texture2D icon => s_FolderIconCache ??= EditorGUIUtility.FindTexture("Folder Icon");

            public void OnClicked() { }
        }

        /// <summary>
        /// Helper class to represent an asset group (not a file system folder) with children
        /// Shows an asset type icon instead of a folder icon
        /// </summary>
        [Serializable]
        internal class AssetGroupItem : IFolderRenderPipelineConverterItem, ISerializationCallbackReceiver
        {
            [field:SerializeField] public string name { get; set; }
            [field:SerializeField] public string info { get; set; }

            /// <summary>
            /// Asset group is enabled if at least one child is enabled.
            /// This follows Unity Editor Design System guidelines: disable controls that aren't available right now.
            /// </summary>
            public bool isEnabled
            {
                get
                {
                    // An asset group is enabled if any of its children are enabled
                    foreach (var child in m_Children)
                    {
                        if (child.isEnabled)
                            return true;
                    }
                    return false;
                }
                set { /* Computed property, setter is no-op */ }
            }

            [field:SerializeField] public string isDisabledMessage { get; set; }

            [SerializeReference]
            private List<IRenderPipelineConverterItem> m_Children = new List<IRenderPipelineConverterItem>();
            public IList<IRenderPipelineConverterItem> children
            {
                get => m_Children;
                set => m_Children = value == null ? new List<IRenderPipelineConverterItem>() : (value as List<IRenderPipelineConverterItem> ?? new List<IRenderPipelineConverterItem>(value));
            }

            /// <summary>
            /// The asset type to use for the icon (e.g., typeof(Material), typeof(Shader))
            /// </summary>
            public System.Type assetType { get; set; }

            [SerializeField]
            private string m_AssetTypeAssemblyQualifiedName;

            private Texture2D m_CachedIcon;

            public void OnBeforeSerialize()
            {
                m_AssetTypeAssemblyQualifiedName = assetType?.AssemblyQualifiedName;
            }

            public void OnAfterDeserialize()
            {
                if (!string.IsNullOrEmpty(m_AssetTypeAssemblyQualifiedName))
                {
                    assetType = System.Type.GetType(m_AssetTypeAssemblyQualifiedName);
                }
            }

            public virtual Texture2D icon
            {
                get
                {
                    if (m_CachedIcon == null && assetType != null)
                    {
                        var content = EditorGUIUtility.ObjectContent(null, assetType);
                        m_CachedIcon = content?.image as Texture2D;
                    }
                    return m_CachedIcon;
                }
            }

            public void OnClicked() { }

            public void Reset()
            {
                name = null;
                info = null;
                // isEnabled is now computed from children, no need to reset it
                isDisabledMessage = null;
                assetType = null;
                m_AssetTypeAssemblyQualifiedName = null;
                m_CachedIcon = null;
                m_Children.Clear();
            }
        }

        /// <summary>
        /// Recursively collects all leaf items from a collection, flattening any folder hierarchies.
        /// </summary>
        internal static void CollectLeafItems(IEnumerable<IRenderPipelineConverterItem> items, List<IRenderPipelineConverterItem> targetList)
        {
            foreach (var item in items)
            {
                if (item is IFolderRenderPipelineConverterItem folder)
                    CollectLeafItems(folder.children, targetList);
                else
                    targetList.Add(item);
            }
        }
    }
}

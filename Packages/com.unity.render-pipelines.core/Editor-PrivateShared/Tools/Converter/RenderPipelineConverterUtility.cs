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
        internal class FolderItem : IFolderRenderPipelineConverterItem
        {
            public string name { get; set; }
            public string info { get; set; }
            public bool isEnabled { get; set; } = true;
            public string isDisabledMessage { get; set; }
            public IList<IRenderPipelineConverterItem> children { get; set; } = new List<IRenderPipelineConverterItem>();

            public virtual Texture2D icon => s_FolderIconCache ??= EditorGUIUtility.FindTexture("Folder Icon");

            public void OnClicked() { }
        }

        /// <summary>
        /// Helper class to represent an asset group (not a file system folder) with children
        /// Shows an asset type icon instead of a folder icon
        /// </summary>
        internal class AssetGroupItem : IFolderRenderPipelineConverterItem
        {
            public string name { get; set; }
            public string info { get; set; }
            public bool isEnabled { get; set; } = true;
            public string isDisabledMessage { get; set; }
            public IList<IRenderPipelineConverterItem> children { get; set; } = new List<IRenderPipelineConverterItem>();

            /// <summary>
            /// The asset type to use for the icon (e.g., typeof(Material), typeof(Shader))
            /// </summary>
            public System.Type assetType { get; set; }

            private Texture2D m_CachedIcon;

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
                isEnabled = true;
                isDisabledMessage = null;
                assetType = null;
                m_CachedIcon = null;
                children.Clear();
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

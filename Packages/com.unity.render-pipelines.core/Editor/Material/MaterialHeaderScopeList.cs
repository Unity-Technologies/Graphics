using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Collection to store <see cref="MaterialHeaderScopeItem"></see>
    /// </summary>
    public class MaterialHeaderScopeList
    {
        readonly uint m_DefaultExpandedState;
        internal readonly List<MaterialHeaderScopeItem> m_Items = new List<MaterialHeaderScopeItem>();

        /// <summary>
        /// Constructor that initializes it with the default expanded state for the internal scopes
        /// </summary>
        /// <param name="defaultExpandedState">By default, everything is expanded</param>
        public MaterialHeaderScopeList(uint defaultExpandedState = uint.MaxValue)
        {
            m_DefaultExpandedState = defaultExpandedState;
        }

        /// <summary>
        /// Registers a <see cref="MaterialHeaderScopeItem"/> into the list
        /// </summary>
        /// <param name="title"><see cref="GUIContent"/> The title of the scope</param>
        /// <param name="expandable">The mask identifying the scope</param>
        /// <param name="action">The action that will be drawn if the scope is expanded</param>
        /// <typeparam name="TEnum"> The enum for the scope </typeparam>
        public void RegisterHeaderScope<TEnum>(GUIContent title, TEnum expandable, Action<Material> action)
            where TEnum : struct, IConvertible
        {
            m_Items.Add(new MaterialHeaderScopeItem()
            {
                headerTitle = title,
                expandable = Convert.ToUInt32(expandable),
                drawMaterialScope = action,
                url = DocumentationUtils.GetHelpURL<TEnum>(expandable)
            });
        }

        /// <summary>
        /// Draws all the <see cref="MaterialHeaderScopeItem"/> with its information stored
        /// </summary>
        /// <param name="materialEditor"><see cref="MaterialEditor"/></param>
        /// <param name="material"><see cref="Material"/></param>
        public void DrawHeaders(MaterialEditor materialEditor, Material material)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            if (materialEditor == null)
                throw new ArgumentNullException(nameof(materialEditor));

            foreach (var item in m_Items)
            {
                using var header = new MaterialHeaderScope(
                    item.headerTitle,
                    item.expandable,
                    materialEditor,
                    defaultExpandedState: m_DefaultExpandedState,
                    documentationURL: item.url);
                if (!header.expanded)
                    continue;

                item.drawMaterialScope(material);

                EditorGUILayout.Space();
            }
        }
    }
}

using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Set of extensions to allow storing, getting and setting the expandable states of a <see cref="MaterialEditor"/> areas
    /// </summary>
    internal static class MaterialEditorExtension
    {
        const string k_KeyPrefix = "CoreRP:Material:UI_State:";

        /// <summary>
        /// Obtains if an area is expanded in a <see cref="MaterialEditor"/>
        /// </summary>
        /// <param name="editor"><see cref="MaterialEditor"/></param>
        /// <param name="mask">The mask identifying the area to check the state</param>
        /// <param name="defaultExpandedState">Default value if is key is not present</param>
        /// <returns>true if the area is expanded</returns>
        public static bool IsAreaExpanded(this MaterialEditor editor, uint mask, uint defaultExpandedState = uint.MaxValue)
        {
            string key = editor.GetEditorPrefsKey();

            if (EditorPrefs.HasKey(key))
            {
                uint state = (uint)EditorPrefs.GetInt(key);
                return (state & mask) > 0;
            }

            EditorPrefs.SetInt(key, (int)defaultExpandedState);
            return (defaultExpandedState & mask) > 0;
        }

        /// <summary>
        /// Sets if the area is expanded <see cref="MaterialEditor"/>
        /// </summary>
        /// <param name="editor"><see cref="MaterialEditor"/></param>
        /// <param name="mask">The mask identifying the area to check the state</param>
        public static void SetIsAreaExpanded(this MaterialEditor editor, uint mask, bool value)
        {
            string key = editor.GetEditorPrefsKey();

            uint state = (uint)EditorPrefs.GetInt(key);

            if (value)
            {
                state |= mask;
            }
            else
            {
                mask = ~mask;
                state &= mask;
            }

            EditorPrefs.SetInt(key, (int)state);
        }

        static string GetEditorPrefsKey(this MaterialEditor editor)
        {
            return k_KeyPrefix + (editor.target as Material).shader.name;
        }
    }
}

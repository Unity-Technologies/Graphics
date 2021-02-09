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
            string key = GetKey(editor);

            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetInt(key, (int)defaultExpandedState);
                return (defaultExpandedState & mask) > 0;
            }

            uint state = GetState(editor);
            bool result = (state & mask) > 0;
            return result;
        }

        /// <summary>
        /// Sets if the area is expanded <see cref="MaterialEditor"/>
        /// </summary>
        /// <param name="editor"><see cref="MaterialEditor"/></param>
        /// <param name="mask">The mask identifying the area to check the state</param>
        public static void SetIsAreaExpanded(this MaterialEditor editor, uint mask, bool value)
        {
            uint state = GetState(editor);

            if (value)
            {
                state |= mask;
            }
            else
            {
                mask = ~mask;
                state &= mask;
            }

            SetState(editor, state);
        }

        static uint GetState(this MaterialEditor editor)
        {
            return (uint)EditorPrefs.GetInt(GetKey(editor));
        }

        static void SetState(this MaterialEditor editor, uint value)
        {
            EditorPrefs.SetInt(GetKey(editor), (int)value);
        }

        static string GetKey(this MaterialEditor editor)
        {
            return k_KeyPrefix + (editor.target as Material).shader.name;
        }
    }
}

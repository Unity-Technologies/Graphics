using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    internal static class MaterialEditorExtension
    {
        static uint defaultExpandedState => 0xFFFFFFFF; //all opened by default

        private const string k_KeyPrefix = "HDRP:Material:UI_State:";

        public static void InitExpandableState(this MaterialEditor editor)
        {
            string key = GetKey(editor);
            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetInt(key, (int)defaultExpandedState);
            }
        }

        public static bool GetExpandedAreas(this MaterialEditor editor, uint mask)
        {
            uint state = GetState(editor);
            bool result = (state & mask) > 0;
            return result;
        }

        public static void SetExpandedAreas(this MaterialEditor editor, uint mask, bool value)
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
            return k_KeyPrefix + ((Material)editor.target).shader.name;
        }
    }
}

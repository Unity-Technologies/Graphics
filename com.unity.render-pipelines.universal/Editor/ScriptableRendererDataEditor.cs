using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    public class ScriptableRendererDataEditor : PropertyDrawer
    {
        protected static int index = -1;

        internal static void DrawRenderer(Editor ownerEditor, int index, SerializedProperty serializedProperty)
        {
            ScriptableRendererDataEditor.index = index;
            EditorGUILayout.PropertyField(serializedProperty);
            ScriptableRendererDataEditor.index = -1;
        }
    }
}

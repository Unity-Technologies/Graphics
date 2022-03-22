using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class CopyPasteUtils
    {
        public static bool HasCopyObject(object obj)
        {
            string text = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(text))
                return false;
            var prefix = PropertyPrefix(obj);
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        public static void ParseObject(object obj)
        {
            string text = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(text))
                return;
            var prefix = PropertyPrefix(obj);
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return;
            try
            {
                EditorJsonUtility.FromJsonOverwrite(text.Substring(prefix.Length), obj);
            }
            catch (ArgumentException)
            {
                return;
            }
        }

        public static void WriteObject(object obj)
        {
            EditorGUIUtility.systemCopyBuffer = PropertyPrefix(obj) + EditorJsonUtility.ToJson(obj);
        }

        static string PropertyPrefix(object obj)
        {
            return obj.GetType().FullName + "JSON:";
        }
    }
}

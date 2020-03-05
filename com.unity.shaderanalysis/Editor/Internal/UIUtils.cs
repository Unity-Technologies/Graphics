using System;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis.Internal
{
    public static class UIUtils
    {
        const int k_MaxLogSize = ushort.MaxValue / 4 - 5;
        static GUIContent s_TextContent = new GUIContent();

        public static GUIContent Text(string text)
        {
            s_TextContent.text = text;
            return s_TextContent;
        }

        public static GUIContent Text(string format, params object[] args)
        {
            s_TextContent.text = string.Format(format, args);
            return s_TextContent;
        }

        public static string ClampText(string text)
        {
            return text.Length > k_MaxLogSize
                ? text.Substring(0, k_MaxLogSize)
                : text;
        }
    }
}

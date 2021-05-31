using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    static class BlackboardUtils
    {
        internal static int GetInsertionIndex(VisualElement owner, Vector2 position, IEnumerable<VisualElement> children)
        {
            var index = -1;
            if (owner.ContainsPoint(position))
            {
                index = 0;
                foreach (VisualElement child in children)
                {
                    Rect rect = child.layout;

                    if (position.y > (rect.y + rect.height / 2))
                    {
                        ++index;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return index;
        }

        internal static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "—";
            return path;
        }

        internal static string SanitizePath(string path)
        {
            var splitString = path.Split('/');
            List<string> newStrings = new List<string>();
            foreach (string s in splitString)
            {
                var str = s.Trim();
                if (!string.IsNullOrEmpty(str))
                {
                    newStrings.Add(str);
                }
            }

            return string.Join("/", newStrings.ToArray());
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D.GUIFramework;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal static class PathEditorToolExtensions
    {
        public static void CycleTangentMode<T>(this PathEditorTool<T> pathEditorTool) where T : ScriptablePath
        {
            var first = true;
            var mixed = false;
            var tangentMode = TangentMode.Linear;
            var targets = pathEditorTool.targets;

            foreach(var target in targets)
            {
                var path = pathEditorTool.GetPath(target);

                if (path.selection.Count == 0)
                    continue;

                for (var i = 0; i < path.pointCount; ++i)
                {
                    if (!path.selection.Contains(i))
                        continue;

                    var point = path.GetPoint(i);
                    
                    if (first)
                    {
                        first = false;
                        tangentMode = point.tangentMode;
                    }
                    else if (point.tangentMode != tangentMode)
                    {
                        mixed = true;
                        break;
                    }
                }

                if (mixed)
                    break;
            }

            if (mixed)
                tangentMode = TangentMode.Linear;
            else
                tangentMode = GetNextTangentMode(tangentMode);

            foreach(var target in targets)
            {
                var path = pathEditorTool.GetPath(target);

                if (path.selection.Count == 0)
                    continue;

                path.undoObject.RegisterUndo("Cycle Tangent Mode");

                for (var i = 0; i < path.pointCount; ++i)
                {
                    if (!path.selection.Contains(i))
                        continue;

                    path.SetTangentMode(i, tangentMode);
                }

                pathEditorTool.SetPath(target);
            }
        }

        public static void MirrorTangent<T>(this PathEditorTool<T> pathEditorTool) where T : ScriptablePath
        {
            var targets = pathEditorTool.targets;

            foreach(var target in targets)
            {
                var path = pathEditorTool.GetPath(target);

                if (path.selection.Count == 0)
                    continue;

                path.undoObject.RegisterUndo("Mirror Tangents");

                for (var i = 0; i < path.pointCount; ++i)
                {
                    if (!path.selection.Contains(i))
                        continue;

                    path.MirrorTangent(i);
                }

                pathEditorTool.SetPath(target);
            }
        }

        private static TangentMode GetNextTangentMode(TangentMode tangentMode)
        {
            return (TangentMode)((((int)tangentMode) + 1) % Enum.GetValues(typeof(TangentMode)).Length);
        }
    }

}

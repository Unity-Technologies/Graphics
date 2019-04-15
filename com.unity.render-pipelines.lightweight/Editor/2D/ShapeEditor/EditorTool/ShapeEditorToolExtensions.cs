using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.LWRP.Path2D.GUIFramework;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal static class ShapeEditorToolExtensions
    {
        public static void CycleTangentMode<T>(this ShapeEditorTool<T> shapeEditorTool) where T : ScriptableShapeEditor
        {
            var first = true;
            var mixed = false;
            var tangentMode = TangentMode.Linear;
            var targets = shapeEditorTool.targets;

            foreach(var target in targets)
            {
                var shapeEditor = shapeEditorTool.GetShapeEditor(target);

                if (shapeEditor.selection.Count == 0)
                    continue;

                for (var i = 0; i < shapeEditor.pointCount; ++i)
                {
                    if (!shapeEditor.selection.Contains(i))
                        continue;

                    var point = shapeEditor.GetPoint(i);
                    
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
                var shapeEditor = shapeEditorTool.GetShapeEditor(target);

                if (shapeEditor.selection.Count == 0)
                    continue;

                shapeEditor.undoObject.RegisterUndo("Cycle Tangent Mode");

                for (var i = 0; i < shapeEditor.pointCount; ++i)
                {
                    if (!shapeEditor.selection.Contains(i))
                        continue;

                    shapeEditor.SetTangentMode(i, tangentMode);
                }

                shapeEditorTool.SetShape(target);
            }
        }

        public static void MirrorTangent<T>(this ShapeEditorTool<T> shapeEditorTool) where T : ScriptableShapeEditor
        {
            var targets = shapeEditorTool.targets;

            foreach(var target in targets)
            {
                var shapeEditor = shapeEditorTool.GetShapeEditor(target);

                if (shapeEditor.selection.Count == 0)
                    continue;

                shapeEditor.undoObject.RegisterUndo("Mirror Tangents");

                for (var i = 0; i < shapeEditor.pointCount; ++i)
                {
                    if (!shapeEditor.selection.Contains(i))
                        continue;

                    shapeEditor.MirrorTangent(i);
                }

                shapeEditorTool.SetShape(target);
            }
        }

        private static TangentMode GetNextTangentMode(TangentMode tangentMode)
        {
            return (TangentMode)((((int)tangentMode) + 1) % Enum.GetValues(typeof(TangentMode)).Length);
        }
    }

}

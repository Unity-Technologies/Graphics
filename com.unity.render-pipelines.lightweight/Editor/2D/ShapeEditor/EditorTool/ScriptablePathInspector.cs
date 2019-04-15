using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptablePath), true)]
    internal class ScriptablePathInspector : Editor
    {
        private static class Contents
        {
            public static readonly GUIContent linearIcon = IconContent("TangentLinear", "TangentLinearPro", "Linear");
            public static readonly GUIContent continuousIcon = IconContent("TangentContinuous", "TangentContinuousPro", "Continuous");
            public static readonly GUIContent brokenIcon = IconContent("TangentBroken", "TangentBrokenPro", "Broken");
            public static readonly GUIContent positionLabel = new GUIContent("Position", "Position of the Control Point");
            public static readonly GUIContent enableSnapLabel = new GUIContent("Snapping", "Snap points using the snap settings");
            public static readonly GUIContent tangentModeLabel = new GUIContent("Tangent Mode");
            public static readonly GUIContent pointLabel = new GUIContent("Point");


            private static GUIContent IconContent(string name, string tooltip = null)
            {
                return new GUIContent(Resources.Load<Texture>(name), tooltip);
            }

            private static GUIContent IconContent(string personal, string pro, string tooltip)
            {
                if (EditorGUIUtility.isProSkin)
                    return IconContent(pro, tooltip);
                
                return IconContent(personal, tooltip);
            }
        }

        private List<ScriptablePath> m_ShapeEditors = null;
        private bool m_Dragged = false;

        protected List<ScriptablePath> shapeEditors
        {
            get
            {
                if (m_ShapeEditors == null)
                    m_ShapeEditors = targets.Select( t => t as ScriptablePath).ToList();
                
                return m_ShapeEditors;
            }
        }

        public override void OnInspectorGUI()
        {
            DoTangentModeInspector();
            DoPositionInspector();
        }

        protected void DoTangentModeInspector()
        {
            if (!IsAnyShapeType(ShapeType.Spline))
                return;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Contents.tangentModeLabel);

            using (new EditorGUI.DisabledGroupScope(!IsAnyPointSelected()))
            {
                if (DoToggle(GetToggleStateFromTangentMode(TangentMode.Linear), Contents.linearIcon))
                    SetMixedTangentMode(TangentMode.Linear);

                if (DoToggle(GetToggleStateFromTangentMode(TangentMode.Continuous), Contents.continuousIcon))
                    SetMixedTangentMode(TangentMode.Continuous);

                if (DoToggle(GetToggleStateFromTangentMode(TangentMode.Broken), Contents.brokenIcon))
                    SetMixedTangentMode(TangentMode.Broken);
            }

            EditorGUILayout.EndHorizontal();
        }

        protected void DoPositionInspector()
        {
            var position = Vector3.zero;
            var isMixed = GetMixedPosition(out position);

            EditorGUI.showMixedValue = isMixed;
            EditorGUIUtility.wideMode = true;

            using (new EditorGUI.DisabledGroupScope(!IsAnyPointSelected()))
            {
                if (GUIUtility.hotControl == 0)
                    m_Dragged = false;

                EditorGUI.BeginChangeCheck();

                var delta = EditorGUILayout.Vector2Field(Contents.positionLabel, position) - (Vector2)position;

                if (EditorGUI.EndChangeCheck())
                {
                    if (m_Dragged == false)
                    {
                        foreach(var shapeEditor in shapeEditors)
                            shapeEditor.undoObject.RegisterUndo("Point Position");
                        
                        m_Dragged = true;
                    }

                    SetMixedDeltaPosition(delta);
                }
            }

            EditorGUI.showMixedValue = false;
        }

        private bool DoToggle(bool value, GUIContent icon)
        {
            const float kButtonWidth = 33f;
            const float kButtonHeight = 23f;
            var buttonStyle = new GUIStyle("EditModeSingleButton");

            var changed = false;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                value = GUILayout.Toggle(value, icon, buttonStyle, GUILayout.Width(kButtonWidth), GUILayout.Height(kButtonHeight));
                changed = check.changed;
            }
            
            return value && changed;
        }

        private bool GetToggleStateFromTangentMode(TangentMode mode)
        {
            foreach(var shapeEditor in shapeEditors)
            {
                var selection = shapeEditor.selection;

                foreach (var index in selection.elements)
                    if (shapeEditor.GetPoint(index).tangentMode != mode)
                        return false;
            }
            
            return true;
        }

        private void SetMixedTangentMode(TangentMode tangentMode)
        {
            foreach(var shapeEditor in shapeEditors)
            {
                shapeEditor.undoObject.RegisterUndo("Tangent Mode");

                foreach (var index in shapeEditor.selection.elements)
                    shapeEditor.SetTangentMode(index, tangentMode);
            }

            SceneView.RepaintAll();
        }

        private bool GetMixedPosition(out Vector3 position)
        {
            var first = true;
            position = Vector3.zero;

            foreach(var shapeEditor in shapeEditors)
            {
                var selection = shapeEditor.selection;
                var matrix = shapeEditor.localToWorldMatrix;

                shapeEditor.localToWorldMatrix = Matrix4x4.identity;

                foreach (var index in selection.elements)
                {
                    var controlPoint = shapeEditor.GetPoint(index);

                    if (first)
                    {
                        position  = controlPoint.position;
                        first = false;
                    }
                    else if (position != controlPoint.position)
                    {
                        return true;
                    }
                }

                shapeEditor.localToWorldMatrix = matrix;
            }
            
            return false;
        }

        private void SetMixedDeltaPosition(Vector3 delta)
        {
            foreach(var shapeEditor in shapeEditors)
            {
                var selection = shapeEditor.selection;
                var matrix = shapeEditor.localToWorldMatrix;

                shapeEditor.localToWorldMatrix = Matrix4x4.identity;

                foreach (var index in selection.elements)
                {
                    var controlPoint = shapeEditor.GetPoint(index);
                    controlPoint.position += delta;
                    shapeEditor.SetPoint(index, controlPoint);
                }

                shapeEditor.localToWorldMatrix = matrix;
            }
        }

        private bool IsAnyShapeType(ShapeType shapeType)
        {
            foreach(var shapeEditor in shapeEditors)
                if (shapeEditor.shapeType == shapeType)
                    return true;

            return false;
        }

        protected bool IsAnyPointSelected()
        {
            foreach(var shapeEditor in shapeEditors)
                if (shapeEditor.selection.Count > 0)
                    return true;

            return false;
        }
    }
}

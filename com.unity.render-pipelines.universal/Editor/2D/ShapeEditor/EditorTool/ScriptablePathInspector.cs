using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
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

        private List<ScriptablePath> m_Paths = null;
        private bool m_Dragged = false;

        protected List<ScriptablePath> paths
        {
            get
            {
                if (m_Paths == null)
                    m_Paths = targets.Select( t => t as ScriptablePath).ToList();
                
                return m_Paths;
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
            var showMixedValue = EditorGUI.showMixedValue;
            var wideMode = EditorGUIUtility.wideMode;

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
                        foreach(var path in paths)
                            path.undoObject.RegisterUndo("Point Position");
                        
                        m_Dragged = true;
                    }

                    SetMixedDeltaPosition(delta);
                }
            }

            EditorGUI.showMixedValue = showMixedValue;
            EditorGUIUtility.wideMode = wideMode;
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
            foreach(var path in paths)
            {
                var selection = path.selection;

                foreach (var index in selection.elements)
                    if (path.GetPoint(index).tangentMode != mode)
                        return false;
            }
            
            return true;
        }

        private void SetMixedTangentMode(TangentMode tangentMode)
        {
            foreach(var path in paths)
            {
                path.undoObject.RegisterUndo("Tangent Mode");

                foreach (var index in path.selection.elements)
                    path.SetTangentMode(index, tangentMode);
            }

            SceneView.RepaintAll();
        }

        private bool GetMixedPosition(out Vector3 position)
        {
            var first = true;
            position = Vector3.zero;

            foreach(var path in paths)
            {
                var selection = path.selection;
                var matrix = path.localToWorldMatrix;

                path.localToWorldMatrix = Matrix4x4.identity;

                foreach (var index in selection.elements)
                {
                    var controlPoint = path.GetPoint(index);

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

                path.localToWorldMatrix = matrix;
            }
            
            return false;
        }

        private void SetMixedDeltaPosition(Vector3 delta)
        {
            foreach(var path in paths)
            {
                var selection = path.selection;
                var matrix = path.localToWorldMatrix;

                path.localToWorldMatrix = Matrix4x4.identity;

                foreach (var index in selection.elements)
                {
                    var controlPoint = path.GetPoint(index);
                    controlPoint.position += delta;
                    path.SetPoint(index, controlPoint);
                }

                path.localToWorldMatrix = matrix;
            }
        }

        private bool IsAnyShapeType(ShapeType shapeType)
        {
            foreach(var path in paths)
                if (path.shapeType == shapeType)
                    return true;

            return false;
        }

        protected bool IsAnyPointSelected()
        {
            foreach(var path in paths)
                if (path.selection.Count > 0)
                    return true;

            return false;
        }
    }
}

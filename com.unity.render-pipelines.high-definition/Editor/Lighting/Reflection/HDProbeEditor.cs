using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using System.Reflection;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;
using System;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    interface IHDProbeEditor
    {
        Object target { get; }
        HDProbe GetTarget(Object editorTarget);
    }

    abstract class HDProbeEditor<TProvider, TSerialized> : Editor, IHDProbeEditor
        where TProvider : struct, HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
        where TSerialized : SerializedHDProbe
    {
        static Dictionary<Component, Editor> s_Editors = new Dictionary<Component, Editor>();
        internal static Editor GetEditorFor(Component p) => s_Editors.TryGetValue(p, out Editor e) ? e : null;


        protected abstract TSerialized NewSerializedObject(SerializedObject so);
        internal abstract HDProbe GetTarget(Object editorTarget);
        HDProbe IHDProbeEditor.GetTarget(Object editorTarget) => GetTarget(editorTarget);

        TSerialized m_SerializedHDProbe;
        protected HDProbeUI m_UIState;
        protected HDProbeUI[] m_UIHandleState;
        protected HDProbe[] m_TypedTargets;

        public override void OnInspectorGUI()
        {
            m_SerializedHDProbe.Update();
            EditorGUI.BeginChangeCheck();
            Draw(m_UIState, m_SerializedHDProbe, this);
            if (EditorGUI.EndChangeCheck())
                m_SerializedHDProbe.Apply();
        }

        protected virtual void OnEnable()
        {
            m_SerializedHDProbe = NewSerializedObject(serializedObject);
            m_UIState = new HDProbeUI();

            m_TypedTargets = new HDProbe[targets.Length];
            m_UIHandleState = new HDProbeUI[m_TypedTargets.Length];
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_TypedTargets[i] = GetTarget(targets[i]);
                m_UIHandleState[i] = new HDProbeUI();
            }

            foreach (var target in serializedObject.targetObjects)
                s_Editors[(Component)target] = this;
        }

        protected virtual void OnDisable()
        {
            foreach (var target in serializedObject.targetObjects)
            {
                if (target != null && !target.Equals(null))
                    s_Editors.Remove((Component)target);
            }
                
        }

        protected virtual void Draw(HDProbeUI s, TSerialized p, Editor o)
        {
            HDProbeUI.Drawer<TProvider>.DrawToolbars(s, p, o);
            HDProbeUI.Drawer<TProvider>.DrawPrimarySettings(s, p, o);
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedProjection, "Projection Settings"))
            {
                ++EditorGUI.indentLevel;
                HDProbeUI.Drawer<TProvider>.DrawProjectionSettings(s, p, o);
                --EditorGUI.indentLevel;
            }
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedInfluence, "Influence Volume"))
            {
                ++EditorGUI.indentLevel;
                HDProbeUI.Drawer<TProvider>.DrawInfluenceSettings(s, p, o);
                --EditorGUI.indentLevel;
            }
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedCapture, "Capture Settings"))
            {
                DrawAdditionalCaptureSettings(s, p, o);
                HDProbeUI.Drawer<TProvider>.DrawCaptureSettings(s, p, o);
            }
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedCustom, "Custom Settings"))
                HDProbeUI.Drawer<TProvider>.DrawCustomSettings(s, p, o);
            HDProbeUI.Drawer<TProvider>.DrawBakeButton(s, p, o);
        }

        protected virtual void DrawHandles(HDProbeUI s, TSerialized d, Editor o) { }
        protected virtual void DrawAdditionalCaptureSettings(HDProbeUI s, TSerialized d, Editor o) { }

        // TODO: generalize this
        static bool DrawAndSetSectionFoldout(HDProbeUI s, HDProbeUI.Flag flag, string title)
            => s.SetFlag(flag, HDEditorUtils.DrawSectionFoldout(title, s.HasFlag(flag)));

        protected void OnSceneGUI()
        {
            m_UIState.Update(m_SerializedHDProbe);
            m_SerializedHDProbe.Update();

            EditorGUI.BeginChangeCheck();
            HDProbeUI.DrawHandles(m_UIState, m_SerializedHDProbe, this);
            HDProbeUI.Drawer<TProvider>.DoToolbarShortcutKey(this);
            DrawHandles(m_UIState, m_SerializedHDProbe, this);
            if (EditorGUI.EndChangeCheck())
                m_SerializedHDProbe.Apply();
        }

        static Func<float> s_CapturePointPreviewSizeGetter = ComputeCapturePointPreviewSizeGetter();
        static Func<float> ComputeCapturePointPreviewSizeGetter()
        {
            var type = Type.GetType("UnityEditor.AnnotationUtility,UnityEditor");
            var property = type.GetProperty("iconSize", BindingFlags.Static | BindingFlags.NonPublic);
            var lambda = Expression.Lambda<Func<float>>(
                Expression.Multiply(
                    Expression.Property(null, property),
                    Expression.Constant(30.0f)
                )
            );
            return lambda.Compile();
        }
        internal static float capturePointPreviewSize
        { get { return s_CapturePointPreviewSizeGetter(); } }
    }
}

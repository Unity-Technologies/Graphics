using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using System.Reflection;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    interface IHDProbeEditor
    {
        Object target { get; }
        HDProbe GetTarget(Object editorTarget);

        bool showChromeGizmo { get; set; }
    }

    abstract class HDProbeEditor<TProvider, TSerialized> : Editor, IHDProbeEditor, IDefaultFrameSettingsType
        where TProvider : struct, HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
        where TSerialized : SerializedHDProbe
    {
        static Dictionary<Component, Editor> s_Editors = new Dictionary<Component, Editor>();
        internal static Editor GetEditorFor(Component p) => s_Editors.TryGetValue(p, out Editor e) ? e : null;


        protected abstract TSerialized NewSerializedObject(SerializedObject so);
        internal abstract HDProbe GetTarget(Object editorTarget);
        HDProbe IHDProbeEditor.GetTarget(Object editorTarget) => GetTarget(editorTarget);

        TSerialized m_SerializedHDProbe;
        Dictionary<Object, TSerialized> m_SerializedHDProbePerTarget;
        protected HDProbe[] m_TypedTargets;

        public override void OnInspectorGUI()
        {
            m_SerializedHDProbe.Update();
            EditorGUI.BeginChangeCheck();
            Draw(m_SerializedHDProbe, this);
            if (EditorGUI.EndChangeCheck())
                m_SerializedHDProbe.Apply();
        }

        const string k_ShowChromeGizmoKey = "HDRP:ReflectionProbe:ChromeGizmo";
        static bool m_ShowChromeGizmo = true;
        public bool showChromeGizmo
        {
            get => m_ShowChromeGizmo;
            set
            {
                m_ShowChromeGizmo = value;
                EditorPrefs.SetBool(k_ShowChromeGizmoKey, value);
            }
        }

        protected virtual void OnEnable()
        {
            m_SerializedHDProbe = NewSerializedObject(serializedObject);

            if (EditorPrefs.HasKey(k_ShowChromeGizmoKey))
                m_ShowChromeGizmo = EditorPrefs.GetBool(k_ShowChromeGizmoKey);

            m_SerializedHDProbePerTarget = new Dictionary<Object, TSerialized>(targets.Length);
            m_TypedTargets = new HDProbe[targets.Length];
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_TypedTargets[i] = GetTarget(targets[i]);
                var so = new SerializedObject(targets[i]);
                m_SerializedHDProbePerTarget[targets[i]] = NewSerializedObject(so);
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

        protected virtual void Draw(TSerialized serialized, Editor owner)
        {
            HDProbeUI.Drawer<TProvider>.DrawToolbars(serialized, owner);

            HDProbeUI.Drawer<TProvider>.DrawPrimarySettings(serialized, owner);

            //note: cannot use 'using CED = something' due to templated type passed.
            CoreEditorDrawer<TSerialized>.Group(
                CoreEditorDrawer<TSerialized>.FoldoutGroup(HDProbeUI.k_ProxySettingsHeader, HDProbeUI.Expandable.Projection, HDProbeUI.k_ExpandedState,
                    HDProbeUI.Drawer<TProvider>.DrawProjectionSettings),
                CoreEditorDrawer<TSerialized>.FoldoutGroup(HDProbeUI.k_InfluenceVolumeHeader, HDProbeUI.Expandable.Influence, HDProbeUI.k_ExpandedState,
                    HDProbeUI.Drawer<TProvider>.DrawInfluenceSettings,
                    HDProbeUI.Drawer_DifferentShapeError
                ),
                CoreEditorDrawer<TSerialized>.AdvancedFoldoutGroup(HDProbeUI.k_CaptureSettingsHeader, HDProbeUI.Expandable.Capture, HDProbeUI.k_ExpandedState,
                    (s, o) => s.GetEditorOnlyData(SerializedHDProbe.EditorOnlyData.CaptureSettingsIsAdvanced),
                    (s, o) => s.ToggleEditorOnlyData(SerializedHDProbe.EditorOnlyData.CaptureSettingsIsAdvanced),
                    CoreEditorDrawer<TSerialized>.Group(
                        DrawAdditionalCaptureSettings,
                        HDProbeUI.Drawer<TProvider>.DrawCaptureSettings
                    ),
                    HDProbeUI.Drawer<TProvider>.DrawAdvancedCaptureSettings
                ),
                CoreEditorDrawer<TSerialized>.FoldoutGroup(HDProbeUI.k_CustomSettingsHeader, HDProbeUI.Expandable.Custom, HDProbeUI.k_ExpandedState,
                    HDProbeUI.Drawer<TProvider>.DrawCustomSettings),
                CoreEditorDrawer<TSerialized>.Group(HDProbeUI.Drawer<TProvider>.DrawBakeButton)
            ).Draw(serialized, owner);
        }

        protected virtual void DrawHandles(TSerialized serialized, Editor owner) { }
        protected virtual void DrawAdditionalCaptureSettings(TSerialized serialiezed, Editor owner) { }

        protected void OnSceneGUI()
        {
            EditorGUI.BeginChangeCheck();
            var soo = m_SerializedHDProbePerTarget[target];
            soo.Update();
            HDProbeUI.DrawHandles(soo, this);

            HDProbeUI.Drawer<TProvider>.DoToolbarShortcutKey(this);
            DrawHandles(soo, this);
            if (EditorGUI.EndChangeCheck())
                soo.Apply();
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

        public FrameSettingsRenderType GetFrameSettingsType()
             => GetTarget(target).mode == ProbeSettings.Mode.Realtime
                 ? FrameSettingsRenderType.RealtimeReflection
                 : FrameSettingsRenderType.CustomOrBakedReflection;
    }
}

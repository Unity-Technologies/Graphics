using System;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class HDReflectionProbeEditor : Editor
    {
        class SerializedReflectionProbe
        {
            internal SerializedProperty mode;
            internal SerializedProperty renderDynamicObjects;

            public SerializedReflectionProbe(ReflectionProbe target, SerializedObject so)
            {
                mode = so.FindProperty("m_Mode");
                renderDynamicObjects = so.FindProperty("m_RenderDynamicObjects");
            }
        }

        class UIState
        {
            AnimBool[] m_ModeSettingsDisplays = new AnimBool[Enum.GetValues(typeof(ReflectionProbeMode)).Length];

            internal UIState()
            {
                for (var i = 0; i < m_ModeSettingsDisplays.Length; i++)
                    m_ModeSettingsDisplays[i] = new AnimBool();
            }

            internal void Reset(UnityAction repaint, ReflectionProbeMode mode)
            {
                for (var i = 0; i < m_ModeSettingsDisplays.Length; i++)
                {
                    m_ModeSettingsDisplays[i].valueChanged.RemoveAllListeners();
                    m_ModeSettingsDisplays[i].valueChanged.AddListener(repaint);
                    m_ModeSettingsDisplays[i].value = (int)mode == i;
                }
            }

            public float GetModeFaded(ReflectionProbeMode mode)
            {
                return m_ModeSettingsDisplays[(int)mode].faded;
            }

            public void SetModeTarget(int value)
            {
                for (var i = 0; i < m_ModeSettingsDisplays.Length; i++)
                    m_ModeSettingsDisplays[i].target = i == value;
            }
        }

        delegate void Drawer(UIState s, SerializedReflectionProbe p);


        SerializedReflectionProbe m_SerializedReflectionProbe;
        ReflectionProbe m_ReflectionProbe;
        UIState m_UIState = new UIState();

        void OnEnable()
        {
            m_ReflectionProbe = (ReflectionProbe)target;
            m_SerializedReflectionProbe = new SerializedReflectionProbe(m_ReflectionProbe, serializedObject);
            m_UIState.Reset(Repaint, m_ReflectionProbe.mode);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Drawer_ReflectionProbeMode(m_UIState, m_SerializedReflectionProbe);
            Drawer_ModeSettings(m_UIState, m_SerializedReflectionProbe);

            serializedObject.ApplyModifiedProperties();
        }

        static readonly GUIContent[] k_Content_ReflectionProbeMode = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_Content_ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        static void Drawer_ReflectionProbeMode(UIState s, SerializedReflectionProbe p)
        {
            EditorGUILayout.IntPopup(p.mode, k_Content_ReflectionProbeMode, k_Content_ReflectionProbeModeValues, CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting)."));
            s.SetModeTarget(p.mode.intValue);
        }

        static void Drawer_NOOP(UIState s, SerializedReflectionProbe p) { }


        static void Drawer_ModeSettings(UIState s, SerializedReflectionProbe p)
        {
            for (var i = 0; i < k_ModeDrawers.Length; ++i)
            {
                if (EditorGUILayout.BeginFadeGroup(s.GetModeFaded((ReflectionProbeMode)i)))
                {
                    ++EditorGUI.indentLevel;
                    k_ModeDrawers[i](s, p);
                    --EditorGUI.indentLevel;
                }
                EditorGUILayout.EndFadeGroup();
            }
        }

        static readonly Drawer[] k_ModeDrawers = { Drawer_NOOP, Drawer_ModeRealtime , Drawer_ModeCustom };
        static void Drawer_ModeCustom(UIState s, SerializedReflectionProbe p)
        {
            EditorGUILayout.PropertyField(p.renderDynamicObjects, CoreEditorUtils.GetContent("Dynamic Objects|If enabled dynamic objects are also rendered into the cubemap"));
        }

        static void Drawer_ModeRealtime(UIState s, SerializedReflectionProbe p) { }
    }
}

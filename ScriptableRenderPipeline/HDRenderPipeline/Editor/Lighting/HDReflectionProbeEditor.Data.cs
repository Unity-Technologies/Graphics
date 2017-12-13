using System;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.Rendering;
using UnityEditorInternal;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor
{
    partial class HDReflectionProbeEditor
    {
        class SerializedReflectionProbe
        {
            internal SerializedObject so;

            internal SerializedProperty mode;
            internal SerializedProperty renderDynamicObjects;
            internal SerializedProperty customBakedTexture; 
            internal SerializedProperty refreshMode;
            internal SerializedProperty timeSlicingMode;
            internal SerializedProperty intensityMultiplier;

            internal SerializedProperty influenceShape;

            public SerializedReflectionProbe(SerializedObject so, SerializedObject addso)
            {
                var o = new PropertyFetcher<HDAdditionalReflectionData>(addso);

                this.so = so;

                mode = so.FindProperty("m_Mode");
                customBakedTexture = so.FindProperty("m_CustomBakedTexture"); 
                renderDynamicObjects = so.FindProperty("m_RenderDynamicObjects");
                refreshMode = so.FindProperty("m_RefreshMode");
                timeSlicingMode = so.FindProperty("m_TimeSlicingMode");
                intensityMultiplier = so.FindProperty("m_IntensityMultiplier");

                influenceShape = o.Find(d => d.m_InfluenceShape);
            }
        }

        class UIState
        {
            AnimBool[] m_ModeSettingsDisplays = new AnimBool[Enum.GetValues(typeof(ReflectionProbeMode)).Length];

            Editor owner { get; set; }

            public bool shouldUpdateOldLocalSpace { get; set; }

            public bool sceneViewEditing
            {
                get { return IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(owner); }
            }

            internal UIState()
            {
                for (var i = 0; i < m_ModeSettingsDisplays.Length; i++)
                    m_ModeSettingsDisplays[i] = new AnimBool();
            }

            internal void Reset(Editor owner, UnityAction repaint, int modeValue)
            {
                this.owner = owner;
                shouldUpdateOldLocalSpace = false;

                for (var i = 0; i < m_ModeSettingsDisplays.Length; i++)
                {
                    m_ModeSettingsDisplays[i].valueChanged.RemoveAllListeners();
                    m_ModeSettingsDisplays[i].valueChanged.AddListener(repaint);
                    m_ModeSettingsDisplays[i].value = modeValue == i;
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

            static bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
            {
                return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox || editMode == EditMode.SceneViewEditMode.Collider || editMode == EditMode.SceneViewEditMode.GridBox ||
                    editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
            }
        }

        delegate void Drawer(UIState s, SerializedReflectionProbe p, Editor owner);
    }
}

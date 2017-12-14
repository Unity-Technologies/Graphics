using System;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.Rendering;
using UnityEditorInternal;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        internal class SerializedReflectionProbe
        {
            internal SerializedObject so;

            internal SerializedProperty mode;
            internal SerializedProperty renderDynamicObjects;
            internal SerializedProperty customBakedTexture; 
            internal SerializedProperty refreshMode;
            internal SerializedProperty timeSlicingMode;
            internal SerializedProperty intensityMultiplier;
            internal SerializedProperty blendDistance;
            internal SerializedProperty boxSize;
            internal SerializedProperty boxOffset;
            
            internal SerializedProperty influenceShape;
            internal SerializedProperty influenceSphereRadius;
            internal SerializedProperty useSeparateProjectionVolume;

            public SerializedReflectionProbe(SerializedObject so, SerializedObject addso)
            {
                this.so = so;

                mode = so.FindProperty("m_Mode");
                customBakedTexture = so.FindProperty("m_CustomBakedTexture"); 
                renderDynamicObjects = so.FindProperty("m_RenderDynamicObjects");
                refreshMode = so.FindProperty("m_RefreshMode");
                timeSlicingMode = so.FindProperty("m_TimeSlicingMode");
                intensityMultiplier = so.FindProperty("m_IntensityMultiplier"); 
                blendDistance = so.FindProperty("m_BlendDistance");
                boxSize = so.FindProperty("m_BoxSize");
                boxOffset = so.FindProperty("m_BoxOffset");

                influenceShape = addso.Find((HDAdditionalReflectionData d) => d.m_InfluenceShape);
                influenceSphereRadius = addso.Find((HDAdditionalReflectionData d) => d.m_InfluenceSphereRadius);
                useSeparateProjectionVolume = addso.Find((HDAdditionalReflectionData d) => d.m_UseSeparateProjectionVolume);
            }
        }

        [Flags]
        internal enum Operation
        {
            None = 0,
            UpdateOldLocalSpace = 1 << 0,
            FitVolumeToSurroundings = 1 << 1
        }

        internal class UIState
        {
            AnimBool[] m_ModeSettingsDisplays = new AnimBool[Enum.GetValues(typeof(ReflectionProbeMode)).Length];
            AnimBool[] m_InfluenceShapeDisplays = new AnimBool[Enum.GetValues(typeof(ReflectionInfluenceShape)).Length];

            Editor owner { get; set; }
            Operation operations { get; set; }

            public bool HasOperation(Operation op) { return (operations & op) == op; }
            public void ClearOperation(Operation op) { operations &= ~op; }
            public void AddOperation(Operation op) { operations |= op; }

            public bool HasAndClearOperation(Operation op)
            {
                var has = HasOperation(op);
                ClearOperation(op);
                return has;
            }

            public bool sceneViewEditing
            {
                get { return IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(owner); }
            }

            internal UIState()
            {
                for (var i = 0; i < m_ModeSettingsDisplays.Length; i++)
                    m_ModeSettingsDisplays[i] = new AnimBool();
                for (var i = 0; i < m_InfluenceShapeDisplays.Length; i++)
                    m_InfluenceShapeDisplays[i] = new AnimBool();
            }

            internal void Reset(Editor owner, UnityAction repaint, int modeValue, int shapeValue)
            {
                this.owner = owner;
                operations = 0;

                for (var i = 0; i < m_ModeSettingsDisplays.Length; i++)
                {
                    m_ModeSettingsDisplays[i].valueChanged.RemoveAllListeners();
                    m_ModeSettingsDisplays[i].valueChanged.AddListener(repaint);
                    m_ModeSettingsDisplays[i].value = modeValue == i;
                }

                for (var i = 0; i < m_InfluenceShapeDisplays.Length; i++)
                {
                    m_InfluenceShapeDisplays[i].valueChanged.RemoveAllListeners();
                    m_InfluenceShapeDisplays[i].valueChanged.AddListener(repaint);
                    m_InfluenceShapeDisplays[i].value = shapeValue == i;
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

            public float GetShapeFaded(ReflectionInfluenceShape value)
            {
                return m_InfluenceShapeDisplays[(int)value].faded;
            }

            public void SetShapeTarget(int value)
            {
                for (var i = 0; i < m_InfluenceShapeDisplays.Length; i++)
                    m_InfluenceShapeDisplays[i].target = i == value;
            }

            static bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
            {
                return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox || editMode == EditMode.SceneViewEditMode.Collider || editMode == EditMode.SceneViewEditMode.GridBox ||
                    editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        internal class SerializedReflectionProbe
        {
            internal ReflectionProbe target;
            internal HDAdditionalReflectionData targetData;

            internal SerializedObject so;

            internal SerializedProperty mode;
            internal SerializedProperty renderDynamicObjects;
            internal SerializedProperty customBakedTexture;
            internal SerializedProperty refreshMode;
            internal SerializedProperty timeSlicingMode;
            internal SerializedProperty intensityMultiplier;
            internal SerializedProperty legacyBlendDistance;
            internal SerializedProperty boxSize;
            internal SerializedProperty boxOffset;
            internal SerializedProperty resolution;
            internal SerializedProperty shadowDistance;
            internal SerializedProperty cullingMask;
            internal SerializedProperty useOcclusionCulling;
            internal SerializedProperty nearClip;
            internal SerializedProperty farClip;
            internal SerializedProperty boxProjection;

            internal SerializedProperty influenceShape;
            internal SerializedProperty influenceSphereRadius;
            internal SerializedProperty useSeparateProjectionVolume;
            internal SerializedProperty boxReprojectionVolumeSize;
            internal SerializedProperty boxReprojectionVolumeCenter;
            internal SerializedProperty sphereReprojectionVolumeRadius;
            internal SerializedProperty blendDistancePositive;
            internal SerializedProperty blendDistanceNegative;
            internal SerializedProperty blendNormalDistancePositive;
            internal SerializedProperty blendNormalDistanceNegative;
            internal SerializedProperty boxSideFadePositive;
            internal SerializedProperty boxSideFadeNegative;
            internal SerializedProperty dimmer;

            public SerializedReflectionProbe(SerializedObject so, SerializedObject addso)
            {
                this.so = so;
                target = (ReflectionProbe)so.targetObject;
                targetData = target.GetComponent<HDAdditionalReflectionData>();

                mode = so.FindProperty("m_Mode");
                customBakedTexture = so.FindProperty("m_CustomBakedTexture");
                renderDynamicObjects = so.FindProperty("m_RenderDynamicObjects");
                refreshMode = so.FindProperty("m_RefreshMode");
                timeSlicingMode = so.FindProperty("m_TimeSlicingMode");
                intensityMultiplier = so.FindProperty("m_IntensityMultiplier");
                boxSize = so.FindProperty("m_BoxSize");
                boxOffset = so.FindProperty("m_BoxOffset");
                resolution = so.FindProperty("m_Resolution");
                shadowDistance = so.FindProperty("m_ShadowDistance");
                cullingMask = so.FindProperty("m_CullingMask");
                useOcclusionCulling = so.FindProperty("m_UseOcclusionCulling");
                nearClip = so.FindProperty("m_NearClip");
                farClip = so.FindProperty("m_FarClip");
                boxProjection = so.FindProperty("m_BoxProjection");
                legacyBlendDistance = so.FindProperty("m_BlendDistance");

                influenceShape = addso.Find((HDAdditionalReflectionData d) => d.influenceShape);
                influenceSphereRadius = addso.Find((HDAdditionalReflectionData d) => d.influenceSphereRadius);
                useSeparateProjectionVolume = addso.Find((HDAdditionalReflectionData d) => d.useSeparateProjectionVolume);
                boxReprojectionVolumeSize = addso.Find((HDAdditionalReflectionData d) => d.boxReprojectionVolumeSize);
                boxReprojectionVolumeCenter = addso.Find((HDAdditionalReflectionData d) => d.boxReprojectionVolumeCenter);
                sphereReprojectionVolumeRadius = addso.Find((HDAdditionalReflectionData d) => d.sphereReprojectionVolumeRadius);
                dimmer = addso.Find((HDAdditionalReflectionData d) => d.dimmer);
                blendDistancePositive = addso.Find((HDAdditionalReflectionData d) => d.blendDistancePositive);
                blendDistanceNegative = addso.Find((HDAdditionalReflectionData d) => d.blendDistanceNegative);
                blendNormalDistancePositive = addso.Find((HDAdditionalReflectionData d) => d.blendNormalDistancePositive);
                blendNormalDistanceNegative = addso.Find((HDAdditionalReflectionData d) => d.blendNormalDistanceNegative);
                boxSideFadePositive = addso.Find((HDAdditionalReflectionData d) => d.boxSideFadePositive);
                boxSideFadeNegative = addso.Find((HDAdditionalReflectionData d) => d.boxSideFadeNegative);
            }
        }

        [Flags]
        internal enum Operation
        {
            None = 0,
            FitVolumeToSurroundings = 1 << 0
        }

        internal class UIState
        {
            AnimBool[] m_IsSectionExpandedModeSettings = new AnimBool[Enum.GetValues(typeof(ReflectionProbeMode)).Length];
            AnimBool[] m_IsSectionExpandedInfluenceShape = new AnimBool[Enum.GetValues(typeof(ReflectionInfluenceShape)).Length];

            Editor owner { get; set; }
            Operation operations { get; set; }
            public bool HasOperation(Operation op) { return (operations & op) == op; }
            public void ClearOperation(Operation op) { operations &= ~op; }
            public void AddOperation(Operation op) { operations |= op; }

            public BoxBoundsHandle boxInfluenceHandle = new BoxBoundsHandle();
            public BoxBoundsHandle boxProjectionHandle = new BoxBoundsHandle();
            public BoxBoundsHandle boxBlendHandle = new BoxBoundsHandle();
            public BoxBoundsHandle boxBlendNormalHandle = new BoxBoundsHandle();
            public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
            public SphereBoundsHandle sphereProjectionHandle = new SphereBoundsHandle();
            public SphereBoundsHandle sphereBlendHandle = new SphereBoundsHandle();
            public SphereBoundsHandle sphereBlendNormalHandle = new SphereBoundsHandle();
            public Matrix4x4 oldLocalSpace = Matrix4x4.identity;

            public AnimBool isSectionExpandedInfluenceVolume = new AnimBool();
            public AnimBool isSectionExpandedSeparateProjection = new AnimBool();
            public AnimBool isSectionExpandedCaptureSettings = new AnimBool();
            public AnimBool isSectionExpandedAdditional = new AnimBool();

            List<AnimBool> m_AnimBools = new List<AnimBool>();

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
                for (var i = 0; i < m_IsSectionExpandedModeSettings.Length; i++)
                {
                    m_IsSectionExpandedModeSettings[i] = new AnimBool();
                    m_AnimBools.Add(m_IsSectionExpandedModeSettings[i]);
                }

                for (var i = 0; i < m_IsSectionExpandedInfluenceShape.Length; i++)
                {
                    m_IsSectionExpandedInfluenceShape[i] = new AnimBool();
                    m_AnimBools.Add(m_IsSectionExpandedInfluenceShape[i]);
                }

                m_AnimBools.Add(isSectionExpandedInfluenceVolume);
                m_AnimBools.Add(isSectionExpandedSeparateProjection);
                m_AnimBools.Add(isSectionExpandedCaptureSettings);
                m_AnimBools.Add(isSectionExpandedAdditional);
            }

            internal void Reset(
                Editor owner, 
                UnityAction repaint, 
                SerializedReflectionProbe p)
            {
                this.owner = owner;
                operations = 0;

                for (var i = 0; i < m_IsSectionExpandedModeSettings.Length; i++)
                    m_IsSectionExpandedModeSettings[i].value = p.mode.intValue == i;

                for (var i = 0; i < m_IsSectionExpandedInfluenceShape.Length; i++)
                    m_IsSectionExpandedInfluenceShape[i].value = p.influenceShape.intValue == i;

                isSectionExpandedSeparateProjection.value = p.useSeparateProjectionVolume.boolValue;
                isSectionExpandedCaptureSettings.value = true;
                isSectionExpandedInfluenceVolume.value = true;
                isSectionExpandedAdditional.value = false;

                for (var i = 0; i < m_AnimBools.Count; ++i)
                {
                    m_AnimBools[i].valueChanged.RemoveAllListeners();
                    m_AnimBools[i].valueChanged.AddListener(repaint);
                }
            }

            public AnimBool IsSectionExpandedMode(ReflectionProbeMode mode)
            {
                return m_IsSectionExpandedModeSettings[(int)mode];
            }

            public void SetModeTarget(int value)
            {
                for (var i = 0; i < m_IsSectionExpandedModeSettings.Length; i++)
                    m_IsSectionExpandedModeSettings[i].target = i == value;
            }

            public AnimBool IsSectionExpandedShape(ReflectionInfluenceShape value)
            {
                return m_IsSectionExpandedInfluenceShape[(int)value];
            }

            public void SetShapeTarget(int value)
            {
                for (var i = 0; i < m_IsSectionExpandedInfluenceShape.Length; i++)
                    m_IsSectionExpandedInfluenceShape[i].target = i == value;
            }

            internal void UpdateOldLocalSpace(ReflectionProbe target)
            {
                oldLocalSpace = GetLocalSpace(target);
            }
        }
    }
}

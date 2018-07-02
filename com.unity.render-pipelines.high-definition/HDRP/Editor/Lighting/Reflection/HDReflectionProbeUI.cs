using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Gizmo;

namespace UnityEditor.Experimental.Rendering
{
    public partial class HDReflectionProbeUI : BaseUI<SerializedHDReflectionProbe>
    {
        const int k_AnimBoolSingleFieldCount = 7;
        static readonly int k_ReflectionProbeModeCount = Enum.GetValues(typeof(ReflectionProbeMode)).Length;
        static readonly int k_ReflectionInfluenceShapeCount = Enum.GetValues(typeof(ShapeType)).Length;
        static readonly int k_AnimBoolsCount = k_ReflectionProbeModeCount + k_ReflectionInfluenceShapeCount + k_AnimBoolSingleFieldCount;

        [Flags]
        public enum Operation
        {
            None = 0,
            FitVolumeToSurroundings = 1 << 0
        }
        Operation operations { get; set; }

        public Editor owner { get; set; }
        public bool HasOperation(Operation op) { return (operations & op) == op; }
        public void ClearOperation(Operation op) { operations &= ~op; }
        public void AddOperation(Operation op) { operations |= op; }

        public BoxBoundsHandle boxInfluenceHandle = new BoxBoundsHandle();
        public BoxBoundsHandle boxProjectionHandle = new BoxBoundsHandle();
        public BoxBoundsHandle boxBlendHandle = new BoxBoundsHandle();
        public BoxBoundsHandle boxBlendNormalHandle = new BoxBoundsHandle();
        public Gizmo6FacesBox alternativeBoxInfluenceHandle = new Gizmo6FacesBox();
        public Gizmo6FacesBoxContained alternativeBoxBlendHandle;
        public Gizmo6FacesBoxContained alternativeBoxBlendNormalHandle;
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereProjectionHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereBlendHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereBlendNormalHandle = new SphereBoundsHandle();
        public Matrix4x4 oldLocalSpace = Matrix4x4.identity;

        public AnimBool isSectionExpandedProxyVolume { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedInfluenceVolume { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[2]; } }
        public AnimBool isSectionExpandedAdditional { get { return m_AnimBools[3]; } }
        public AnimBool isSectionExpendedAdvancedBlendDistanceBoxShape { get { return m_AnimBools[4]; } }
        public AnimBool isSectionExpendedAdvancedBlendNormalDistanceBoxShape { get { return m_AnimBools[5]; } }
        public AnimBool isSectionExpendedAdvancedBlendFaceFadeBoxShape { get { return m_AnimBools[6]; } }

        public bool HasAndClearOperation(Operation op)
        {
            var has = HasOperation(op);
            ClearOperation(op);
            return has;
        }

        public bool sceneViewEditing
        {
            get { return HDReflectionProbeEditor.IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(owner); }
        }

        public HDReflectionProbeUI()
            : base(k_AnimBoolsCount)
        {
            isSectionExpandedProxyVolume.value = true;
            isSectionExpandedCaptureSettings.value = true;
            isSectionExpandedInfluenceVolume.value = true;
            isSectionExpandedAdditional.value = false;
            isSectionExpendedAdvancedBlendDistanceBoxShape.value = false;
            isSectionExpendedAdvancedBlendNormalDistanceBoxShape.value = false;
            isSectionExpendedAdvancedBlendFaceFadeBoxShape.value = false;

            alternativeBoxBlendHandle = new Gizmo6FacesBoxContained(alternativeBoxInfluenceHandle);
            alternativeBoxBlendNormalHandle = new Gizmo6FacesBoxContained(alternativeBoxInfluenceHandle);

            Color[] handleColors = new Color[]
            {
                HDReflectionProbeEditor.k_handlesColor[0][0],
                HDReflectionProbeEditor.k_handlesColor[0][1],
                HDReflectionProbeEditor.k_handlesColor[0][2],
                HDReflectionProbeEditor.k_handlesColor[1][0],
                HDReflectionProbeEditor.k_handlesColor[1][1],
                HDReflectionProbeEditor.k_handlesColor[1][2]
            };
            alternativeBoxInfluenceHandle.handleColors = handleColors;
            alternativeBoxBlendHandle.handleColors = handleColors;
            alternativeBoxBlendNormalHandle.handleColors = handleColors;

            //unified color is finally better [Todo: improve gizmo to have an override color]
            alternativeBoxInfluenceHandle.faceColors = new Color[] 
            {
                HDReflectionProbeEditor.k_GizmoThemeColorExtent,
                HDReflectionProbeEditor.k_GizmoThemeColorExtent,
                HDReflectionProbeEditor.k_GizmoThemeColorExtent,
                HDReflectionProbeEditor.k_GizmoThemeColorExtent,
                HDReflectionProbeEditor.k_GizmoThemeColorExtent,
                HDReflectionProbeEditor.k_GizmoThemeColorExtent
            };
            alternativeBoxInfluenceHandle.faceColorsSelected = new Color[] 
            {
                HDReflectionProbeEditor.k_GizmoThemeColorExtentFace,
                HDReflectionProbeEditor.k_GizmoThemeColorExtentFace,
                HDReflectionProbeEditor.k_GizmoThemeColorExtentFace,
                HDReflectionProbeEditor.k_GizmoThemeColorExtentFace,
                HDReflectionProbeEditor.k_GizmoThemeColorExtentFace,
                HDReflectionProbeEditor.k_GizmoThemeColorExtentFace
            };
            alternativeBoxBlendHandle.faceColors = new Color[] 
            {
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend
            };
            alternativeBoxBlendHandle.faceColorsSelected = new Color[] 
            {
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace
            };
            alternativeBoxBlendNormalHandle.faceColors = new Color[] 
            {
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend
            };
            alternativeBoxBlendNormalHandle.faceColorsSelected = new Color[] 
            {
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace,
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace
            };
        }

        public override void Update()
        {
            operations = 0;

            SetModeTarget(data.mode.hasMultipleDifferentValues ? -1 : data.mode.intValue);
            SetShapeTarget(data.influenceShape.hasMultipleDifferentValues ? -1 : data.influenceShape.intValue);

            base.Update();
        }

        public AnimBool IsSectionExpandedMode(ReflectionProbeMode mode)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + (int)mode];
        }

        public void SetModeTarget(int value)
        {
            for (var i = 0; i < k_ReflectionProbeModeCount; i++)
                GetReflectionProbeModeBool(i).target = i == value;
        }

        public AnimBool IsSectionExpandedShape(ShapeType value)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + k_ReflectionProbeModeCount + (int)value];
        }

        public void SetShapeTarget(int value)
        {
            for (var i = 0; i < k_ReflectionInfluenceShapeCount; i++)
                GetReflectionInfluenceShapeBool(i).target = i == value;
        }

        internal void UpdateOldLocalSpace(ReflectionProbe target)
        {
            oldLocalSpace = HDReflectionProbeEditorUtility.GetLocalSpace(target);
        }

        AnimBool GetReflectionProbeModeBool(int i)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + i];
        }

        AnimBool GetReflectionInfluenceShapeBool(int i)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + k_ReflectionProbeModeCount  + i];
        }
    }
}
